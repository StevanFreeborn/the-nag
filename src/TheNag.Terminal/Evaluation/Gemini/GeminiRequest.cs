namespace TheNag.Terminal.Evaluation.Gemini;

internal sealed record GeminiRequest
{
  public Content? SystemInstruction { get; init; }
  public required Content[] Contents { get; init; }
  public GenerationConfig? GenerationConfig { get; init; }
}
