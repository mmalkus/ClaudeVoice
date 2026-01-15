# Eventlet monkey patch must be first
import eventlet
eventlet.monkey_patch()

from flask import Flask, request, jsonify, render_template
from flask_cors import CORS
from flask_socketio import SocketIO, emit
from faster_whisper import WhisperModel
from vosk import Model, KaldiRecognizer, SetLogLevel
import os
import tempfile
import logging
import json
import wave
import threading

# Suppress Vosk logs BEFORE any model loading
SetLogLevel(-1)

app = Flask(__name__)
CORS(app)

@app.route('/')
def index():
    """Serve the web interface"""
    return render_template('index.html')
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='eventlet')

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize Whisper model for batch transcription
logger.info("Loading Whisper model...")
model = WhisperModel("tiny", device="cuda", compute_type="int8")
logger.info("Whisper model loaded successfully")

# Initialize Vosk models for streaming (auto-downloads if not present)
logger.info("Loading Vosk model for streaming...")
vosk_models = {}
current_language = "en-us"
VOSK_MODELS_PATH = os.path.join(os.path.dirname(__file__), "vosk-models")

def get_vosk_model(lang="en-us"):
    global vosk_models, current_language

    if lang in vosk_models:
        return vosk_models[lang]

    # Try to load from local path first
    local_path = os.path.join(VOSK_MODELS_PATH, f"vosk-model-small-{lang}")

    try:
        if os.path.exists(local_path):
            logger.info(f"Loading Vosk model from {local_path}")
            vosk_models[lang] = Model(local_path)
        else:
            # Download model automatically
            logger.info(f"Downloading Vosk model for {lang} (first time only)...")
            vosk_models[lang] = Model(lang=lang)

        logger.info(f"Vosk model for {lang} loaded successfully")
        current_language = lang
        return vosk_models[lang]
    except Exception as e:
        logger.error(f"Failed to load model for {lang}: {e}")
        # Fall back to English
        if lang != "en-us" and "en-us" in vosk_models:
            return vosk_models["en-us"]
        raise

# Store active streaming sessions
streaming_sessions = {}

# Global lock for Vosk operations (Vosk is not thread-safe)
vosk_lock = threading.Lock()

class StreamingSession:
    def __init__(self, sid, lang="en-us"):
        self.sid = sid
        self.language = lang
        self.lock = threading.Lock()
        with vosk_lock:
            self.recognizer = KaldiRecognizer(get_vosk_model(lang), 16000)
            self.recognizer.SetWords(True)
        self.all_words = []
        self.partial_text = ""
        self.active = True
        self.switching_language = False  # Flag to prevent processing during switch

    def reset_recognizer(self, lang):
        """Reset recognizer with a new language model"""
        self.switching_language = True  # Block audio processing
        self.language = lang
        # Create completely new recognizer with lock
        with vosk_lock:
            self.recognizer = KaldiRecognizer(get_vosk_model(lang), 16000)
            self.recognizer.SetWords(True)
        self.all_words = []
        self.partial_text = ""
        self.switching_language = False  # Allow audio processing again

    def process_audio(self, audio_data):
        """Process audio chunk and return any new words"""
        with self.lock:
            if not self.active or self.switching_language:
                return None, False
            try:
                with vosk_lock:
                    if self.recognizer.AcceptWaveform(audio_data):
                        # Final result for this segment
                        result = json.loads(self.recognizer.Result())
                        return result, True
                    else:
                        # Partial result (words being formed)
                        result = json.loads(self.recognizer.PartialResult())
                        return result, False
            except Exception as e:
                logger.error(f"Error in process_audio: {e}")
                return None, False

    def get_final_result(self):
        """Get final result when stream ends"""
        with self.lock:
            with vosk_lock:
                result = json.loads(self.recognizer.FinalResult())
                return result

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({
        "status": "ok",
        "model": "tiny",
        "streaming": True,
        "streaming_engine": "vosk"
    }), 200

@app.route('/transcribe', methods=['POST'])
def transcribe():
    """Transcribe audio file using Whisper (batch mode, higher quality)"""
    try:
        if 'audio' not in request.files:
            return jsonify({"error": "No audio file provided"}), 400

        audio_file = request.files['audio']

        with tempfile.NamedTemporaryFile(delete=False, suffix='.wav') as temp_file:
            temp_path = temp_file.name
            audio_file.save(temp_path)

        logger.info(f"Processing audio file: {temp_path}")

        segments, info = model.transcribe(
            temp_path,
            word_timestamps=True,
            vad_filter=True,
            vad_parameters=dict(min_silence_duration_ms=500)
        )

        words = []
        full_text = []

        for segment in segments:
            if hasattr(segment, 'words') and segment.words:
                for word in segment.words:
                    words.append({
                        "word": word.word.strip(),
                        "start": word.start,
                        "end": word.end,
                        "confidence": word.probability
                    })
                    full_text.append(word.word.strip())
            else:
                segment_words = segment.text.split()
                for word in segment_words:
                    words.append({
                        "word": word,
                        "start": segment.start,
                        "end": segment.end,
                        "confidence": 0.5
                    })
                    full_text.append(word)

        os.unlink(temp_path)

        return jsonify({
            "text": " ".join(full_text),
            "words": words,
            "language": info.language,
            "duration": info.duration
        }), 200

    except Exception as e:
        logger.error(f"Error during transcription: {str(e)}")
        if 'temp_path' in locals() and os.path.exists(temp_path):
            os.unlink(temp_path)
        return jsonify({"error": str(e)}), 500

# WebSocket events for real-time streaming with Vosk
@socketio.on('connect')
def handle_connect():
    logger.info(f"Client connected: {request.sid}")
    streaming_sessions[request.sid] = StreamingSession(request.sid, current_language)
    emit('connected', {'status': 'ready', 'engine': 'vosk', 'language': current_language})

@socketio.on('disconnect')
def handle_disconnect():
    logger.info(f"Client disconnected: {request.sid}")
    if request.sid in streaming_sessions:
        streaming_sessions[request.sid].active = False
        del streaming_sessions[request.sid]

@socketio.on('set_language')
def handle_set_language(data):
    """Change the language model for this session"""
    global current_language
    lang = data.get('language', 'en-us')
    sid = request.sid
    logger.info(f"Changing language to {lang} for session {sid}")

    try:
        # Mark session as inactive to stop audio processing immediately
        if sid in streaming_sessions:
            old_session = streaming_sessions[sid]
            old_session.active = False
            old_session.switching_language = True

            # Flush the recognizer to clear internal state
            try:
                old_session.recognizer.FinalResult()
            except:
                pass

            # Remove from dict
            del streaming_sessions[sid]

        # Pre-load the model (downloads if needed)
        get_vosk_model(lang)

        # Create completely new session with new language
        streaming_sessions[sid] = StreamingSession(sid, lang)
        current_language = lang

        emit('language_changed', {
            'language': lang,
            'model': f'vosk-model-small-{lang}',
            'status': 'ready'
        })
        logger.info(f"Language changed to {lang}")

    except Exception as e:
        logger.error(f"Failed to change language to {lang}: {e}")
        emit('language_changed', {
            'language': 'en-us',
            'error': str(e),
            'status': 'fallback'
        })

@socketio.on('audio_chunk')
def handle_audio_chunk(data):
    """Process audio chunk with Vosk for real-time results"""
    sid = request.sid
    if sid not in streaming_sessions:
        streaming_sessions[sid] = StreamingSession(sid, current_language)

    session = streaming_sessions[sid]

    # Skip processing if session is not active or switching languages
    if not session.active or session.switching_language:
        return

    audio_bytes = data.get('audio', b'')
    if isinstance(audio_bytes, list):
        audio_bytes = bytes(audio_bytes)

    # Process with Vosk
    result, is_final = session.process_audio(audio_bytes)

    # Skip if processing returned None (session inactive or error)
    if result is None:
        return

    if is_final:
        # Final result - emit confirmed words
        if 'result' in result:
            for word_info in result['result']:
                word_data = {
                    "word": word_info['word'],
                    "start": word_info.get('start', 0),
                    "end": word_info.get('end', 0),
                    "confidence": word_info.get('conf', 0.9),
                    "final": True
                }
                session.all_words.append(word_data)
                emit('word', word_data)
                logger.info(f"[FINAL] {word_info['word']}")
    else:
        # Partial result - emit as preview
        partial = result.get('partial', '')
        if partial and partial != session.partial_text:
            # Find new words in partial
            old_words = session.partial_text.split()
            new_words = partial.split()

            # Emit only newly appearing words as partial
            if len(new_words) > len(old_words):
                for word in new_words[len(old_words):]:
                    emit('partial', {"word": word, "text": partial})
                    logger.info(f"[PARTIAL] {word}")

            session.partial_text = partial

@socketio.on('end_stream')
def handle_end_stream():
    """Finalize transcription when stream ends"""
    sid = request.sid
    if sid in streaming_sessions:
        session = streaming_sessions[sid]

        # Get final result from Vosk
        final_result = session.get_final_result()

        if 'result' in final_result:
            for word_info in final_result['result']:
                word_data = {
                    "word": word_info['word'],
                    "start": word_info.get('start', 0),
                    "end": word_info.get('end', 0),
                    "confidence": word_info.get('conf', 0.9),
                    "final": True
                }
                session.all_words.append(word_data)
                emit('word', word_data)

        # Send completion
        emit('transcription_complete', {
            'text': ' '.join([w['word'] for w in session.all_words]),
            'words': session.all_words
        })

if __name__ == '__main__':
    # Pre-load Vosk model
    get_vosk_model()
    socketio.run(app, host='0.0.0.0', port=5000, debug=False)
