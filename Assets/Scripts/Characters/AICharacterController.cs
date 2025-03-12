using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AICharacterController : MonoBehaviour
{
    [Header("Character Configuration")]
    [Tooltip("The context defines this character's personality and responses")]
    public CharacterContext context;

    [Header("Optional Debug UI")]
    public TextMeshProUGUI transcriptionText;
    public Button startListeningButton;
    
    // Core services
    private ServiceManager serviceManager;
    private VoiceSDKManager voiceManager;
    private AudioSource audioSource;
    private Animator animator;
    private bool isListening = false;
    private bool isInitialized = false;

    private void Awake()
    {
        // Create ServiceManager first if it doesn't exist
        if (ServiceManager.Instance == null)
        {
            var serviceObj = new GameObject("ServiceManager");
            serviceManager = serviceObj.AddComponent<ServiceManager>();
            Debug.Log("Created ServiceManager in Awake");
        }
        else
        {
            serviceManager = ServiceManager.Instance;
            Debug.Log("Found existing ServiceManager Instance");
        }
    }

    private void Start()
    {
        // Start delayed initialization to ensure ServiceManager is ready
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait a frame for other components to initialize
        yield return null;

        // Validate context
        if (context == null)
        {
            Debug.LogError("No CharacterContext assigned to AICharacterController!");
            context = FindFirstObjectByType<CharacterContext>();
            if (context != null)
            {
                Debug.Log("Found CharacterContext in scene");
            }
            else
            {
                enabled = false;
                Debug.LogError("Could not find any CharacterContext, disabling component");
                yield break;
            }
        }

        // Initialize services
        InitializeServices();
        SetupAudioSource();
        SetupUI();

        isInitialized = true;
        Debug.Log("AICharacterController initialization complete");
    }

    private void InitializeServices()
    {
        // Double-check ServiceManager
        if (serviceManager == null)
        {
            serviceManager = ServiceManager.Instance;
            if (serviceManager == null)
            {
                serviceManager = FindFirstObjectByType<ServiceManager>();
                if (serviceManager == null)
                {
                    Debug.LogError("ServiceManager still not found after delayed initialization!");
                    var serviceObj = new GameObject("ServiceManager");
                    serviceManager = serviceObj.AddComponent<ServiceManager>();
                    // Wait for initialization to happen automatically (via Awake)
                    Debug.Log("Created new ServiceManager during initialization");
                }
            }
        }

        // Get or create VoiceSDKManager
        voiceManager = FindFirstObjectByType<VoiceSDKManager>();
        if (voiceManager == null)
        {
            Debug.LogError("VoiceSDKManager not found in scene!");
            var voiceObj = new GameObject("VoiceInput");
            voiceManager = voiceObj.AddComponent<VoiceSDKManager>();
            Debug.Log("Created new VoiceSDKManager");
        }

        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource component found!");
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("Added AudioSource component");
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found!");
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
        Debug.Log("AudioSource configured");
    }

    private void SetupUI()
    {
        if (startListeningButton != null)
        {
            startListeningButton.onClick.RemoveAllListeners();
            startListeningButton.onClick.AddListener(StartListening);
            Debug.Log("Start Listening button configured");
        }
    }

    public async void StartListening()
    {
        if (isListening) return;
        if (!isInitialized)
        {
            Debug.LogError("Cannot start listening - not fully initialized");
            return;
        }

        isListening = true;
        if (startListeningButton != null) startListeningButton.interactable = false;

        try
        {
            Debug.Log("Starting listening async via VoiceManager");
            string transcription = await voiceManager.StartListeningAsync();
            Debug.Log($"Received transcription: {transcription}");
            
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
                Debug.LogWarning("Empty transcription received from voice manager");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during voice input: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
        finally
        {
            isListening = false;
            if (startListeningButton != null) startListeningButton.interactable = true;
        }
    }

    public async Task ProcessUserInput(string userInput)
    {
        Debug.Log($"[AICharacterController] Processing input: {userInput}");
        
        if (context == null)
        {
            Debug.LogError("No character context assigned!");
            return;
        }

        if (serviceManager == null)
        {
            Debug.LogError("ServiceManager is null! Cannot process input.");
            // One last attempt to find it
            serviceManager = ServiceManager.Instance;
            if (serviceManager == null)
            {
                serviceManager = FindFirstObjectByType<ServiceManager>();
                if (serviceManager == null)
                {
                    Debug.LogError("ServiceManager STILL not found, giving up");
                    return;
                }
                Debug.Log("Found ServiceManager on last attempt");
            }
        }

        try
        {
            Debug.Log($"[AICharacterController] Sending to ServiceManager");
            var response = await serviceManager.ProcessUserInput(userInput, transform, context);
            Debug.Log($"[AICharacterController] Response complete. Audio clip length: {(response != null ? response.length : 0)}s");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing input: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    // For testing
    public void TestWithText(string text)
    {
        Debug.Log($"Testing with text: {text}");
        _ = ProcessUserInput(text);
    }

    private void OnDestroy()
    {
        if (startListeningButton != null)
        {
            startListeningButton.onClick.RemoveListener(StartListening);
        }
    }
}