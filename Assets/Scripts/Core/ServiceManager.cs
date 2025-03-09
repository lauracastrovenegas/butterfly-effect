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
            
            // Setup music player
            SetupBackgroundMusic();
            
            responseCache = new ResponseCache();

            Debug.Log("Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize services: {e.Message}");
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