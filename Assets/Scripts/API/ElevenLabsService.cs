using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

public class ElevenLabsService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string voiceId = "W71zT1VwIFFx3mMGH2uZ"; // Fixed voice ID
    private readonly VoiceConfig voiceConfig;
    private readonly string baseUrl = "https://api.elevenlabs.io/v1";

    public ElevenLabsService(string apiKey, VoiceConfig voiceConfig)
    {
        this.apiKey = apiKey;
        this.voiceConfig = voiceConfig;
        
        client = new HttpClient();
        client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
    }

    public async Task<AudioClip> GenerateVoice(string text)
    {
        try
        {
            var requestBody = new
            {
                text = text,
                model_id = "eleven_flash_v2_5", // Using Flash model for faster responses
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

            // Using stream endpoint with maximum latency optimization
            var response = await client.PostAsync(
                $"{baseUrl}/text-to-speech/{voiceId}/stream?optimize_streaming_latency=4",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                byte[] audioData = await response.Content.ReadAsByteArrayAsync();
                return await ConvertToAudioClip(audioData);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.LogError($"ElevenLabs API error: {response.StatusCode}, Content: {errorContent}");
                throw new Exception($"ElevenLabs API error: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error calling ElevenLabs API: {e.Message}");
            return null;
        }
    }

    private async Task<AudioClip> ConvertToAudioClip(byte[] audioData)
    {
        return await Task.Run(() =>
        {
            try
            {
                const int sampleRate = 44100;
                const int channels = 1;
                
                AudioClip clip = AudioClip.Create(
                    "DaVinciVoice",
                    audioData.Length / 2,
                    channels,
                    sampleRate,
                    false
                );

                float[] samples = new float[audioData.Length / 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = BitConverter.ToInt16(audioData, i * 2);
                    samples[i] = sample / 32768f;
                }

                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error converting audio data: {e.Message}");
                return null;
            }
        });
    }

    public async Task<bool> ValidateApiKey()
    {
        try
        {
            var response = await client.GetAsync($"{baseUrl}/voices");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
