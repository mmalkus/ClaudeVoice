@echo off
echo Starting Live Voice Transcription...
echo.
echo Make sure the whisper server is running first (start_server.bat)
echo.
call "%~dp0venv\Scripts\activate.bat"
python "%~dp0live_recorder.py"
pause
