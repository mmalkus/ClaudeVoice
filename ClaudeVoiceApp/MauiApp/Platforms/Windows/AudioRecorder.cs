#if WINDOWS
using NAudio.Wave;

namespace ClaudeVoice.Services;

public class AudioRecorder : IAudioRecorder
{
	private WaveInEvent? waveIn;
	private WaveFileWriter? writer;
	private string? currentFilePath;

	public bool IsRecording => waveIn?.RecordingState == RecordingState.Recording;

	public Task StartRecordingAsync(string filePath)
	{
		if (IsRecording)
		{
			throw new InvalidOperationException("Already recording");
		}

		currentFilePath = filePath;

		waveIn = new WaveInEvent
		{
			WaveFormat = new WaveFormat(16000, 1) // 16kHz, Mono - optimal for Whisper
		};

		writer = new WaveFileWriter(filePath, waveIn.WaveFormat);

		waveIn.DataAvailable += (s, e) =>
		{
			writer?.Write(e.Buffer, 0, e.BytesRecorded);
		};

		waveIn.StartRecording();

		return Task.CompletedTask;
	}

	public Task StopRecordingAsync()
	{
		if (waveIn != null)
		{
			waveIn.StopRecording();
			waveIn.Dispose();
			waveIn = null;
		}

		if (writer != null)
		{
			writer.Dispose();
			writer = null;
		}

		return Task.CompletedTask;
	}
}
#endif
