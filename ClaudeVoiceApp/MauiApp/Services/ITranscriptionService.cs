using ClaudeVoice.Models;

namespace ClaudeVoice.Services;

public interface ITranscriptionService
{
	Task<TranscriptionResponse?> TranscribeAsync(string audioFilePath);
	Task<bool> CheckHealthAsync();
}
