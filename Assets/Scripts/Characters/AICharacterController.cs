using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

[RequireComponent(typeof(AudioSource))]
public class AICharacterController : MonoBehaviour
{
    [Header("Character Configuration")]
    [Tooltip("The context defines this character's personality and responses")]
    public CharacterContext context;

    [Header("Optional Debug UI")]
    public TextMeshProUGUI transcriptionText;
    public Button startListeningButton;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;

    // Core services
    private ServiceManager serviceManager;
    private VoiceSDKManager voiceManager;
    private AudioSource audioSource;
    private Animator animator;
    private bool isListening = false;
    private bool isProcessing = false;  // Added to prevent overlapping processing

    private void Start()
    {
        // Validate context
        if (context == null)
        {
            LogMessage("No CharacterContext assigned to AICharacterController!", true);
            enabled = false;
            return;
        }

        InitializeServices();
        SetupAudioSource();
        SetupUI();
    }

    private void InitializeServices()
    {
        // Get or create ServiceManager
        serviceManager = FindFirstObjectByType<ServiceManager>();
        if (serviceManager == null)
        {
            LogMessage("ServiceManager not found in scene!", true);
            var serviceObj = new GameObject("ServiceManager");
            serviceManager = serviceObj.AddComponent<ServiceManager>();
            LogMessage("Created new ServiceManager, but it may not be properly configured!", true);
        }
        else
        {
            LogMessage("Found existing ServiceManager");
        }

        // Get or create VoiceSDKManager
        voiceManager = FindFirstObjectByType<VoiceSDKManager>();
        if (voiceManager == null)
        {
            LogMessage("VoiceSDKManager not found in scene!", true);
            var voiceObj = new GameObject("VoiceInput");
            voiceManager = voiceObj.AddComponent<VoiceSDKManager>();
            LogMessage("Created new VoiceSDKManager, but it may not be properly configured!", true);
        }
        else
        {
            LogMessage("Found existing VoiceSDKManager");
        }

        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            LogMessage("No AudioSource component found!", true);
            audioSource = gameObject.AddComponent<AudioSource>();
            LogMessage("Added AudioSource component");
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            LogMessage("No Animator component found, animations will not work", true);
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null) return;
        
        audioSource.spatialBlend = 1.0f;
        audioSource.spread = 60.0f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.maxDistance = 10.0f;
        audioSource.minDistance = 1.0f;
        LogMessage("AudioSource configured");
    }

    private void SetupUI()
    {
        if (startListeningButton != null)
        {
            // Remove existing listeners to avoid duplicates
            startListeningButton.onClick.RemoveAllListeners();
            startListeningButton.onClick.AddListener(StartListening);
            LogMessage("Start Listening button configured");
        }
    }

    public async void StartListening()
    {
        if (isListening) 
        {
            LogMessage("Already listening, ignoring request");
            return;
        }

        isListening = true;
        if (startListeningButton != null) startListeningButton.interactable = false;

        try
        {
            LogMessage("Starting listening async via VoiceManager");
            string transcription = await voiceManager.StartListeningAsync();
            LogMessage($"Received transcription: {transcription}");
            
            if (transcriptionText != null)
            {
                transcriptionText.text = transcription;
            }

            if (!string.IsNullOrEmpty(transcription))
            {
                await ProcessUserInput(transcription);
            }
            else
            {
                LogMessage("Empty transcription received from voice manager", true);
            }
        }
        catch (System.Exception e)
        {
            LogMessage($"Error during voice input: {e.Message}", true);
            LogMessage($"Stack trace: {e.StackTrace}", true);
        }
        finally
        {
            isListening = false;
            if (startListeningButton != null) startListeningButton.interactable = true;
        }
    }

    public async Task ProcessUserInput(string userInput)
    {
        // Prevent overlapping processing
        if (isProcessing)
        {
            LogMessage("Already processing input, queuing is not implemented yet", true);
            return;
        }

        isProcessing = true;
        
        try
        {
            LogMessage($"Processing input: {userInput}");
            
            if (string.IsNullOrWhiteSpace(userInput))
            {
                LogMessage("Empty input received, ignoring", true);
                return;
            }
            
            if (context == null)
            {
                LogMessage("No character context assigned!", true);
                return;
            }

            if (serviceManager == null)
            {
                LogMessage("ServiceManager is null! Cannot process input.", true);
                // Try to find it again as a last resort
                serviceManager = FindFirstObjectByType<ServiceManager>();
                if (serviceManager == null)
                {
                    LogMessage("Still cannot find ServiceManager after retry", true);
                    return;
                }
                LogMessage("Found ServiceManager on retry");
            }

            // Log before sending to ServiceManager
            LogMessage($"Sending to ServiceManager");
            var response = await serviceManager.ProcessUserInput(userInput, transform, context);
            
            if (response != null)
            {
                LogMessage($"Response complete. Audio clip length: {response.length}s");
            }
            else
            {
                LogMessage("Received null response from ServiceManager", true);
            }
        }
        catch (System.Exception e)
        {
            LogMessage($"Error processing input: {e.Message}", true);
            LogMessage($"Stack trace: {e.StackTrace}", true);
        }
        finally
        {
            isProcessing = false;
        }
    }

    // For testing
    public void TestWithText(string text)
    {
        LogMessage($"Testing with text: {text}");
        _ = ProcessUserInput(text);
    }
    
    private void LogMessage(string message, bool isError = false)
    {
        if (!enableDebugLogging && !isError) return;
        
        if (isError)
            Debug.LogError($"[AICharacterController] {message}");
        else
            Debug.Log($"[AICharacterController] {message}");
    }

    private void OnDestroy()
    {
        if (startListeningButton != null)
        {
            startListeningButton.onClick.RemoveListener(StartListening);
        }
    }
}