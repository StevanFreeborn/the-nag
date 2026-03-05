using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;

using TheNag.Terminal.Evaluation.Gemini;

namespace TheNag.Terminal.Evaluation;

internal sealed class Optimizer(
  GeminiService gemini,
  IFileSystem fileSystem
)
{
  private readonly GeminiService _gemini = gemini;
  private readonly IFileSystem _fileSystem = fileSystem;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public async Task<string> RunAsync<TResult>(
    IScenario<TResult> scenario, 
    CancellationToken cancellationToken = default
  )
  {
    var judge = scenario.GetJudge();
    var currentPrompt = scenario.InitialPrompt;
    double bestTrainingScore = 0;
    var optimalPrompt = currentPrompt;

    foreach (var iteration in Enumerable.Range(1, scenario.MaxIterations))
    {
      Console.WriteLine($"\n=== Iteration {iteration} ===");

      Console.WriteLine("Training:");
      var trainingResults = await EvaluateTestCasesAsync(
        currentPrompt, 
        scenario.TrainingCases, 
        judge, 
        cancellationToken
      );
      var trainingScore = trainingResults.Average(r => r.Score);
      Console.WriteLine($"  Average: {trainingScore:F2}%");

      Console.WriteLine("\nValidation:");
      var validationResults = await EvaluateTestCasesAsync(
        currentPrompt, 
        scenario.ValidationCases, 
        judge, 
        cancellationToken
      );
      var validationScore = validationResults.Average(r => r.Score);
      Console.WriteLine($"  Average: {validationScore:F2}%");

      var combinedErrorLog = string.Join("\n\n", trainingResults
        .Where(r => !string.IsNullOrEmpty(r.ErrorLog))
        .Select(r => $"[{r.TestCaseName}]\n{r.ErrorLog}"));

      scenario.AddIteration(new(
        Number: iteration,
        Prompt: currentPrompt,
        TrainingScore: trainingScore,
        ValidationScore: validationScore,
        ErrorLog: combinedErrorLog,
        TrainingResults: trainingResults,
        ValidationResults: validationResults
      ));

      if (trainingScore > bestTrainingScore)
      {
        bestTrainingScore = trainingScore;
        optimalPrompt = currentPrompt;
      }

      if (trainingScore >= scenario.TargetScore)
      {
        break;
      }

      var metaPrompt = scenario.GetMetaPrompt(currentPrompt, combinedErrorLog);
      currentPrompt = await _gemini.RefinePromptAsync(metaPrompt, cancellationToken);
    }

    await PersistResultsToDisk(optimalPrompt, scenario, cancellationToken);
    return optimalPrompt;
  }

  private async Task<IReadOnlyList<TestCaseResult<TResult>>> EvaluateTestCasesAsync<TResult>(
    string prompt,
    IReadOnlyList<ITestCase<TResult>> testCases,
    IJudge<TResult> judge,
    CancellationToken cancellationToken
  )
  {
    var results = new List<TestCaseResult<TResult>>();

    foreach (var testCase in testCases)
    {
      try
      {
        var jsonResponse = await _gemini.GetStructuredResponseAsync(
          prompt,
          testCase.Context.ToContextString(),
          judge.GetJsonSchema(),
          cancellationToken
        );

        var aiOutput = JsonSerializer.Deserialize<TResult>(jsonResponse);

        if (aiOutput is null)
        {
          Console.WriteLine($"  - {testCase.Name}: ERROR - Failed to parse AI output");
          results.Add(new(
            TestCaseName: testCase.Name,
            Score: 0,
            ErrorLog: "Failed to parse AI output",
            RawResponse: default
          ));
          continue;
        }

        var eval = judge.Evaluate(aiOutput, testCase.GroundTruth);
        Console.WriteLine($"  - {testCase.Name}: {eval.FinalScore:F2}%");

        results.Add(new(
          TestCaseName: testCase.Name,
          Score: eval.FinalScore,
          ErrorLog: eval.DetailedErrorLog,
          RawResponse: aiOutput
        ));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"  - {testCase.Name}: ERROR - {ex.Message}");

        results.Add(new(
          TestCaseName: testCase.Name,
          Score: 0,
          ErrorLog: $"Exception during evaluation: {ex.Message}",
          RawResponse: default
        ));
      }
    }

    return results;
  }

  private async Task PersistResultsToDisk<TResult>(
    string finalPrompt, 
    IScenario<TResult> scenario, 
    CancellationToken cancellationToken
  )
  {
    if (scenario.History.Count == 0)
    {
      Console.WriteLine("[Warning] No iterations to persist. Skipping report generation.");
      return;
    }

    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
    var directory = _fileSystem.Path.Combine(AppContext.BaseDirectory, "sessions", timestamp);
    _fileSystem.Directory.CreateDirectory(directory);

    var sb = new StringBuilder();
    sb.AppendLine(CultureInfo.InvariantCulture, $"# Optimization Session: {timestamp}");
    sb.AppendLine();
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Final Status**: {(scenario.IsSuccessful ? "Success" : "Incomplete")}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Total Iterations**: {scenario.History.Count}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Best Training Score**: {scenario.History.Max(i => i.TrainingScore):F2}%");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Best Validation Score**: {scenario.History.Max(i => i.ValidationScore):F2}%");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Training Cases**: {scenario.TrainingCases.Count}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Validation Cases**: {scenario.ValidationCases.Count}");
    sb.AppendLine();

    sb.AppendLine("## Final Optimized Prompt");
    sb.AppendLine();
    sb.AppendLine("```text");
    sb.AppendLine(finalPrompt);
    sb.AppendLine("```");
    sb.AppendLine();
    sb.AppendLine("---");
    sb.AppendLine();

    foreach (var iter in scenario.History)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"## Iteration {iter.Number}");
      sb.AppendLine();
      sb.AppendLine(CultureInfo.InvariantCulture, $"> **Training Score**: {iter.TrainingScore:F2}%  ");
      sb.AppendLine(CultureInfo.InvariantCulture, $"> **Validation Score**: {iter.ValidationScore:F2}%");
      sb.AppendLine();

      sb.AppendLine("### Prompt Used");
      sb.AppendLine();
      sb.AppendLine("```text");
      sb.AppendLine(iter.Prompt);
      sb.AppendLine("```");
      sb.AppendLine();

      sb.AppendLine("### Training Results");
      sb.AppendLine();
      foreach (var result in iter.TrainingResults)
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"#### {result.TestCaseName}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"> **Score**: {result.Score:F2}%");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(result.ErrorLog))
        {
          sb.AppendLine("**Issues:**");
          sb.AppendLine();
          sb.AppendLine(result.ErrorLog);
          sb.AppendLine();
        }

        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Raw Response</summary>");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine(JsonSerializer.Serialize(result.RawResponse, JsonOptions));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("</details>");
        sb.AppendLine();
      }

      sb.AppendLine("### Validation Results");
      sb.AppendLine();
      foreach (var result in iter.ValidationResults)
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"#### {result.TestCaseName}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"> **Score**: {result.Score:F2}%");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(result.ErrorLog))
        {
          sb.AppendLine("<details>");
          sb.AppendLine("<summary>Issues</summary>");
          sb.AppendLine();
          sb.AppendLine(result.ErrorLog);
          sb.AppendLine();
          sb.AppendLine("</details>");
          sb.AppendLine();
        }

        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Raw Response</summary>");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine(JsonSerializer.Serialize(result.RawResponse, JsonOptions));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("</details>");
        sb.AppendLine();
      }

      sb.AppendLine("---");
      sb.AppendLine();
    }

    var filePath = _fileSystem.Path.Combine(directory, "report.md");
    await _fileSystem.File.WriteAllTextAsync(filePath, sb.ToString(), cancellationToken);
    Console.WriteLine($"\n[System] Session report persisted to: {filePath}");
  }
}