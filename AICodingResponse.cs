using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinOps.GLCodingEngine.Services;

// 1. The expected output format from the LLM
public sealed class AICodingResponse
{
    [JsonPropertyName("matchedRuleId")]
    public string? MatchedRuleId { get; set; }

    [JsonPropertyName("glCode")]
    public string? GlCode { get; set; }

    [JsonPropertyName("costCenterCode")]
    public string? CostCenterCode { get; set; }

    [JsonPropertyName("categoryCode")]
    public string? CategoryCode { get; set; }

    [JsonPropertyName("confidenceScore")]
    public int ConfidenceScore { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}

// 2. The Agentic Client
public sealed class AIGLCodingAgent
{
    private readonly HttpClient _httpClient;
    // For the demo, hardcode your API key or load it from config. 
    // Example uses Groq's fast Llama3 API, but adapt to Gemini/Ollama as needed.
    private readonly string _apiKey = Constants.GROQ_API_KEY ?? string.Empty;
    private readonly string _endpoint = "https://api.groq.com/openai/v1/chat/completions";

    public AIGLCodingAgent(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AICodingResponse?> PredictAsync(
        string? vendorName,
        string? description,
        string availableRulesJson)
    {
        // The System Prompt: Defines the persona and strict output constraints
        var systemPrompt = @"You are an expert Accounts Payable FinOps Data Entry Agent.
                            Your job is to analyze an invoice line item and assign the correct GL Code, Cost Center, and Category based ONLY on the provided master data rules.
                            You MUST output strictly in raw JSON format matching the requested schema. Do not include markdown formatting like ```json.";

                                    // The User Prompt: Injects the specific invoice reality and the dynamic master data
                                    var userPrompt = $@"
                            INVOICE LINE ITEM:
                            Vendor Name: {vendorName ?? "UNKNOWN"}
                            Description: {description ?? "UNKNOWN"}

                            AVAILABLE MASTER DATA RULES (JSON):
                            {availableRulesJson}

                            INSTRUCTIONS:
                            1. Find the best matching rule. Consider both exact and fuzzy matches for the vendor and description keywords.
                            2. If a rule matches strongly, use its GLCode, CostCenterCode, and CategoryCode.
                            3. If the vendor is unknown, try to map based on the description to the closest Category.
                            4. Calculate a Confidence Score (0-100).
                               - 90-100: Exact vendor and strong keyword match.
                               - 60-89: Vendor matches, but description requires interpretation.
                               - 1-59: Fuzzy category match, no vendor rule.
                               - 0: Cannot determine.

                            OUTPUT SCHEMA REQUIRED:
                            {{
                              ""matchedRuleId"": ""string or null"",
                              ""glCode"": ""string or null"",
                              ""costCenterCode"": ""string or null"",
                              ""categoryCode"": ""string or null"",
                              ""confidenceScore"": integer,
                              ""reasoning"": ""string explanation of your choice""
                            }}";

        var requestBody = new
        {
            model = "llama-3.1-8b-instant", // Fast, free model on Groq
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1, // Low temp for factual, deterministic matching
            response_format = new { type = "json_object" } // Forces JSON output
        };

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"AI API call failed: {error}");
        }

        var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentString = jsonResult.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrWhiteSpace(contentString)) return null;

        return JsonSerializer.Deserialize<AICodingResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}