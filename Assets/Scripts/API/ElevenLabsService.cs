using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.IO;
using System.Net.Http.Headers;

public class ElevenLabsService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly VoiceConfig voiceConfig;
    private readonly string baseUrl = "https://api.elevenlabs.io/v1";

    public ElevenLabsService(string apiKey, VoiceConfig voiceConfig)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("ElevenLabs API key cannot be empty");
        }

        this.apiKey = apiKey;
        this.voiceConfig = voiceConfig;
        
        client = new HttpClient();
        client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AudioClip> GenerateVoice(string text)
    {
        try
        {
            Debug.Log($"[ElevenLabsService] Generating voice for text: {text}");

            var requestBody = new
            {
                text = text,
                model_id = "eleven_flash_v2_5",
                voice_settings = new
                {
                    stability = voiceConfig.Stability,
                    similarity_boost = voiceConfig.SimilarityBoost,
                    style = 0.0,
                    use_speaker_boost = true
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Request MP3 output directly
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

            var response = await client.PostAsync($"{baseUrl}/text-to-speech/{voiceConfig.VoiceId}", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"ElevenLabs API error: {response.StatusCode}, Content: {err}");
            }

            byte[] audioData = await response.Content.ReadAsByteArrayAsync();
            Debug.Log($"[ElevenLabsService] Received {audioData.Length} bytes of MP3 data");
            return await LoadAudioClip(audioData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ElevenLabsService] Error: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    private async Task<AudioClip> LoadAudioClip(byte[] audioData)
    {
        // Write file out and then load it using UnityWebRequest
        string tempPath = Path.Combine(Application.temporaryCachePath, "temp_audio.mp3");
        await File.WriteAllBytesAsync(tempPath, audioData);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            await www.SendWebRequest();  // Await the operation directly
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load audio: {www.error}");
                return null;
            }
            
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            Debug.Log($"[ElevenLabsService] Successfully loaded MP3 clip, Length: {clip.length}s");
            File.Delete(tempPath);
            return clip;
        }
    }
}