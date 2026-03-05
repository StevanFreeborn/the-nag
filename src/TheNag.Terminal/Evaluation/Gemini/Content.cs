namespace TheNag.Terminal.Evaluation.Gemini;

internal sealed record Content
{
  public required Part[] Parts { get; init; }
}
