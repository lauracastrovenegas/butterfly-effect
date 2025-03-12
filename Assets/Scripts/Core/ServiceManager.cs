using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections;

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
    
    // Flag to track initialization status
    public bool IsInitialized { get; private set; } = false;
    
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
    private ResponseCache responseCache;
    
    private void Awake()
    {
        Debug.Log("[ServiceManager] Awake called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeServicesCoroutine());
        }
        else if (Instance != this)
        {
            Debug.Log("[ServiceManager] Destroying duplicate ServiceManager");
            Destroy(gameObject);
        }
    }

    private IEnumerator InitializeServicesCoroutine()
    {
        yield return null; // Wait one frame
        Initialize();
    }
    
    // Public method to force initialization and allow for delayed init
    public void Initialize()
    {
        try
        {
            Debug.Log("[ServiceManager] Initialize method called");
            
            // Exit if already initialized
            if (IsInitialized)
            {
                Debug.Log("[ServiceManager] Already initialized");
                return;
            }
            
            LoadConfiguration();
            
            try 
            {
                geminiService = new GeminiService(configData.ApiKeys.Gemini);
                Debug.Log("[ServiceManager] GeminiService initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServiceManager] Failed to initialize GeminiService: {e.Message}");
            }
            
            try
            {
                elevenLabsService = new ElevenLabsService(configData.ApiKeys.ElevenLabs, configData.VoiceConfig);
                Debug.Log("[ServiceManager] ElevenLabsService initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServiceManager] Failed to initialize ElevenLabsService: {e.Message}");
            }
            
            // Instead of adding components, find existing ones first
            voiceSDKManager = FindFirstObjectByType<VoiceSDKManager>();
            if (voiceSDKManager == null)
            {
                voiceSDKManager = gameObject.AddComponent<VoiceSDKManager>();
                Debug.Log("[ServiceManager] Created VoiceSDKManager component");
            }
            else
            {
                Debug.Log("[ServiceManager] Found existing VoiceSDKManager");
            }
            
            audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<AudioManager>();
                Debug.Log("[ServiceManager] Created AudioManager component");
            }
            else
            {
                Debug.Log("[ServiceManager] Found existing AudioManager");
            }
            
            responseCache = new ResponseCache();
            
            IsInitialized = true;
            Debug.Log("[ServiceManager] Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManager] Failed to initialize services: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void LoadConfiguration()
    {
        try
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
            if (!File.Exists(configPath))
            {
                Debug.LogWarning("[ServiceManager] Configuration file not found. Creating default config.");
                configData = new ConfigData
                {
                    ApiKeys = new ApiKeys
                    {
                        Gemini = "YOUR_GEMINI_API_KEY", // Default value
                        ElevenLabs = "YOUR_ELEVENLABS_API_KEY" // Default value
                    },
                    VoiceConfig = new VoiceConfig
                    {
                        VoiceId = "pNInz6obpgDQGcFmaJgB", // Default voice ID
                        Stability = 0.5f,
                        SimilarityBoost = 0.75f
                    }
                };
                return;
            }
            
            string jsonContent = File.ReadAllText(configPath);
            configData = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
            Debug.Log("[ServiceManager] Configuration loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManager] Error loading configuration: {e.Message}");
            // Create default config as fallback
            configData = new ConfigData
            {
                ApiKeys = new ApiKeys
                {
                    Gemini = "",
                    ElevenLabs = ""
                },
                VoiceConfig = new VoiceConfig
                {
                    VoiceId = "pNInz6obpgDQGcFmaJgB",
                    Stability = 0.5f,
                    SimilarityBoost = 0.75f
                }
            };
        }
    }
    
    public async Task<AudioClip> ProcessUserInput(string userInput, Transform audioSource, CharacterContext context = null)
    {
        try
        {
            // Check if initialized
            if (!IsInitialized)
            {
                Debug.LogWarning("[ServiceManager] Not fully initialized yet, initializing now");
                Initialize();
            }
            
            Debug.Log($"[ServiceManager] Processing user input: {userInput}");
            
            currentContext = context ?? currentContext;
            
            if (geminiService == null)
            {
                Debug.LogError("[ServiceManager] GeminiService is null!");
                return null;
            }
            
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
                Debug.Log($"[ServiceManager] Triggered animation: {marker}");
            }
            
            // Check if ElevenLabs is available
            if (elevenLabsService == null)
            {
                Debug.LogError("[ServiceManager] ElevenLabsService is null!");
                return null;
            }
            
            // Check cache first before generating new audio
            if (responseCache != null && responseCache.TryGetCachedResponse(cleanResponse, out AudioClip cachedClip))
            {
                Debug.Log($"[ServiceManager] Using cached audio clip");
                if (audioManager != null)
                {
                    audioManager.PlaySpatialAudio(cachedClip, audioSource);
                }
                return cachedClip;
            }
            
            // Convert to audio using ElevenLabs - important: use cleanResponse without markers
            Debug.Log("[ServiceManager] Generating voice audio...");
            AudioClip audioClip = await elevenLabsService.GenerateVoice(cleanResponse);
            
            // Ensure audio clip was created successfully
            if (audioClip != null && audioClip.length > 0)
            {
                Debug.Log($"[ServiceManager] Generated audio clip length: {audioClip.length}s");
                if (audioManager != null)
                {
                    audioManager.PlaySpatialAudio(audioClip, audioSource);
                }
                else
                {
                    Debug.LogError("[ServiceManager] AudioManager is null!");
                }
                
                if (responseCache != null)
                {
                    responseCache.CacheResponse(cleanResponse, audioClip);
                }
            }
            else
            {
                Debug.LogError("[ServiceManager] Generated audio clip is invalid");
            }
            
            return audioClip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManager] Error processing user input: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    
    // Help with debugging
    private void OnEnable()
    {
        Debug.Log("[ServiceManager] OnEnable called");
    }
    
    private void OnDisable()
    {
        Debug.Log("[ServiceManager] OnDisable called");
    }
    
    private void OnDestroy()
    {
        Debug.Log("[ServiceManager] OnDestroy called");
        if (Instance == this)
        {
            Instance = null;
        }
    }
}