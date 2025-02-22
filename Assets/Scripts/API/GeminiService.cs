using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

public class GeminiService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private readonly DaVinciContext context;

    public GeminiService(string apiKey)
    {
        this.apiKey = apiKey;
        client = new HttpClient();
        context = new DaVinciContext();
    }

    public async Task<string> GetResponse(string userInput)
    {
        try
        {
            // Get contextualized prompt - keeping it concise for Flash model
            var prompt = context.get_prompt_context(userInput, new Dictionary<string, object>
            {
                ["is_painting"] = true,
                ["focused_project"] = "mona_lisa",
                ["frustration_level"] = 0.3f
            });

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.9,
                    topK = 40,
                    topP = 0.8,
                    candidateCount = 1,
                    maxOutputTokens = 256, // Reduced for Flash model
                    stopSequences = new[] { "User:", "Visitor:" }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            Debug.Log($"Sending request to Gemini Flash: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{baseUrl}?key={apiKey}";

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return ParseGeminiResponse(responseContent);
            }
            else
            {
                Debug.LogError($"Gemini API error: {response.StatusCode}, Content: {responseContent}");
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error calling Gemini API: {e.Message}");
            return "Mi dispiace, I am momentarily lost in thought...";
        }
    }

    private string ParseGeminiResponse(string responseJson)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);
            
            if (response?.candidates != null && 
                response.candidates.Length > 0 && 
                response.candidates[0].content?.parts != null &&
                response.candidates[0].content.parts.Length > 0)
            {
                return response.candidates[0].content.parts[0].text;
            }

            throw new Exception("Invalid response structure");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing Gemini response: {e.Message}\nResponse JSON: {responseJson}");
            return "Mi dispiace, I am having trouble forming my thoughts...";
        }
    }

    private class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
        public PromptFeedback promptFeedback { get; set; }
    }

    private class Candidate
    {
        public Content content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
    }

    private class Content
    {
        public Part[] parts { get; set; }
        public string role { get; set; }
    }

    private class Part
    {
        public string text { get; set; }
    }

    private class PromptFeedback
    {
        public SafetyRating[] safetyRatings { get; set; }
    }

    private class SafetyRating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }
}