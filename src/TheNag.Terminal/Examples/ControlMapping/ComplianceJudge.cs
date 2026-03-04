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

    foreach (var expected in goldKey.Evaluations)
    {
      totalPoints += 10;

      var actual = aiOutput.Evaluations.FirstOrDefault(x => x.ControlId == expected.ControlId);

      if (actual == null)
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Missing Control: {expected.ControlId} was not analyzed.");
        continue;
      }

      if (actual.Status.Equals(expected.Status, StringComparison.OrdinalIgnoreCase))
      {
        earnedPoints += 7;
      }
      else
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Logic Error {expected.ControlId}: Expected {expected.Status}, but AI reported {actual.Status}.");
      }

      if (string.IsNullOrWhiteSpace(actual.Quote) is false && actual.Quote.Length > 10)
      {
        earnedPoints += 3;
      }
      else
      {
        errorLog.AppendLine(CultureInfo.InvariantCulture, $"- Citation Error {expected.ControlId}: Verbatim quote was missing or too short.");
      }
    }

    return new EvaluationResult
    {
      FinalScore = earnedPoints / totalPoints * 100,
      DetailedErrorLog = errorLog.ToString()
    };
  }
}