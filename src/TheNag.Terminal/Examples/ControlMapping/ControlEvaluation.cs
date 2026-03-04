namespace TheNag.Terminal.Examples.ControlMapping;

internal sealed record ControlEvaluation
{
  public string ControlId { get; init; } = string.Empty;
  public string Status { get; init; } = string.Empty;
  public string Quote { get; init; } = string.Empty;
  public string Reasoning { get; init; } = string.Empty;
}