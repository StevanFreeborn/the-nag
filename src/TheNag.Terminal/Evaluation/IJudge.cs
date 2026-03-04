using System.Text.Json;
using System.Text.Json.Schema;

namespace TheNag.Terminal.Evaluation;

internal interface IJudge<TResult>
{
  EvaluationResult Evaluate(TResult aiOutput, TResult groundTruth);

  string GetJsonSchema()
  {
    var options = new JsonSchemaExporterOptions()
    {
      TreatNullObliviousAsNonNullable = true,
    };
    return JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TResult), options).ToString();
  }
}