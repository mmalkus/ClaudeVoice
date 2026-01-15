# Claude Voice - AI Voice Transcription for Windows

A Windows desktop application that provides local voice-to-text transcription using OpenAI's Whisper model, with seamless integration into Claude Code via MCP (Model Context Protocol).

## Features

- **Local Transcription**: Uses Whisper AI for accurate speech-to-text conversion, running entirely on your machine
- **Interactive Word Editing**: Click on any transcribed word to correct it
- **Confidence Visualization**: Words are color-coded based on transcription confidence
  - Red: Low confidence (<70%)
  - Orange: Medium confidence (70-90%)
  - Dark: High confidence (>90%)
- **Real-time Audio Recording**: Record audio directly in the app with visual feedback
- **Claude Code Integration**: MCP server exposes transcription as a tool for Claude Code
- **Privacy-First**: All processing happens locally - no data sent to external servers

## Architecture

```
ClaudeVoiceApp/
├── WpfApp/              # Windows WPF application (C#)
│   ├── MainWindow.xaml  # UI layout
│   └── MainWindow.xaml.cs # Application logic
├── PythonBackend/       # Whisper transcription service
│   └── whisper_server.py # Flask API server
├── MCPServer/           # Model Context Protocol server
│   └── server.js        # MCP integration for Claude Code
└── Shared/              # Shared resources
```

## Requirements

### For the WPF Application:
- Windows 10 or later
- .NET 8.0 SDK or later
- Microphone access

### For the Python Backend:
- Python 3.8 or later
- pip (Python package manager)

### For Claude Code Integration:
- Node.js 18 or later
- npm (Node package manager)

## Installation

### 1. Install .NET SDK (if not already installed)

Download and install from: https://dotnet.microsoft.com/download

### 2. Set up Python Backend

```bash
cd ClaudeVoiceApp/PythonBackend
pip install -r requirements.txt
```

The first time you run the transcription, it will download the Whisper "tiny" model (~75MB).

### 3. Set up MCP Server

```bash
cd ClaudeVoiceApp/MCPServer
npm install
```

### 4. Build the WPF Application

```bash
cd ClaudeVoiceApp/WpfApp
dotnet restore
dotnet build
```

## Usage

### Running the Application

#### Option 1: Run from Visual Studio
1. Open `ClaudeVoiceApp/WpfApp/ClaudeVoice.csproj` in Visual Studio
2. Press F5 to build and run

#### Option 2: Run from command line
```bash
cd ClaudeVoiceApp/WpfApp
dotnet run
```

The application will automatically:
1. Start the Python Whisper backend on port 5000
2. Start the MCP server for Claude Code integration
3. Display the main window

### Using the App

1. **Start Recording**: Click "🎙️ Start Recording" and speak into your microphone
2. **Stop & Transcribe**: Click "⏹️ Stop" to end recording and begin transcription
3. **Edit Words**: Click on any word in the transcription to edit it
   - Press Enter to save changes
   - Press Escape to cancel
4. **Clear**: Click "🗑️ Clear" to remove the current transcription

### Integrating with Claude Code

To use the transcription tool in Claude Code, add the MCP server to your Claude Code configuration:

1. Create or edit `~/.claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "claude-voice": {
      "command": "node",
      "args": [
        "C:/path/to/ClaudeVoice/ClaudeVoiceApp/MCPServer/server.js"
      ]
    }
  }
}
```

2. Restart Claude Code

3. The transcription tools will now be available to Claude:
   - `transcribe_audio`: Transcribe an audio file
   - `get_transcription_status`: Check if the service is running

### Example Claude Code Usage

Once configured, you can ask Claude to transcribe audio files:

```
Claude, can you transcribe the audio file at C:\recordings\meeting.wav?
```

Claude will use the `transcribe_audio` tool and return the transcribed text with word-level confidence scores.

## Configuration

### Changing the Whisper Model

The default model is "tiny" for speed. To use a different model, edit `PythonBackend/whisper_server.py`:

```python
# Available models: tiny, base, small, medium, large
model = WhisperModel("base", device="cpu", compute_type="int8")
```

Model sizes:
- **tiny**: ~75MB, fastest, good for English
- **base**: ~140MB, better accuracy
- **small**: ~460MB, high accuracy
- **medium**: ~1.5GB, very high accuracy
- **large**: ~3GB, best accuracy

### GPU Acceleration

If you have an NVIDIA GPU, you can enable GPU acceleration for faster transcription:

1. Install CUDA Toolkit: https://developer.nvidia.com/cuda-downloads
2. Install cuBLAS and cuDNN
3. Modify `whisper_server.py`:

```python
model = WhisperModel("tiny", device="cuda", compute_type="float16")
```

## Troubleshooting

### "Python backend not starting"
- Ensure Python is installed and in your PATH
- Check that all dependencies are installed: `pip install -r requirements.txt`
- Manually start the backend to see errors: `python PythonBackend/whisper_server.py`

### "No microphone detected"
- Check Windows privacy settings to ensure microphone access is enabled
- Verify your microphone is working in other applications

### "MCP Server offline"
- Ensure Node.js is installed
- Check that dependencies are installed: `cd MCPServer && npm install`
- Manually start the server: `node MCPServer/server.js`

### "Transcription is slow"
- Consider using GPU acceleration (see Configuration section)
- The "tiny" model is fastest; larger models take longer but are more accurate
- Reduce background noise for better performance

## Development

### Project Structure

- **WpfApp**: C# WPF application using .NET 8.0
  - NAudio for audio recording
  - HttpClient for communication with Python backend

- **PythonBackend**: Flask REST API
  - faster-whisper for optimized transcription
  - Word-level timestamps and confidence scores

- **MCPServer**: TypeScript/Node.js MCP server
  - Exposes transcription capabilities to Claude Code
  - Follows Model Context Protocol specification

### Building for Release

```bash
cd ClaudeVoiceApp/WpfApp
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in `bin/Release/net8.0-windows/win-x64/publish/`

## License

MIT License - See LICENSE file for details

## Acknowledgments

- [OpenAI Whisper](https://github.com/openai/whisper) - Speech recognition model
- [faster-whisper](https://github.com/guillaumekln/faster-whisper) - Optimized Whisper implementation
- [Model Context Protocol](https://modelcontextprotocol.io/) - Claude Code integration standard
- [NAudio](https://github.com/naudio/NAudio) - Audio recording library
