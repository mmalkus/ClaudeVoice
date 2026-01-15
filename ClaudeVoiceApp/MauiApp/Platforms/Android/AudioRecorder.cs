#if ANDROID
using Android.Media;

namespace ClaudeVoice.Services;

public class AudioRecorder : IAudioRecorder
{
	private MediaRecorder? mediaRecorder;
	private string? currentFilePath;

	public bool IsRecording { get; private set; }

	public Task StartRecordingAsync(string filePath)
	{
		if (IsRecording)
		{
			throw new InvalidOperationException("Already recording");
		}

		currentFilePath = filePath;

		try
		{
			mediaRecorder = new MediaRecorder();
			mediaRecorder.SetAudioSource(AudioSource.Mic);
			mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
			mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
			mediaRecorder.SetAudioSamplingRate(16000);
			mediaRecorder.SetAudioChannels(1);
			mediaRecorder.SetOutputFile(filePath);
			mediaRecorder.Prepare();
			mediaRecorder.Start();

			IsRecording = true;

			return Task.CompletedTask;
		}
		catch (Exception ex)
		{
			mediaRecorder?.Release();
			mediaRecorder = null;
			throw new Exception($"Failed to start recording: {ex.Message}", ex);
		}
	}

	public Task StopRecordingAsync()
	{
		if (mediaRecorder != null && IsRecording)
		{
			try
			{
				mediaRecorder.Stop();
				mediaRecorder.Release();
			}
			catch { }
			finally
			{
				mediaRecorder = null;
				IsRecording = false;
			}
		}

		return Task.CompletedTask;
	}
}
#endif
