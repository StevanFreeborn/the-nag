namespace TheNag.Terminal.Examples.ControlMapping;

internal sealed record MappingResult
{
  public List<ControlEvaluation> Evaluations { get; init; } = [];
}
