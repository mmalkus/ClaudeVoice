# Claude Voice - Real-time Voice Transcription

A voice-to-text transcription application with real-time streaming, web interface, and Claude Code integration via MCP (Model Context Protocol).

## Features

- **Real-time Streaming**: Words appear as you speak using Vosk speech recognition
- **Web Interface**: Browser-based UI at `http://localhost:5000` - no desktop app needed
- **Dual Engine**:
  - **Vosk** for real-time streaming (low latency)
  - **Whisper** for batch transcription (higher accuracy)
- **Claude Code Integration**: MCP server exposes transcription tools
- **Privacy-First**: All processing happens locally - no data sent to external servers
- **GPU Support**: CUDA acceleration for faster Whisper transcription

## Architecture

```
ClaudeVoiceApp/
├── PythonBackend/           # Main server (Flask + SocketIO)
│   ├── whisper_server.py    # API server with streaming support
│   ├── templates/
│   │   └── index.html       # Web UI
│   ├── requirements.txt     # Python dependencies
│   └── venv/                # Virtual environment
├── MCPServer/               # Model Context Protocol server
│   └── server.js            # MCP integration for Claude Code
└── WpfApp/                  # Optional Windows desktop app (requires .NET)
```

## Requirements

- **Python 3.11+** (3.13 recommended)
- **Node.js 18+** (for MCP server)
- **NVIDIA GPU** (optional, for faster Whisper transcription)

## Quick Start

### 1. Run Setup

```bash
setup.bat
```

This will:
- Create a Python virtual environment
- Install all dependencies (Flask, Vosk, Whisper, etc.)
- Install Node.js dependencies for MCP
- Download the Vosk model (~50MB, first run only)

### 2. Start the Server

```bash
start_all.bat
```

Or manually:
```bash
cd ClaudeVoiceApp\PythonBackend
venv\Scripts\activate
python whisper_server.py
```

### 3. Open the Web Interface

Open your browser to: **http://localhost:5000**

- Click the record button to start
- Speak into your microphone
- Words appear in real-time
- Click again to stop

## Usage

### Web Interface (Recommended)

1. Start the server: `start_all.bat`
2. Open http://localhost:5000
3. Click the red button to record
4. Speak - words appear as you talk
5. Click again to stop
6. Use "Copy" to copy the transcription

### MCP Tools (for Claude Code)

Add to your Claude Code MCP configuration (`~/.claude/claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "claude-voice": {
      "command": "node",
      "args": ["C:/path/to/ClaudeVoice/ClaudeVoiceApp/MCPServer/server.js"]
    }
  }
}
```

Available tools:
- `transcribe_audio` - Batch transcription with Whisper (higher quality)
- `transcribe_audio_streaming` - Streaming transcription
- `get_transcription_status` - Check service status

### REST API

**Health Check:**
```
GET http://localhost:5000/health
```

**Batch Transcription:**
```
POST http://localhost:5000/transcribe
Content-Type: multipart/form-data
Body: audio=<audio file>
```

**WebSocket Streaming:**
Connect to `ws://localhost:5000` and emit:
- `audio_chunk` - Send audio data (16-bit PCM, 16kHz, mono)
- `end_stream` - Finalize transcription

Listen for:
- `word` - Confirmed word with timestamp
- `partial` - Word being formed (preview)
- `transcription_complete` - Final result

## Configuration

### Whisper Model

Edit `PythonBackend/whisper_server.py` to change the model:

```python
# Available: tiny, base, small, medium, large
model = WhisperModel("tiny", device="cuda", compute_type="int8")
```

| Model | Size | Speed | Accuracy |
|-------|------|-------|----------|
| tiny | 75MB | Fastest | Good for English |
| base | 140MB | Fast | Better accuracy |
| small | 460MB | Medium | High accuracy |
| medium | 1.5GB | Slow | Very high accuracy |
| large | 3GB | Slowest | Best accuracy |

### GPU Acceleration

For NVIDIA GPUs, change device to `cuda`:

```python
model = WhisperModel("tiny", device="cuda", compute_type="float16")
```

Requirements:
- CUDA Toolkit
- cuBLAS and cuDNN

### Vosk Model

The default English model is downloaded automatically. For other languages, download from [Vosk Models](https://alphacephei.com/vosk/models) and place in `PythonBackend/vosk-model/`.

## Scripts

| Script | Description |
|--------|-------------|
| `setup.bat` | Initial setup (venv, dependencies) |
| `start_all.bat` | Start all services |
| `run.bat` | Start WPF app (requires .NET) |
| `ClaudeVoiceApp\PythonBackend\start_server.bat` | Start Python backend only |

## Troubleshooting

### "Connection refused" / Server not starting
- Check if port 5000 is in use: `netstat -an | findstr 5000`
- Start server manually to see errors:
  ```bash
  cd ClaudeVoiceApp\PythonBackend
  venv\Scripts\activate
  python whisper_server.py
  ```

### "No microphone access" in browser
- Allow microphone permission when prompted
- Check browser settings for site permissions
- Try a different browser (Chrome/Edge recommended)

### "Vosk model not found"
- Delete `vosk-model` folder and restart - it will re-download
- Or manually download from https://alphacephei.com/vosk/models

### Transcription is slow
- Use GPU acceleration (see Configuration)
- Use the "tiny" Whisper model
- Vosk streaming is faster than Whisper batch mode

### MCP Server offline
- Ensure Python backend is running first
- Check Node.js is installed: `node --version`
- Reinstall dependencies: `cd ClaudeVoiceApp\MCPServer && npm install`

## Development

### Tech Stack
- **Backend**: Python, Flask, Flask-SocketIO
- **Speech Recognition**: Vosk (streaming), faster-whisper (batch)
- **Frontend**: Vanilla HTML/CSS/JS, Socket.IO client
- **MCP**: Node.js, @modelcontextprotocol/sdk

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Web interface |
| `/health` | GET | Health check |
| `/transcribe` | POST | Batch transcription |
| WebSocket | - | Real-time streaming |

## License

MIT License - See LICENSE file for details

## Acknowledgments

- [Vosk](https://alphacephei.com/vosk/) - Real-time speech recognition
- [OpenAI Whisper](https://github.com/openai/whisper) - High-quality transcription
- [faster-whisper](https://github.com/guillaumekln/faster-whisper) - Optimized Whisper
- [Model Context Protocol](https://modelcontextprotocol.io/) - Claude Code integration
