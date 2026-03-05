using System.Text.Json;

namespace TheNag.Terminal.Evaluation.Gemini;

internal sealed record GenerationConfig
{
  public string? ResponseMimeType { get; init; }
  public JsonElement? ResponseSchema { get; init; }
  public double? Temperature { get; init; }
  public double? TopP { get; init; }
}
