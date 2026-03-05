using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheNag.Terminal.Evaluation.Gemini;

internal sealed class GeminiService(IHttpClientFactory httpClientFactory, string apiKey)
{
  private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
  private readonly string _apiKey = apiKey;

  public async Task<string> GetStructuredResponseAsync(string prompt, string context, string schemaJson)
  {
    var request = new GeminiRequest
    {
      SystemInstruction = new Content
      {
        Parts = [new Part { Text = prompt }]
      },
      Contents = [new Content { Parts = [new Part { Text = $"CONTEXT:\n{context}" }] }],
      GenerationConfig = new GenerationConfig
      {
        ResponseMimeType = "application/json",
        ResponseSchema = JsonSerializer.Deserialize<JsonElement>(schemaJson),
        Temperature = 0.1,
      }
    };

    var response = await SendRequestAsync(request, "gemini-2.5-flash");
    return ExtractTextFromResponse(response);
  }

  public async Task<string> RefinePromptAsync(string metaPrompt)
  {
    var request = new GeminiRequest
    {
      Contents = [new Content { Parts = [new Part { Text = metaPrompt }] }],
      GenerationConfig = new GenerationConfig
      {
        Temperature = 0.7,
        TopP = 0.95
      }
    };

    var response = await SendRequestAsync(request, "gemini-2.5-pro");
    return ExtractTextFromResponse(response);
  }

  private async Task<GeminiResponse> SendRequestAsync(GeminiRequest request, string model)
  {
    using var client = _httpClientFactory.CreateClient();
    var jsonRequest = JsonSerializer.Serialize(request, JsonSerializerOptions);
    using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(
      new Uri($"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}"),
      content
    );

    if (response.IsSuccessStatusCode is false)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new HttpRequestException($"Gemini API Error ({model}): {response.StatusCode} - {errorContent}");
    }

    var jsonResponse = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<GeminiResponse>(jsonResponse, JsonSerializerOptions)
      ?? throw new JsonException($"Failed to deserialize response from {model}");
  }

  private static string ExtractTextFromResponse(GeminiResponse response)
  {
    return response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
      ?? throw new JsonException("Invalid response structure: missing text in candidates[0].content.parts[0]");
  }

  private static readonly JsonSerializerOptions JsonSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };
}
