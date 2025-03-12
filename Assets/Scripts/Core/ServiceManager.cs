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
    }

    private ConfigData configData;
    private ResponseCache responseCache;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.3f;
    [SerializeField] private bool playMusicOnAwake = true;
    
    private AudioSource musicSource;

    // Debugging
    [Header("Debug Settings")]
    [SerializeField] private bool enableVerboseLogging = true;

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
            
            // Try to find existing VoiceSDKManager first
            voiceSDKManager = FindFirstObjectByType<VoiceSDKManager>();
            if (voiceSDKManager == null)
            {
                LogMessage("No VoiceSDKManager found, creating new instance");
                voiceSDKManager = gameObject.AddComponent<VoiceSDKManager>();
            }
            else
            {
                LogMessage("Found existing VoiceSDKManager");
            }
            
            // Try to find existing AudioManager first
            audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                LogMessage("No AudioManager found, creating new instance");
                audioManager = gameObject.AddComponent<AudioManager>();
            }
            else
            {
                LogMessage("Found existing AudioManager");
            }
            
            // Setup music player
            SetupBackgroundMusic();
            
            responseCache = new ResponseCache();

            LogMessage("Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize services: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SetupBackgroundMusic()
    {
        // Create audio source for background music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D sound
        
        // Apply settings from config if available
        if (configData.MusicConfig != null)
        {
            musicSource.volume = configData.MusicConfig.Volume;
            playMusicOnAwake = configData.MusicConfig.AutoPlay;
        }
        
        // Start playing if set to play on awake
        if (playMusicOnAwake && backgroundMusic != null)
        {
            musicSource.Play();
        }
    }

    private void LoadConfiguration()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (!File.Exists(configPath))
        {
            Debug.LogWarning("Configuration file not found. Creating default config for testing.");
            configData = new ConfigData
            {
                ApiKeys = new ApiKeys
                {
                    Gemini = "YOUR_GEMINI_API_KEY", // Replace in production
                    ElevenLabs = "YOUR_ELEVENLABS_API_KEY" // Replace in production
                },
                VoiceConfig = new VoiceConfig
                {
                    VoiceId = "pNInz6obpgDQGcFmaJgB", // default ElevenLabs voice ID
                    Stability = 0.5f,
                    SimilarityBoost = 0.75f
                },
                MusicConfig = new MusicConfig
                {
                    Volume = 0.3f,
                    AutoPlay = true
                }
            };
            return;
        }

        string jsonContent = File.ReadAllText(configPath);
        configData = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
        LogMessage("Configuration loaded successfully");
    }

    public async Task<AudioClip> ProcessUserInput(string userInput, Transform audioSource, CharacterContext context = null)
    {
        try
        {
            if (string.IsNullOrEmpty(userInput))
            {
                LogMessage("Empty user input received, aborting", true);
                return null;
            }

            LogMessage($"Processing user input: {userInput}");
            
            // Update current context if provided
            currentContext = context ?? currentContext;
            
            if (currentContext == null)
            {
                LogMessage("No character context available!", true);
                return null;
            }
            
            // Get AI response - pass the context to ensure correct personality
            string response = await geminiService.GetResponse(userInput, currentContext);
            
            if (string.IsNullOrEmpty(response))
            {
                LogMessage("Empty response from Gemini API", true);
                return null;
            }
            
            LogMessage($"Received response from Gemini: {response.Substring(0, Math.Min(50, response.Length))}...");
            
            string marker = "NORMAL";
            string cleanResponse = response;

            // Parse for animation markers
            var parsedResponse = currentContext.ParseResponse(response);
            marker = parsedResponse.marker;
            cleanResponse = parsedResponse.response;
            
            LogMessage($"Parsed marker: [{marker}]");
            
            // Trigger animation based on marker
            LogMessage($"Triggering animation: {marker}");
            OnAnimationTrigger?.Invoke(marker);
            
            // Check cache first before generating new audio
            if (responseCache.TryGetCachedResponse(cleanResponse, out AudioClip cachedClip))
            {
                LogMessage($"Using cached audio clip");
                audioManager.PlaySpatialAudio(cachedClip, audioSource);
                return cachedClip;
            }
            
            // Convert to audio using ElevenLabs - important: use cleanResponse without markers
            LogMessage($"Generating voice for response");
            AudioClip audioClip = await elevenLabsService.GenerateVoice(cleanResponse);

            // Ensure audio clip was created successfully
            if (audioClip != null && audioClip.length > 0)
            {
                LogMessage($"Generated audio clip length: {audioClip.length}s");
                audioManager.PlaySpatialAudio(audioClip, audioSource);
                responseCache.CacheResponse(cleanResponse, audioClip);
            }
            else
            {
                LogMessage("Generated audio clip is invalid", true);
            }

            return audioClip;
        }
        catch (Exception e)
        {
            LogMessage($"Error processing user input: {e.Message}\n{e.StackTrace}", true);
            return null;
        }
    }
    
    private void LogMessage(string message, bool isError = false)
    {
        if (!enableVerboseLogging && !isError) return;
        
        if (isError)
            Debug.LogError($"[ServiceManager] {message}");
        else
            Debug.Log($"[ServiceManager] {message}");
    }
    
    // Music control methods
    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }
    
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    public void ChangeMusic(AudioClip newMusic)
    {
        if (musicSource != null && newMusic != null)
        {
            bool wasPlaying = musicSource.isPlaying;
            musicSource.Stop();
            musicSource.clip = newMusic;
            backgroundMusic = newMusic;
            
            if (wasPlaying)
            {
                musicSource.Play();
            }
        }
    }
}