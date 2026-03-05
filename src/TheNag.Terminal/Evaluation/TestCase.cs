namespace TheNag.Terminal.Evaluation;

internal sealed class TestCase<TResult>(
  string name,
  ITaskContext context,
  TResult groundTruth
) : ITestCase<TResult>
{
  public string Name { get; } = name;
  public ITaskContext Context { get; } = context;
  public TResult GroundTruth { get; } = groundTruth;
}
