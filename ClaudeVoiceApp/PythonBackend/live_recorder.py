"""
Live Voice Recorder with Real-time Streaming Transcription
Records from microphone and streams words as you speak.

Usage: python live_recorder.py
Press Ctrl+C to stop recording.
"""

import pyaudio
import socketio
import threading
import sys
import time

# Audio settings
CHUNK = 1600  # 100ms at 16kHz
FORMAT = pyaudio.paInt16
CHANNELS = 1
RATE = 16000

# Server connection
SERVER_URL = "http://localhost:5000"

class LiveRecorder:
    def __init__(self):
        self.sio = socketio.Client()
        self.audio = pyaudio.PyAudio()
        self.stream = None
        self.recording = False
        self.transcript = []

        # Setup socket events
        self.sio.on('connect', self.on_connect)
        self.sio.on('connected', self.on_ready)
        self.sio.on('word', self.on_word)
        self.sio.on('transcription_complete', self.on_complete)
        self.sio.on('disconnect', self.on_disconnect)

    def on_connect(self):
        print("\n[Connected to transcription server]")

    def on_ready(self, data):
        print("[Server ready - Start speaking!]\n")
        print("-" * 50)
        print("LIVE TRANSCRIPTION:")
        print("-" * 50)

    def on_word(self, data):
        word = data.get('word', '')
        confidence = data.get('confidence', 0)

        # Print word inline (no newline for streaming effect)
        if confidence >= 0.7:
            sys.stdout.write(f"{word} ")
        else:
            sys.stdout.write(f"[{word}?] ")
        sys.stdout.flush()

        self.transcript.append(word)

    def on_complete(self, data):
        print("\n" + "-" * 50)
        print("\n[Transcription complete]")
        full_text = data.get('text', ' '.join(self.transcript))
        print(f"\nFinal text: {full_text}")
        print(f"Total words: {len(data.get('words', self.transcript))}")

    def on_disconnect(self):
        print("\n[Disconnected from server]")

    def start_recording(self):
        """Start recording and streaming audio"""
        print("=" * 50)
        print("LIVE VOICE TRANSCRIPTION")
        print("=" * 50)
        print("\nConnecting to server...")

        try:
            self.sio.connect(SERVER_URL, transports=['websocket'])
        except Exception as e:
            print(f"\nError: Could not connect to server at {SERVER_URL}")
            print("Make sure the whisper server is running (start_server.bat)")
            return

        # Open audio stream
        try:
            self.stream = self.audio.open(
                format=FORMAT,
                channels=CHANNELS,
                rate=RATE,
                input=True,
                frames_per_buffer=CHUNK
            )
        except Exception as e:
            print(f"\nError: Could not open microphone: {e}")
            print("Make sure a microphone is connected and accessible.")
            self.sio.disconnect()
            return

        self.recording = True
        print("\n[Recording... Press Ctrl+C to stop]\n")

        # Record and stream audio
        try:
            while self.recording:
                try:
                    audio_data = self.stream.read(CHUNK, exception_on_overflow=False)
                    # Send audio chunk to server
                    self.sio.emit('audio_chunk', {'audio': list(audio_data)})
                except Exception as e:
                    if self.recording:
                        print(f"\nAudio error: {e}")
                    break
        except KeyboardInterrupt:
            pass

        self.stop_recording()

    def stop_recording(self):
        """Stop recording and finalize transcription"""
        self.recording = False

        print("\n\n[Stopping recording...]")

        # Close audio stream
        if self.stream:
            self.stream.stop_stream()
            self.stream.close()
            self.stream = None

        # Tell server we're done
        if self.sio.connected:
            self.sio.emit('end_stream')
            # Wait a moment for final transcription
            time.sleep(1)
            self.sio.disconnect()

        # Cleanup
        self.audio.terminate()

def main():
    recorder = LiveRecorder()
    recorder.start_recording()
    print("\nDone!")

if __name__ == '__main__':
    main()
