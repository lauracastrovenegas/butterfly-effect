using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;  // Add this line

public class GeminiService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string baseUrl = "https://generativelanguage.googleapis.com/v1beta2/models/gemini-2.0-flash:generateText";
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
            Debug.LogError($"Error parsing Gemini response: {e.Message}");
            return "Mi dispiace, I am having trouble forming my thoughts...";
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