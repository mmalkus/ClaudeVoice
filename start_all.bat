@echo off
echo Starting Claude Voice Services...
echo.

echo [1/2] Starting Python Whisper Backend...
start "Whisper Backend" cmd /c "cd /d %~dp0ClaudeVoiceApp\PythonBackend && call venv\Scripts\activate.bat && python whisper_server.py"

echo Waiting for backend to initialize...
timeout /t 3 /nobreak >nul

echo [2/2] Starting MCP Server...
start "MCP Server" cmd /c "cd /d %~dp0ClaudeVoiceApp\MCPServer && node server.js"

echo.
echo ===============================================
echo Claude Voice services started!
echo ===============================================
echo.
echo - Whisper Backend: http://localhost:5000
echo - MCP Server: Running on stdio
echo.
echo Close this window to keep services running,
echo or press any key to stop all services.
pause >nul

echo Stopping services...
taskkill /FI "WINDOWTITLE eq Whisper Backend*" >nul 2>&1
taskkill /FI "WINDOWTITLE eq MCP Server*" >nul 2>&1
echo Done.
