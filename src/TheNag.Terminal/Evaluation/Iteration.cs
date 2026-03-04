namespace TheNag.Terminal.Evaluation;

internal sealed record Iteration<TResult>(
    int Number,
    string Prompt,
    double Score,
    string ErrorLog,
    TResult? RawResponse
);