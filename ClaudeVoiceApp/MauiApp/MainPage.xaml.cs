using ClaudeVoice.Models;
using ClaudeVoice.Services;
using System.Diagnostics;

namespace ClaudeVoice;

public partial class MainPage : ContentPage
{
	private readonly IAudioRecorder audioRecorder;
	private readonly ITranscriptionService transcriptionService;
	private string? currentRecordingPath;
	private Process? pythonBackendProcess;
	private Process? mcpServerProcess;

	public MainPage(IAudioRecorder audioRecorder, ITranscriptionService transcriptionService)
	{
		InitializeComponent();
		this.audioRecorder = audioRecorder;
		this.transcriptionService = transcriptionService;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await StartBackendServices();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopBackendServices();
	}

	private async Task StartBackendServices()
	{
		try
		{
			// Start Python backend
			var basePath = AppDomain.CurrentDomain.BaseDirectory;
			var pythonBackendPath = Path.Combine(basePath, "..", "..", "..", "..", "..", "PythonBackend");

			pythonBackendProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "python",
					Arguments = "whisper_server.py",
					WorkingDirectory = pythonBackendPath,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};

			pythonBackendProcess.Start();
			await Task.Delay(2000); // Wait for server to start

			// Start MCP server
			var mcpServerPath = Path.Combine(basePath, "..", "..", "..", "..", "..", "MCPServer");
			mcpServerProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "node",
					Arguments = "server.js",
					WorkingDirectory = mcpServerPath,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};

			mcpServerProcess.Start();
			await Task.Delay(1000);

			// Check MCP server status
			await CheckMcpServerStatus();
		}
		catch (Exception ex)
		{
			UpdateStatus($"Error starting services: {ex.Message}");
		}
	}

	private void StopBackendServices()
	{
		try
		{
			pythonBackendProcess?.Kill();
			mcpServerProcess?.Kill();
		}
		catch { }
	}

	private async Task CheckMcpServerStatus()
	{
		var isOnline = await transcriptionService.CheckHealthAsync();
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (isOnline)
			{
				McpStatusIndicator.BackgroundColor = Color.FromArgb("#27AE60"); // Green
				McpStatusLabel.Text = "Online";
			}
			else
			{
				McpStatusIndicator.BackgroundColor = Color.FromArgb("#E74C3C"); // Red
				McpStatusLabel.Text = "Offline";
			}
		});
	}

	private async void OnRecordClicked(object sender, EventArgs e)
	{
		try
		{
			// Request microphone permission
			var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<Permissions.Microphone>();
			}

			if (status != PermissionStatus.Granted)
			{
				await DisplayAlert("Permission Denied", "Microphone permission is required to record audio.", "OK");
				return;
			}

			currentRecordingPath = Path.Combine(FileSystem.CacheDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
			await audioRecorder.StartRecordingAsync(currentRecordingPath);

			RecordButton.IsEnabled = false;
			StopButton.IsEnabled = true;
			UpdateStatus("Recording... Speak now");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to start recording: {ex.Message}", "OK");
		}
	}

	private async void OnStopClicked(object sender, EventArgs e)
	{
		try
		{
			await audioRecorder.StopRecordingAsync();

			RecordButton.IsEnabled = true;
			StopButton.IsEnabled = false;
			UpdateStatus("Recording stopped, transcribing...");

			await TranscribeAudio();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to stop recording: {ex.Message}", "OK");
		}
	}

	private void OnClearClicked(object sender, EventArgs e)
	{
		TranscriptionPanel.Children.Clear();
		UpdateStatus("Transcription cleared");
	}

	private async Task TranscribeAudio()
	{
		if (string.IsNullOrEmpty(currentRecordingPath) || !File.Exists(currentRecordingPath))
		{
			await DisplayAlert("Error", "No recording found to transcribe.", "OK");
			return;
		}

		try
		{
			UpdateStatus("Transcribing audio...");

			var result = await transcriptionService.TranscribeAsync(currentRecordingPath);

			if (result?.Words != null && result.Words.Count > 0)
			{
				DisplayTranscription(result.Words);
				UpdateStatus($"Transcription complete - {result.Words.Count} words");
			}
			else
			{
				UpdateStatus("No transcription result");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to transcribe audio: {ex.Message}", "OK");
			UpdateStatus("Transcription failed");
		}
		finally
		{
			// Clean up the recording file
			if (File.Exists(currentRecordingPath))
			{
				try { File.Delete(currentRecordingPath); } catch { }
			}
		}
	}

	private void DisplayTranscription(List<WordInfo> words)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			TranscriptionPanel.Children.Clear();

			foreach (var wordInfo in words)
			{
				var button = new Button
				{
					Text = wordInfo.Word,
					Padding = new Thickness(5, 2),
					Margin = new Thickness(2),
					FontSize = 16,
					CornerRadius = 3,
					BackgroundColor = Colors.Transparent,
					BorderWidth = 0
				};

				// Color code by confidence
				if (wordInfo.Confidence < 0.7)
				{
					button.TextColor = Color.FromArgb("#E74C3C"); // Red for low confidence
				}
				else if (wordInfo.Confidence < 0.9)
				{
					button.TextColor = Color.FromArgb("#F39C12"); // Orange for medium confidence
				}
				else
				{
					button.TextColor = Color.FromArgb("#2C3E50"); // Dark for high confidence
				}

				button.Clicked += async (s, e) => await EditWord(button, wordInfo);

				TranscriptionPanel.Children.Add(button);
			}
		});
	}

	private async Task EditWord(Button button, WordInfo wordInfo)
	{
		var result = await DisplayPromptAsync(
			"Edit Word",
			$"Current: {wordInfo.Word}\nConfidence: {wordInfo.Confidence:P0}",
			"Save",
			"Cancel",
			wordInfo.Word,
			maxLength: 50,
			keyboard: Keyboard.Text,
			initialValue: wordInfo.Word
		);

		if (!string.IsNullOrEmpty(result))
		{
			wordInfo.Word = result;
			button.Text = result;
			UpdateStatus("Word updated");
		}
	}

	private void UpdateStatus(string message)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			StatusLabel.Text = message;
		});
	}
}
