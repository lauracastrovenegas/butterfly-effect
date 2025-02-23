using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }

    // Add the animation event system
    public delegate void AnimationTriggerHandler(string marker);
    public event AnimationTriggerHandler OnAnimationTrigger;

    private GeminiService geminiService;
    private ElevenLabsService elevenLabsService;
    private VoiceSDKManager voiceSDKManager;
    private AudioManager audioManager;
    private CharacterContext currentContext;

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

    private ConfigData configData;

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

    private void InitializeServices()
    {
        try
        {
            LoadConfiguration();
            
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

    public async Task<AudioClip> ProcessUserInput(string userInput, Transform audioSource, CharacterContext context = null)
    {
        try
        {
            currentContext = context;
            
            // Get AI response
            string response = await geminiService.GetResponse(userInput);

            // Parse for animation markers if we have a context
            if (currentContext != null)
            {
                var (marker, cleanResponse) = currentContext.ParseResponse(response);
                OnAnimationTrigger?.Invoke(marker);
                response = cleanResponse;
            }
            
            // Convert to audio using ElevenLabs
            AudioClip audioClip = await elevenLabsService.GenerateVoice(response);

            // Ensure audio clip was created successfully
            if (audioClip != null && audioClip.length > 0)
            {
                Debug.Log($"[ServiceManager] Generated audio clip length: {audioClip.length}s");
                audioManager.PlaySpatialAudio(audioClip, audioSource);
            }
            else
            {
                Debug.LogError("[ServiceManager] Generated audio clip is invalid");
            }

            return audioClip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManager] Error processing user input: {e.Message}");
            return null;
        }
    }
}