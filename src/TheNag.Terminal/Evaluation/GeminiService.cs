using System.Text;
using System.Text.Json;

namespace TheNag.Terminal.Evaluation;

internal sealed class GeminiService(IHttpClientFactory httpClientFactory, string apiKey)
{
  private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
  private readonly string _apiKey = apiKey;

  public async Task<string> GetStructuredResponseAsync(string prompt, string context, string schemaJson)
  {
    var requestBody = new
    {
      contents = new[] {
        new
        {
          parts = new[]
          {
            new
            {
              text = $"{prompt}\n\nCONTEXT:\n{context}"
            }
          }
        }
      },
      generationConfig = new
      {
        response_mime_type = "application/json",
        response_schema = JsonElement.Parse(Encoding.UTF8.GetBytes(schemaJson)),
      }
    };

    using var client = _httpClientFactory.CreateClient();
    using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(
        new Uri($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}"),
        content
    );

    if (response.IsSuccessStatusCode is false)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new HttpRequestException($"Gemini API Error: {response.StatusCode} - {errorContent}");
    }

    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.GetProperty("candidates")[0]
      .GetProperty("content")
      .GetProperty("parts")[0]
      .GetProperty("text")
      .GetString() ?? throw new JsonException("Invalid response from Gemini API");
  }

  public async Task<string> RefinePromptAsync(string currentPrompt, string errorLog)
  {
    var metaInstruction = $@"
        You are an expert Prompt Engineer specializing in high-precision data extraction and analysis.
        
        GOAL:
        Analyze the provided 'Error Log' and rewrite the 'Current Prompt' to eliminate these errors.
        The rewritten prompt must be more effective, structured, and directive.

        RULES FOR REFINEMENT:
        1. DO NOT include the specific answers or 'Golden Key' data in the new prompt. 
        2. Focus on improving the 'Instructions', 'Constraints', and 'Thinking Process' (e.g., Chain of Thought).
        3. If the error log shows missing data, instruct the model to look specifically for those attributes.
        4. If the error log shows hallucinations, add a constraint requiring verbatim evidence.
        5. Maintain the same output schema requirements.

        CURRENT PROMPT TO IMPROVE:
        ""{currentPrompt}""

        ERROR LOG FROM PREVIOUS RUN:
        {errorLog}

        Output ONLY the text of the new, improved prompt. Do not include any conversational filler.";

    var requestBody = new
    {
      contents = new[]
      {
        new
        {
          parts = new[]
          {
            new
            {
              text = metaInstruction
            }
          }
        }
      },
      generationConfig = new
      {
        temperature = 0.7,
        topP = 0.95,
      }
    };

    var jsonRequest = JsonSerializer.Serialize(requestBody);

    using var client = _httpClientFactory.CreateClient();
    using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(
      new Uri($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={_apiKey}"),
      content
    );

    if (response.IsSuccessStatusCode is false)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new HttpRequestException($"Gemini API RefinePrompt Error: {response.StatusCode} - {errorContent}");
    }

    var jsonResponse = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(jsonResponse);

    return doc.RootElement.GetProperty("candidates")[0]
      .GetProperty("content")
      .GetProperty("parts")[0]
      .GetProperty("text")
      .GetString() ?? throw new JsonException("Invalid response from Gemini API during prompt refinement");
  }
}