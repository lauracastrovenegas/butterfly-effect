using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;

public class GeminiService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private readonly DaVinciContext context;

    public GeminiService(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[GeminiService] API key is empty!");
            throw new ArgumentException("Gemini API key cannot be empty");
        }

        Debug.Log($"[GeminiService] Initializing with API key length: {apiKey.Length}");
        this.apiKey = apiKey;
        client = new HttpClient();
        context = new DaVinciContext();
        client.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GetResponse(string userInput)
    {
        try
        {
            Debug.Log($"[GeminiService] Processing input: {userInput}");

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
                    temperature = 0.9f,
                    topK = 40,
                    topP = 0.8f,
                    maxOutputTokens = 256
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            Debug.Log($"[GeminiService] Request URL: {baseUrl}?key={apiKey.Substring(0, 4)}...");
            Debug.Log($"[GeminiService] Request body: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{baseUrl}?key={apiKey}";

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"[GeminiService] Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                return ParseGeminiResponse(responseContent);
            }
            else
            {
                Debug.LogError($"[GeminiService] API error: {response.StatusCode}, Content: {responseContent}");
                return "[NORMAL] Mi dispiace, I am having trouble with my thoughts...";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GeminiService] Error: {e.Message}\nStack trace: {e.StackTrace}");
            return "[NORMAL] Mi dispiace, I am momentarily lost in thought...";
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
                var result = response.candidates[0].content.parts[0].text;
                Debug.Log($"[GeminiService] Successfully parsed response: {result}");
                return result;
            }

            throw new Exception($"Invalid response structure: {responseJson}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GeminiService] Error parsing response: {e.Message}");
            return "[NORMAL] Mi dispiace, I am having trouble forming my thoughts...";
        }
    }

    private class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
    }

    private class Candidate
    {
        public Content content { get; set; }
    }

    private class Content
    {
        public Part[] parts { get; set; }
    }

    private class Part
    {
        public string text { get; set; }
    }
}