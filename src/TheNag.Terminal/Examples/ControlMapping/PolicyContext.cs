using TheNag.Terminal.Evaluation;

namespace TheNag.Terminal.Examples.ControlMapping;

internal sealed record PolicyContext : ITaskContext
{
  public string PolicyName { get; init; } = string.Empty;
  public string Content { get; init; } = string.Empty;

  public string ToContextString()
  {
    return $"Policy Name: {PolicyName}\nContent: {Content}";
  }
}