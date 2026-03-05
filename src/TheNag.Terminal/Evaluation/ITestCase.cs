namespace TheNag.Terminal.Evaluation;

internal interface ITestCase<TResult>
{
  string Name { get; }
  ITaskContext Context { get; }
  TResult GroundTruth { get; }

}
