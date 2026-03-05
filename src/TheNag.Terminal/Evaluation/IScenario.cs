namespace TheNag.Terminal.Evaluation;

internal interface IScenario<TResult>
{
  string Name { get; }
  string InitialPrompt { get; }
  double TargetScore { get; }
  int MaxIterations { get; }
  ITaskContext Context { get; }
  TResult GroundTruth { get; }
  IJudge<TResult> GetJudge();
  string GetMetaPrompt(string currentPrompt, string errorLog);
  IReadOnlyList<Iteration<TResult>> History { get; }
  void AddIteration(Iteration<TResult> iteration);

  bool IsSuccessful => History.Count > 0 && History[^1].Score >= TargetScore;
}