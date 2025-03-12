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

    // Core services
    private ServiceManager serviceManager;
    private VoiceSDKManager voiceManager;
    private AudioSource audioSource;
    private Animator animator;
    private bool isListening = false;

    private void Start()
    {
        // Validate context
        if (context == null)
        {
            Debug.LogError("No CharacterContext assigned to AICharacterController!");
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
        serviceManager = Object.FindFirstObjectByType<ServiceManager>();
        if (serviceManager == null)
        {
            Debug.LogError("ServiceManager not found in scene!");
            var serviceObj = new GameObject("ServiceManager");
            serviceManager = serviceObj.AddComponent<ServiceManager>();
            Debug.LogWarning("Created new ServiceManager, but it may not be properly configured!");
        }
        else
        {
            Debug.Log("Found existing ServiceManager");
        }

        // Get or create VoiceSDKManager
        voiceManager = Object.FindFirstObjectByType<VoiceSDKManager>();
        if (voiceManager == null)
        {
            Debug.LogError("VoiceSDKManager not found in scene!");
            var voiceObj = new GameObject("VoiceInput");
            voiceManager = voiceObj.AddComponent<VoiceSDKManager>();
            Debug.LogWarning("Created new VoiceSDKManager, but it may not be properly configured!");
        }
        else
        {
            Debug.Log("Found existing VoiceSDKManager");
        }

        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource component found!");
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
            startListeningButton.onClick.AddListener(StartListening);
            Debug.Log("Start Listening button configured");
        }
    }

    public async void StartListening()
    {
        if (isListening) return;

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
            return;
        }

        try
        {
            Debug.Log($"[AICharacterController] Sending to ServiceManager");
            var response = await serviceManager.ProcessUserInput(userInput, transform, context);
            Debug.Log($"[AICharacterController] Response complete. Audio clip length: {(response != null ? response.length : 0)}s");
            
            // If we got no response, something went wrong
            if (response == null)
            {
                Debug.LogError("Received null response from ServiceManager");
            }
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