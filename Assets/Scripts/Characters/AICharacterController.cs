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
            var serviceObj = new GameObject("ServiceManager");
            serviceManager = serviceObj.AddComponent<ServiceManager>();
        }

        // Get or create VoiceSDKManager
        voiceManager = Object.FindFirstObjectByType<VoiceSDKManager>();
        if (voiceManager == null)
        {
            var voiceObj = new GameObject("VoiceInput");
            voiceManager = voiceObj.AddComponent<VoiceSDKManager>();
        }

        // Get components
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>(); // Optional
    }

    private void SetupAudioSource()
    {
        audioSource.spatialBlend = 1.0f;
        audioSource.spread = 60.0f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.maxDistance = 10.0f;
        audioSource.minDistance = 1.0f;
    }

    private void SetupUI()
    {
        if (startListeningButton != null)
        {
            startListeningButton.onClick.AddListener(StartListening);
        }
    }

    public async void StartListening()
    {
        if (isListening) return;

        isListening = true;
        if (startListeningButton != null) startListeningButton.interactable = false;

        try
        {
            string transcription = await voiceManager.StartListeningAsync();
            
            if (transcriptionText != null)
            {
                transcriptionText.text = transcription;
            }

            if (!string.IsNullOrEmpty(transcription))
            {
                await ProcessUserInput(transcription);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during voice input: {e.Message}");
        }
        finally
        {
            isListening = false;
            if (startListeningButton != null) startListeningButton.interactable = true;
        }
    }

    public async Task ProcessUserInput(string userInput)
    {
        if (context == null)
        {
            Debug.LogError("No character context assigned!");
            return;
        }

        try
        {
            await serviceManager.ProcessUserInput(userInput, transform);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing input: {e.Message}");
        }
    }

    // For testing
    public void TestWithText(string text)
    {
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