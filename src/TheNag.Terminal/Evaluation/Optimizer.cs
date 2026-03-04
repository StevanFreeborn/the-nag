using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;

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

  public async Task<string> RunAsync<TResult>(IScenario<TResult> scenario)
  {
    var judge = scenario.GetJudge();
    var currentPrompt = scenario.InitialPrompt;
    double bestScore = 0;
    var optimalPrompt = currentPrompt;

    foreach (var iteration in Enumerable.Range(1, 5))
    {
      Console.WriteLine($"--- Iteration {iteration} ---");

      var jsonResponse = await _gemini.GetStructuredResponseAsync(currentPrompt, scenario.Context.ToContextString(), judge.GetJsonSchema());
      var aiOutput = JsonSerializer.Deserialize<TResult>(jsonResponse);

      if (aiOutput is null)
      {
        Console.WriteLine("Failed to parse AI output. Skipping evaluation.");
        continue;
      }

      var eval = judge.Evaluate(aiOutput, scenario.GroundTruth);
      Console.WriteLine($"Score: {eval.FinalScore}%");

      if (eval.FinalScore > bestScore)
      {
        bestScore = eval.FinalScore;
        optimalPrompt = currentPrompt;
      }

      currentPrompt = await _gemini.RefinePromptAsync(currentPrompt, eval.DetailedErrorLog);

      scenario.AddIteration(new(
        Number: iteration,
        Prompt: currentPrompt,
        Score: eval.FinalScore,
        ErrorLog: eval.DetailedErrorLog,
        RawResponse: aiOutput
      ));
    }

    await PersistResultsToDisk(optimalPrompt, scenario.History);
    return optimalPrompt;
  }

  private async Task PersistResultsToDisk<TResult>(string finalPrompt, IReadOnlyList<Iteration<TResult>> history)
  {
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
    var directory = _fileSystem.Path.Combine(AppContext.BaseDirectory, "sessions", timestamp);
    _fileSystem.Directory.CreateDirectory(directory);

    var sb = new StringBuilder();
    sb.AppendLine(CultureInfo.InvariantCulture, $"# Optimization Session: {timestamp}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Final Status**: {(history[history.Count - 1].Score >= 95 ? "Success" : "Incomplete")}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Total Iterations**: {history.Count}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Best Score Achieved**: {history.Max(i => i.Score)}%");
    sb.AppendLine("\n## Final Optimized Prompt");
    sb.AppendLine("```text");
    sb.AppendLine(finalPrompt);
    sb.AppendLine("```");
    sb.AppendLine("\n---\n");

    foreach (var iter in history)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"## Iteration {iter.Number}");
      sb.AppendLine(CultureInfo.InvariantCulture, $"> **Score**: {iter.Score}%");
      sb.AppendLine("\n### Prompt Used");
      sb.AppendLine("```text");
      sb.AppendLine(iter.Prompt);
      sb.AppendLine("```");

      if (string.IsNullOrEmpty(iter.ErrorLog) is false)
      {
        sb.AppendLine("\n### Issues Identified by Judge");
        sb.AppendLine(iter.ErrorLog);
      }

      sb.AppendLine("\n### Raw AI Response");
      sb.AppendLine("```json");
      sb.AppendLine(JsonSerializer.Serialize(iter.RawResponse, JsonOptions));
      sb.AppendLine("```");
      sb.AppendLine("\n---\n");
    }

    var filePath = _fileSystem.Path.Combine(directory, "report.md");
    await _fileSystem.File.WriteAllTextAsync(filePath, sb.ToString());
    Console.WriteLine($"\n[System] Session report persisted to: {filePath}");
  }
}