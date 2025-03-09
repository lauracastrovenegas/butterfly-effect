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
    private BackgroundMusicManager musicManager;

    [Serializable]
    private class ConfigData
    {
        public ApiKeys ApiKeys;
        public VoiceConfig VoiceConfig;
        public MusicConfig MusicConfig;
    }

    [Serializable]
    private class ApiKeys
    {
        public string Gemini;
        public string ElevenLabs;
    }

    [Serializable]
    private class MusicConfig
    {
        public float Volume = 0.3f;
        public bool AutoPlay = true;
        public string MusicTitle = "O Mia ciecha e dura sorte";
        public string Composer = "Marchetto Cara";
    }

    private ConfigData configData;
    private ResponseCache responseCache;

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
            musicManager = gameObject.AddComponent<BackgroundMusicManager>();
            responseCache = new ResponseCache();

            // Configure music manager with settings
            musicManager.Initialize(configData.MusicConfig.Volume, configData.MusicConfig.AutoPlay);

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
            currentContext = context ?? currentContext;
            
            // Get AI response
            string response = await geminiService.GetResponse(userInput);
            string marker = "NORMAL";
            string cleanResponse = response;

            // Parse for animation markers if we have a context
            if (currentContext != null)
            {
                var parsedResponse = currentContext.ParseResponse(response);
                marker = parsedResponse.marker;
                cleanResponse = parsedResponse.response;
                
                // Trigger animation based on marker
                OnAnimationTrigger?.Invoke(marker);
            }
            
            // Check cache first before generating new audio
            if (responseCache.TryGetCachedResponse(cleanResponse, out AudioClip cachedClip))
            {
                Debug.Log($"[ServiceManager] Using cached audio clip");
                audioManager.PlaySpatialAudio(cachedClip, audioSource);
                return cachedClip;
            }
            
            // Convert to audio using ElevenLabs - important: use cleanResponse without markers
            AudioClip audioClip = await elevenLabsService.GenerateVoice(cleanResponse);

            // Ensure audio clip was created successfully
            if (audioClip != null && audioClip.length > 0)
            {
                Debug.Log($"[ServiceManager] Generated audio clip length: {audioClip.length}s");
                audioManager.PlaySpatialAudio(audioClip, audioSource);
                responseCache.CacheResponse(cleanResponse, audioClip);
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