// Assets/Scripts/Core/ServiceManager.cs
using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }
    
    private GeminiService geminiService;
    private ElevenLabsService elevenLabsService;
    private VoiceSDKManager voiceSDKManager;
    private AudioManager audioManager;
    
    private ConfigData configData;
    
    [Serializable]
    private class ConfigData
    {
        public ApiKeys ApiKeys;
        public VoiceConfig VoiceConfig;
    }
    
    [Serializable]
    private class ApiKeys
    {
        public string Gemini;
        public string ElevenLabs;
    }
    
    [Serializable]
    private class VoiceConfig
    {
        public string VoiceId;
        public float Stability;
        public float SimilarityBoost;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private async void InitializeServices()
    {
        try
        {
            // Load configuration
            LoadConfiguration();
            
            // Initialize services
            geminiService = new GeminiService(configData.ApiKeys.Gemini);
            elevenLabsService = new ElevenLabsService(configData.ApiKeys.ElevenLabs, configData.VoiceConfig);
            voiceSDKManager = gameObject.AddComponent<VoiceSDKManager>();
            audioManager = gameObject.AddComponent<AudioManager>();
            
            Debug.Log("Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize services: {e.Message}");
        }
    }
    
    private void LoadConfiguration()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Configuration file not found. Please create config.json from template.");
        }
        
        string jsonContent = File.ReadAllText(configPath);
        configData = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
    }
    
    public async Task<AudioClip> ProcessUserInput(string userInput, Transform audioSource)
    {
        try
        {
            // Get response from Gemini
            string response = await geminiService.GetResponse(userInput);
            
            // Convert to audio using ElevenLabs
            AudioClip audioClip = await elevenLabsService.GenerateVoice(response);
            
            // Play through audio manager
            audioManager.PlaySpatialAudio(audioClip, audioSource);
            
            return audioClip;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing user input: {e.Message}");
            return null;
        }
    }
}