# Quick Start Guide

Get up and running with Claude Voice in 5 minutes!

## Prerequisites

Make sure you have these installed:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Python 3.8+](https://www.python.org/downloads/)
- [Node.js 18+](https://nodejs.org/)

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
   - Install Python dependencies (Flask, faster-whisper)
   - Install Node.js dependencies (MCP SDK)
   - Build the WPF application

3. **Start the application**
   ```bash
   run.bat
   ```

That's it! The application will start with all services running.

## First Use

1. **Record Audio**
   - Click the "🎙️ Start Recording" button
   - Speak clearly into your microphone
   - Click "⏹️ Stop" when done

2. **View Transcription**
   - The transcription appears automatically
   - Words are color-coded by confidence:
     - **Red**: Low confidence - likely needs correction
     - **Orange**: Medium confidence
     - **Dark**: High confidence

3. **Edit Words**
   - Click any word to edit it
   - Type the correction
   - Press Enter to save or Escape to cancel

4. **Copy to Claude**
   - Select and copy the text
   - Paste into Claude Code or any other application

## Claude Code Integration

To use with Claude Code:

1. **Locate the MCP server path**
   - It's at: `ClaudeVoice/ClaudeVoiceApp/MCPServer/server.js`

2. **Update Claude Code config**

   Open or create `~/.claude/claude_desktop_config.json`:

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

3. **Restart Claude Code**

4. **Test it**
   ```
   You: Claude, what tools do you have available?
   Claude: I have access to the claude-voice tools including transcribe_audio...
   ```

## Troubleshooting

### "No microphone found"
- Check Windows Settings → Privacy → Microphone
- Ensure your microphone is plugged in and working

### "Python backend offline"
- Manually start: `cd ClaudeVoiceApp/PythonBackend && python whisper_server.py`
- Check for error messages
- Ensure all dependencies installed: `pip install -r requirements.txt`

### "Transcription is slow"
- Normal on first run (downloads model ~75MB)
- Subsequent runs are faster
- Consider using GPU acceleration (see README.md)

### "MCP server not working"
- Verify Node.js installed: `node --version`
- Check dependencies: `cd ClaudeVoiceApp/MCPServer && npm install`
- Verify config path is absolute, not relative

## Next Steps

- Read the full [README.md](README.md) for advanced configuration
- Check out [CONTRIBUTING.md](CONTRIBUTING.md) to contribute
- Try different Whisper models for better accuracy (see README)
- Enable GPU acceleration for faster transcription

## Tips

- **Short recordings work best**: Under 30 seconds for fastest results
- **Speak clearly**: Reduces low-confidence words
- **Quiet environment**: Background noise affects accuracy
- **Check the status**: Green indicator = all services running
- **Edit immediately**: Fix red/orange words right away

## Need Help?

- Check the full README: [README.md](README.md)
- Report issues on GitHub
- Review error logs in the application

Happy transcribing! 🎙️
