#if MACCATALYST || IOS
using AVFoundation;
using Foundation;

namespace ClaudeVoice.Services;

public class AudioRecorder : IAudioRecorder
{
	private AVAudioRecorder? audioRecorder;
	private string? currentFilePath;

	public bool IsRecording => audioRecorder?.Recording ?? false;

	public async Task StartRecordingAsync(string filePath)
	{
		if (IsRecording)
		{
			throw new InvalidOperationException("Already recording");
		}

		currentFilePath = filePath;

		// Configure audio session
		var audioSession = AVAudioSession.SharedInstance();
		var error = audioSession.SetCategory(AVAudioSessionCategory.Record);
		if (error != null)
		{
			throw new Exception($"Failed to set audio session category: {error.LocalizedDescription}");
		}

		error = audioSession.SetActive(true);
		if (error != null)
		{
			throw new Exception($"Failed to activate audio session: {error.LocalizedDescription}");
		}

		// Configure audio settings for Whisper (16kHz, Mono, PCM)
		var audioSettings = new AudioSettings
		{
			SampleRate = 16000,
			Format = AudioToolbox.AudioFormatType.LinearPCM,
			NumberChannels = 1,
			AudioQuality = AVAudioQuality.High
		};

		var url = NSUrl.FromFilename(filePath);
		audioRecorder = AVAudioRecorder.Create(url, audioSettings, out error);

		if (error != null || audioRecorder == null)
		{
			throw new Exception($"Failed to create audio recorder: {error?.LocalizedDescription}");
		}

		if (!audioRecorder.Record())
		{
			throw new Exception("Failed to start recording");
		}

		await Task.CompletedTask;
	}

	public async Task StopRecordingAsync()
	{
		if (audioRecorder != null && IsRecording)
		{
			audioRecorder.Stop();
			audioRecorder.Dispose();
			audioRecorder = null;
		}

		var audioSession = AVAudioSession.SharedInstance();
		audioSession.SetActive(false);

		await Task.CompletedTask;
	}
}
#endif
