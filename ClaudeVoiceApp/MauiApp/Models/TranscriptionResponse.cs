namespace ClaudeVoice.Models;

public class TranscriptionResponse
{
	public string? Text { get; set; }
	public List<WordInfo>? Words { get; set; }
	public string? Language { get; set; }
	public double Duration { get; set; }
}
