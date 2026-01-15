namespace ClaudeVoice.Models;

public class WordInfo
{
	public string Word { get; set; } = "";
	public double Start { get; set; }
	public double End { get; set; }
	public double Confidence { get; set; }
}
