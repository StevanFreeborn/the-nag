using System.Globalization;
using System.Text;

using TheNag.Terminal.Evaluation;

namespace TheNag.Terminal.Examples.ControlMapping;

internal sealed class ComplianceJudge : IJudge<MappingResult>
{
  public EvaluationResult Evaluate(MappingResult aiOutput, MappingResult goldKey)
  {
    double totalPoints = 0;
    double earnedPoints = 0;
    var errorLog = new StringBuilder();

    var expectedIds = goldKey.Evaluations.Select(e => e.ControlId).ToList();
    var actualIds = aiOutput.Evaluations.Select(e => e.ControlId).ToList();
    var unmatchedActual = new List<string>(actualIds);

    foreach (var expected in goldKey.Evaluations)
    {
      totalPoints += 10;

      var actual = aiOutput.Evaluations.FirstOrDefault(x => x.ControlId.Contains(expected.ControlId, StringComparison.OrdinalIgnoreCase));

      if (actual == null)
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Missing Control: Expected {expected.ControlId} but it was not in the AI output.");
        continue;
      }

      unmatchedActual.Remove(actual.ControlId);

      if (actual.Status.Equals(expected.Status, StringComparison.OrdinalIgnoreCase))
      {
        earnedPoints += 7;
      }
      else
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Status Mismatch on {expected.ControlId}: Expected '{expected.Status}', got '{actual.Status}'.");
      }

      if (string.IsNullOrWhiteSpace(actual.Quote) is false && actual.Quote.Length > 10)
      {
        earnedPoints += 3;
      }
      else
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Citation Error on {expected.ControlId}: Quote was missing or too short.");
      }
    }

    if (aiOutput.Evaluations.Count == 0)
    {
      errorLog.AppendLine("- CRITICAL: AI returned zero evaluations. The model produced no control analysis at all.");
    }

    if (unmatchedActual.Count > 0)
    {
      errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Unexpected Controls: AI analyzed [{string.Join(", ", unmatchedActual)}] which were not in the expected set [{string.Join(", ", expectedIds)}].");
    }

    return new EvaluationResult
    {
      FinalScore = earnedPoints / totalPoints * 100,
      DetailedErrorLog = errorLog.ToString()
    };
  }
}