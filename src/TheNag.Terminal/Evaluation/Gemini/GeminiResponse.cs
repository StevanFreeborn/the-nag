namespace TheNag.Terminal.Evaluation.Gemini;

internal sealed record GeminiResponse
{
  public Candidate[]? Candidates { get; init; }
}
