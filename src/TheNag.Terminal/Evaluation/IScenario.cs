namespace TheNag.Terminal.Evaluation;

internal interface IScenario<TResult>
{
  string Name { get; }
  string InitialPrompt { get; }
  double TargetScore { get; }
  ITaskContext Context { get; }
  TResult GroundTruth { get; }
  IJudge<TResult> GetJudge();
  IReadOnlyList<Iteration<TResult>> History { get; }
  void AddIteration(Iteration<TResult> iteration);
}