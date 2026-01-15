using ClaudeVoice.Models;
using Newtonsoft.Json;
using System.Net.Http;

namespace ClaudeVoice.Services;

public class TranscriptionService : ITranscriptionService
{
	private readonly HttpClient httpClient;
	private const string PYTHON_BACKEND_URL = "http://localhost:5000";

	public TranscriptionService()
	{
		httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromMinutes(5)
		};
	}

	public async Task<bool> CheckHealthAsync()
	{
		try
		{
			var response = await httpClient.GetAsync($"{PYTHON_BACKEND_URL}/health");
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	public async Task<TranscriptionResponse?> TranscribeAsync(string audioFilePath)
	{
		if (!File.Exists(audioFilePath))
		{
			throw new FileNotFoundException("Audio file not found", audioFilePath);
		}

		try
		{
			using var content = new MultipartFormDataContent();
			var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(audioFilePath));
			fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
			content.Add(fileContent, "audio", Path.GetFileName(audioFilePath));

			var response = await httpClient.PostAsync($"{PYTHON_BACKEND_URL}/transcribe", content);
			response.EnsureSuccessStatusCode();

			var jsonResponse = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<TranscriptionResponse>(jsonResponse);

			return result;
		}
		catch (Exception ex)
		{
			throw new Exception($"Transcription failed: {ex.Message}", ex);
		}
	}
}
