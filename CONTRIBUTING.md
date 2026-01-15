# Contributing to Claude Voice

Thank you for your interest in contributing to Claude Voice! This document provides guidelines and instructions for contributing.

## Development Setup

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/ClaudeVoice.git
   cd ClaudeVoice
   ```

2. **Install Dependencies**
   Run the setup script:
   ```bash
   setup.bat
   ```

3. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Project Structure

```
ClaudeVoice/
├── ClaudeVoiceApp/
│   ├── WpfApp/           # C# WPF Desktop Application
│   ├── PythonBackend/    # Flask API with Whisper
│   ├── MCPServer/        # MCP integration server
│   └── Shared/           # Shared resources
├── setup.bat             # Installation script
├── run.bat               # Quick run script
└── README.md             # Main documentation
```

## Development Guidelines

### C# (WPF Application)

- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Use nullable reference types (`string?` for nullable strings)
- Keep UI logic in XAML when possible
- Use async/await for I/O operations
- Handle errors gracefully with try-catch blocks

### Python (Backend)

- Follow PEP 8 style guide
- Use type hints where appropriate
- Keep functions focused and small
- Add logging for debugging
- Handle exceptions properly

### TypeScript/JavaScript (MCP Server)

- Use ES6+ syntax
- Follow the MCP SDK conventions
- Add JSDoc comments for functions
- Handle errors with proper error messages

## Testing

Before submitting a pull request:

1. Test the WPF application:
   - Record audio successfully
   - Verify transcription accuracy
   - Test word editing functionality
   - Check MCP status indicator

2. Test the Python backend:
   - Verify /health endpoint
   - Test /transcribe endpoint with sample audio
   - Check error handling

3. Test the MCP server:
   - Verify tool registration
   - Test transcribe_audio tool
   - Test get_transcription_status tool

## Submitting Changes

1. **Commit Messages**
   - Use clear, descriptive commit messages
   - Format: `[Component] Brief description`
   - Example: `[WPF] Add volume meter to recording UI`

2. **Pull Request**
   - Provide a clear description of changes
   - Reference any related issues
   - Include screenshots for UI changes
   - Ensure all tests pass

3. **Code Review**
   - Be responsive to feedback
   - Make requested changes promptly
   - Keep discussions professional and constructive

## Feature Ideas

Some areas where contributions are welcome:

- **UI Enhancements**
  - Dark mode support
  - Customizable themes
  - Keyboard shortcuts
  - Waveform visualization during recording

- **Transcription Improvements**
  - Support for more audio formats
  - Real-time transcription while recording
  - Speaker diarization (multiple speakers)
  - Custom vocabulary/terminology

- **Integration Features**
  - Export to various formats (TXT, SRT, VTT)
  - Integration with other tools
  - Cloud sync options
  - Batch processing

- **Performance**
  - GPU acceleration
  - Model caching
  - Faster-whisper optimization
  - Memory usage improvements

## Bug Reports

When reporting bugs, please include:

- Operating system and version
- .NET, Python, and Node.js versions
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable
- Error messages or logs

## Questions?

Feel free to open an issue for:
- Feature requests
- Bug reports
- Documentation improvements
- General questions

Thank you for contributing!
