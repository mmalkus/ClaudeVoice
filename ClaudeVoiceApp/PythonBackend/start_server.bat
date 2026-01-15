@echo off
echo Starting Whisper Transcription Server...
call "%~dp0venv\Scripts\activate.bat"
python whisper_server.py
pause
