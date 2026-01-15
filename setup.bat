@echo off
echo ===============================================
echo Claude Voice - Setup Script
echo ===============================================
echo.

echo [1/4] Checking prerequisites...
echo.

REM Check for .NET
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Please install .NET 8.0 or later from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
echo   .NET SDK: Found

REM Check for Python
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python not found. Please install Python 3.8 or later from https://www.python.org/downloads/
    pause
    exit /b 1
)
echo   Python: Found

REM Check for Node.js
node --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Node.js not found. Please install Node.js 18 or later from https://nodejs.org/
    pause
    exit /b 1
)
echo   Node.js: Found

echo.
echo [2/4] Installing Python dependencies...
cd ClaudeVoiceApp\PythonBackend
pip install -r requirements.txt
if errorlevel 1 (
    echo ERROR: Failed to install Python dependencies
    pause
    exit /b 1
)
cd ..\..

echo.
echo [3/4] Installing Node.js dependencies...
cd ClaudeVoiceApp\MCPServer
call npm install
if errorlevel 1 (
    echo ERROR: Failed to install Node.js dependencies
    pause
    exit /b 1
)
cd ..\..

echo.
echo [4/4] Building WPF application...
cd ClaudeVoiceApp\WpfApp
dotnet restore
dotnet build
if errorlevel 1 (
    echo ERROR: Failed to build WPF application
    pause
    exit /b 1
)
cd ..\..

echo.
echo ===============================================
echo Setup completed successfully!
echo ===============================================
echo.
echo To run the application:
echo   1. Run: run.bat
echo   OR
echo   2. Open ClaudeVoiceApp\WpfApp\ClaudeVoice.csproj in Visual Studio and press F5
echo.
echo To integrate with Claude Code:
echo   Add this to your Claude Code MCP configuration:
echo.
echo   {
echo     "mcpServers": {
echo       "claude-voice": {
echo         "command": "node",
echo         "args": ["%CD%\\ClaudeVoiceApp\\MCPServer\\server.js"]
echo       }
echo     }
echo   }
echo.
pause
