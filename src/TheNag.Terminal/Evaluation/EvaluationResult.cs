namespace TheNag.Terminal.Evaluation;

internal sealed record EvaluationResult
{
  public double FinalScore { get; init; }
  public string DetailedErrorLog { get; init; } = string.Empty;
}