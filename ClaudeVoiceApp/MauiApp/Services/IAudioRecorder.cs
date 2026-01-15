namespace ClaudeVoice.Services;

public interface IAudioRecorder
{
	Task StartRecordingAsync(string filePath);
	Task StopRecordingAsync();
	bool IsRecording { get; }
}
