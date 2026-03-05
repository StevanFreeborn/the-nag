namespace TheNag.Terminal.Evaluation;

internal interface IScenario<TResult>
{
  string Name { get; }
  string InitialPrompt { get; }
  double TargetScore { get; }
  int MaxIterations { get; }
  IReadOnlyList<ITestCase<TResult>> TrainingCases { get; }
  IReadOnlyList<ITestCase<TResult>> ValidationCases { get; }
  IJudge<TResult> GetJudge();
  string GetMetaPrompt(string currentPrompt, string errorLog);
  IReadOnlyList<Iteration<TResult>> History { get; }
  void AddIteration(Iteration<TResult> iteration);

  bool IsSuccessful => History.Count > 0 && History[^1].TrainingScore >= TargetScore;
}