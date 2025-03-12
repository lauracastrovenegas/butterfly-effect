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
    
    // Removed instantiation of DaVinciContext - will use the provided context instead

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
        client.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GetResponse(string userInput, CharacterContext context = null)
    {
        try
        {
            Debug.Log($"[GeminiService] Processing input: {userInput}");

            if (context == null)
            {
                Debug.LogWarning("[GeminiService] No context provided, response may lack character personality");
                return "I'm sorry, I'm having trouble finding my character at the moment.";
            }

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
            // Log only part of the API key for security
            Debug.Log($"[GeminiService] Request URL: {baseUrl}?key={apiKey.Substring(0, 4)}...");
            
            // For detailed debugging (disable in production)
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[GeminiService] Request body: {json}");
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{baseUrl}?key={apiKey}";

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"[GeminiService] Response status: {response.StatusCode}");
            
            // For detailed debugging (disable in production)
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[GeminiService] Response headers: {string.Join(", ", response.Headers)}");
                Debug.Log($"[GeminiService] Response content: {responseContent}");
            }

            if (response.IsSuccessStatusCode)
            {
                string parsedResponse = ParseGeminiResponse(responseContent);
                Debug.Log($"[GeminiService] Parsed response: {parsedResponse.Substring(0, Math.Min(50, parsedResponse.Length))}...");
                return parsedResponse;
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
                
                // If the response doesn't have a marker, add the default [NORMAL] marker
                if (!result.TrimStart().StartsWith("["))
                {
                    result = "[NORMAL] " + result;
                    Debug.Log("[GeminiService] Added missing marker to response");
                }
                
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

    // Response classes remain the same
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