from flask import Flask, request, jsonify
from flask_cors import CORS
from faster_whisper import WhisperModel
import os
import tempfile
import logging

app = Flask(__name__)
CORS(app)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize Whisper model (tiny model for speed)
# Use CPU by default, set device="cuda" if you have NVIDIA GPU
logger.info("Loading Whisper model...")
model = WhisperModel("tiny", device="cpu", compute_type="int8")
logger.info("Whisper model loaded successfully")

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({"status": "ok", "model": "tiny"}), 200

@app.route('/transcribe', methods=['POST'])
def transcribe():
    """
    Transcribe audio file and return text with word-level timestamps and confidence scores
    """
    try:
        if 'audio' not in request.files:
            return jsonify({"error": "No audio file provided"}), 400

        audio_file = request.files['audio']

        # Save uploaded file temporarily
        with tempfile.NamedTemporaryFile(delete=False, suffix='.wav') as temp_file:
            temp_path = temp_file.name
            audio_file.save(temp_path)

        logger.info(f"Processing audio file: {temp_path}")

        # Transcribe with word timestamps
        segments, info = model.transcribe(
            temp_path,
            word_timestamps=True,
            vad_filter=True,  # Voice activity detection to filter out silence
            vad_parameters=dict(min_silence_duration_ms=500)
        )

        # Collect all words with their information
        words = []
        full_text = []

        for segment in segments:
            logger.info(f"Segment: {segment.text}")
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
                # Fallback if word timestamps not available
                segment_words = segment.text.split()
                for word in segment_words:
                    words.append({
                        "word": word,
                        "start": segment.start,
                        "end": segment.end,
                        "confidence": 0.5  # Default confidence
                    })
                    full_text.append(word)

        # Clean up temporary file
        os.unlink(temp_path)

        result = {
            "text": " ".join(full_text),
            "words": words,
            "language": info.language,
            "duration": info.duration
        }

        logger.info(f"Transcription complete: {len(words)} words")
        return jsonify(result), 200

    except Exception as e:
        logger.error(f"Error during transcription: {str(e)}")
        if 'temp_path' in locals() and os.path.exists(temp_path):
            os.unlink(temp_path)
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=False)
