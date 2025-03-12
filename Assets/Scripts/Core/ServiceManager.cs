using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }
    
    // Add the animation event system
    public delegate void AnimationTriggerHandler(string marker);
    public event AnimationTriggerHandler OnAnimationTrigger;
    
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Voice Settings")]
    [SerializeField] private float voiceVolume = 1.0f;
    
    [Header("Conversation Memory")]
    [SerializeField] private int conversationMemoryItems = 3;
    [SerializeField] private bool enableConversationMemory = true;
    
    // Services
    private GeminiService geminiService;
    private ElevenLabsService elevenLabsService;
    private DirectAudioManager audioManager;
    private CharacterContext currentContext;
    
    // Memory for conversation context
    private List<(string userInput, string aiResponse)> conversationHistory = new List<(string userInput, string aiResponse)>();
    
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
            LogMessage("Initializing services...");
            
            LoadConfiguration();
            
            // Initialize DirectAudioManager (simplest audio implementation)
            audioManager = GetComponent<DirectAudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<DirectAudioManager>();
                LogMessage("Created DirectAudioManager component");
            }
            
            // Initialize API services
            geminiService = new GeminiService(configData.ApiKeys.Gemini);
            elevenLabsService = new ElevenLabsService(configData.ApiKeys.ElevenLabs, configData.VoiceConfig);
            
            // Add SimpleMusicPlayer if not already present
            var musicPlayer = GetComponent<SimpleMusicPlayer>();
            if (musicPlayer == null)
            {
                musicPlayer = gameObject.AddComponent<SimpleMusicPlayer>();
                LogMessage("Added SimpleMusicPlayer component");
            }
            
            LogMessage("Services initialized successfully");
        }
        catch (Exception e)
        {
            LogMessage($"Failed to initialize services: {e.Message}", true);
        }
    }
    
    private void LoadConfiguration()
    {
        try
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
            if (!File.Exists(configPath))
            {
                LogMessage("Configuration file not found. Creating default config.", true);
                configData = new ConfigData()
                {
                    ApiKeys = new ApiKeys
                    {
                        Gemini = "YOUR_API_KEY",
                        ElevenLabs = "YOUR_API_KEY"
                    },
                    VoiceConfig = new VoiceConfig
                    {
                        VoiceId = "pNInz6obpgDQGcFmaJgB",
                        Stability = 0.5f,
                        SimilarityBoost = 0.75f
                    }
                };
                return;
            }
            
            string jsonContent = File.ReadAllText(configPath);
            configData = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
            LogMessage("Configuration loaded successfully");
        }
        catch (Exception e)
        {
            LogMessage($"Error loading configuration: {e.Message}", true);
            // Create default config as fallback
            configData = new ConfigData()
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
        if (string.IsNullOrWhiteSpace(userInput))
        {
            LogMessage("Empty user input, ignoring request", true);
            return null;
        }
        
        LogMessage($"Processing user input: '{userInput}'");
        
        try
        {
            // Store the context if provided
            currentContext = context ?? currentContext;
            
            // Include conversation history for context
            string prompt = CreatePromptWithHistory(userInput, currentContext);
            
            // Get AI response from Gemini
            string response = await geminiService.GetResponse(userInput);
            LogMessage($"Received response from Gemini: '{response}'");
            
            // Parse for animation markers
            string marker = "NORMAL";
            string cleanResponse = response;
            
            if (currentContext != null)
            {
                var parsedResponse = currentContext.ParseResponse(response);
                marker = parsedResponse.marker;
                cleanResponse = parsedResponse.response;
                
                // Trigger animation based on marker
                LogMessage($"Animation marker: [{marker}]");
                OnAnimationTrigger?.Invoke(marker);
            }
            
            // Store in conversation history
            AddToConversationHistory(userInput, cleanResponse);
            
            // Generate audio using ElevenLabs
            LogMessage("Generating voice audio...");
            AudioClip audioClip = await elevenLabsService.GenerateVoice(cleanResponse);
            
            // Play the audio
            if (audioClip != null && audioClip.length > 0)
            {
                LogMessage($"Playing audio, length: {audioClip.length}s");
                if (audioManager != null)
                {
                    audioManager.PlaySpatialAudio(audioClip, audioSource);
                }
                else
                {
                    LogMessage("AudioManager is null, cannot play audio", true);
                }
            }
            else
            {
                LogMessage("Generated audio clip is invalid", true);
            }
            
            return audioClip;
        }
        catch (Exception e)
        {
            LogMessage($"Error processing user input: {e.Message}", true);
            return null;
        }
    }
    
    private string CreatePromptWithHistory(string userInput, CharacterContext context)
    {
        if (context == null || !enableConversationMemory || conversationHistory.Count == 0)
        {
            return userInput;
        }
        
        // Create a context with history for DaVinciContext
        var davinci = context as DaVinciContext;
        if (davinci != null)
        {
            // Context will be handled by DaVinciContext
            return userInput;
        }
        
        // For other context types, we might need to format the history differently
        return userInput;
    }
    
    private void AddToConversationHistory(string userInput, string aiResponse)
    {
        if (!enableConversationMemory) return;
        
        // Add the new exchange
        conversationHistory.Add((userInput, aiResponse));
        
        // Trim to keep only the most recent exchanges
        while (conversationHistory.Count > conversationMemoryItems)
        {
            conversationHistory.RemoveAt(0);
        }
        
        LogMessage($"Added conversation exchange. History now contains {conversationHistory.Count} items.");
    }
    
    public string GetFormattedConversationHistory()
    {
        if (!enableConversationMemory || conversationHistory.Count == 0)
        {
            return string.Empty;
        }
        
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("\nRecent conversation history:");
        
        foreach (var exchange in conversationHistory)
        {
            builder.AppendLine($"Visitor: {exchange.userInput}");
            builder.AppendLine($"Leonardo: {exchange.aiResponse}\n");
        }
        
        return builder.ToString();
    }
    
    private void LogMessage(string message, bool isError = false)
    {
        if (!debugMode && !isError) return;
        
        if (isError)
        {
            Debug.LogError($"[ServiceManager] {message}");
        }
        else
        {
            Debug.Log($"[ServiceManager] {message}");
        }
    }
}