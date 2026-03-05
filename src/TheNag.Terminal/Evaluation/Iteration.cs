namespace TheNag.Terminal.Evaluation;

internal sealed record Iteration<TResult>(
    int Number,
    string Prompt,
    double TrainingScore,
    double ValidationScore,
    string ErrorLog,
    IReadOnlyList<TestCaseResult<TResult>> TrainingResults,
    IReadOnlyList<TestCaseResult<TResult>> ValidationResults
);

internal sealed record TestCaseResult<TResult>(
    string TestCaseName,
    double Score,
    string ErrorLog,
    TResult? RawResponse
);