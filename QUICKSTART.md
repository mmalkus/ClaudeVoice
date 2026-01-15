# Quick Start Guide

Get up and running with Claude Voice in 5 minutes!

## Prerequisites

Make sure you have these installed:
- [Python 3.11+](https://www.python.org/downloads/) (3.13 recommended)
- [Node.js 18+](https://nodejs.org/) (for MCP integration)

## Installation

### Windows Quick Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ClaudeVoice
   ```

2. **Run the setup script**
   ```bash
   setup.bat
   ```

   This will:
   - Create a Python virtual environment
   - Install dependencies (Flask, Vosk, Whisper, etc.)
   - Install Node.js dependencies for MCP
   - Download the Vosk English model (~50MB)

3. **Start the server**
   ```bash
   start_all.bat
   ```

4. **Open your browser**

   Go to: **http://localhost:5000**

That's it! You're ready to transcribe.

## First Use

1. **Select Language**
   - Use the dropdown to choose your language
   - First use of a language will download its model (~50MB)

2. **Start Recording**
   - Click the red record button
   - Allow microphone access when prompted

3. **Speak**
   - Words appear in real-time as you speak
   - Gray words = being recognized
   - White words = confirmed

4. **Stop Recording**
   - Click the button again to stop
   - Use "Copy" to copy the transcription

## Supported Languages

- English (US & Indian)
- German, Spanish, French, Italian
- Dutch, Portuguese, Russian
- Chinese, Japanese, Korean
- Turkish, Polish, Ukrainian
- Hindi, Arabic
- And more!

## Claude Code Integration

To use with Claude Code:

1. **Add to your MCP config**

   Edit `~/.claude/claude_desktop_config.json`:

   ```json
   {
     "mcpServers": {
       "claude-voice": {
         "command": "node",
         "args": ["C:/full/path/to/ClaudeVoice/ClaudeVoiceApp/MCPServer/server.js"]
       }
     }
   }
   ```

2. **Restart Claude Code**

3. **Test it**
   ```
   You: Claude, check the transcription service status
   Claude: The transcription service is online...
   ```

## Available MCP Tools

- `transcribe_audio` - Transcribe audio files (Whisper, high quality)
- `transcribe_audio_streaming` - Stream transcription from files
- `get_transcription_status` - Check service status

## Troubleshooting

### "Connection refused"
- Make sure the server is running: `start_all.bat`
- Check if port 5000 is available

### "No microphone access"
- Allow microphone permission in browser
- Check browser settings for site permissions

### "Model download failed"
- Check internet connection
- Try manually: https://alphacephei.com/vosk/models

### "Slow transcription"
- Use GPU acceleration for Whisper (edit `whisper_server.py`)
- Vosk streaming is real-time, Whisper batch is slower but more accurate

## Scripts Reference

| Script | Description |
|--------|-------------|
| `setup.bat` | Initial setup |
| `start_all.bat` | Start all services |
| `ClaudeVoiceApp\PythonBackend\start_server.bat` | Start server only |

## Next Steps

- Read the full [README.md](README.md) for configuration options
- Try different Whisper models for better batch accuracy
- Enable GPU acceleration for faster processing

Happy transcribing!
