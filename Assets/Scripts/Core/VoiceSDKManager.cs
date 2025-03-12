using UnityEngine;
using Meta.Voice;
using Meta.WitAi;
using Meta.WitAi.Json;
using System;
using System.Threading.Tasks;
using TMPro;
using Meta.WitAi.Requests;
using Oculus.Voice;
using System.Collections;

public class VoiceSDKManager : MonoBehaviour
{
    [Header("Voice Component Reference")]
    [Tooltip("Drag the AppVoiceExperience component here")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    
    // Reference to AICharacterController - added to maintain a direct reference
    [SerializeField] private AICharacterController characterController;
    
    private bool isListening = false;
    private bool isActivating = false;
    private TaskCompletionSource<string> currentTranscriptionTask;
    
    [Header("Debug Settings")]
    [SerializeField] private bool autoActivateVoice = true;
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float activationInterval = 5f;
    [SerializeField] private float deactivationDelay = 0.5f;
    private float activationTimer = 0f;

    [Header("UI References")]
    public TextMeshProUGUI transcriptionText; // Optional: For debugging

    private void Start()
    {
        // Use the serialized field reference if provided
        if (appVoiceExperience != null)
        {
            LogMessage("Using assigned AppVoiceExperience reference");
        }
        // Otherwise try to get the component from this GameObject
        else
        {
            LogMessage("No AppVoiceExperience assigned, trying GetComponent");
            appVoiceExperience = GetComponent<AppVoiceExperience>();
        }
        
        // Check if we have a valid reference now
        if (appVoiceExperience == null)
        {
            // As a last resort, try to find it in the scene
            appVoiceExperience = FindFirstObjectByType<AppVoiceExperience>();
            
            if (appVoiceExperience == null)
            {
                LogMessage("AppVoiceExperience component not found anywhere! Voice recognition will not work.", true);
                enabled = false;
                return;
            }
            else
            {
                LogMessage("Found AppVoiceExperience in scene");
            }
        }

        // Find and cache character controller reference if not set in inspector
        if (characterController == null)
        {
            characterController = FindFirstObjectByType<AICharacterController>();
            if (characterController == null)
            {
                LogMessage("No AICharacterController found in scene! Character won't respond to voice.", true);
            }
            else
            {
                LogMessage("Found AICharacterController in scene");
            }
        }

        // Setup voice callbacks
        SetupVoiceCallbacks();
        
        LogMessage("VoiceSDKManager initialized");
        LogMessage($"Available microphones: {string.Join(", ", Microphone.devices)}");
    }

    private void SetupVoiceCallbacks()
    {
        // Check if callbacks are already registered to avoid duplicates
        appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartedListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscriptionReceived);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscriptionReceived);
        appVoiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
        
        // Now register them
        appVoiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartedListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscriptionReceived);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscriptionReceived);
        appVoiceExperience.VoiceEvents.OnError.AddListener(OnError);
        
        LogMessage("Voice callbacks set up successfully");
    }

    private void Update()
    {
        if (autoActivateVoice && !isListening && !isActivating)
        {
            activationTimer += Time.deltaTime;
            if (activationTimer >= activationInterval)
            {
                activationTimer = 0f;
                LogMessage("Auto-activating voice recognition");
                ActivateVoiceInput();
            }
        }
    }

    public void ActivateVoiceInput()
    {
        if (appVoiceExperience == null)
        {
            LogMessage("Cannot activate - AppVoiceExperience is null", true);
            return;
        }
        
        if (isListening || isActivating)
        {
            LogMessage("Already listening or activating");
            return;
        }

        // Force deactivate first to ensure clean state
        StartCoroutine(SafeActivate());
    }

    private IEnumerator SafeActivate()
    {
        isActivating = true;
        
        // First, ensure we're deactivated
        bool needsDelay = false;
        
        if (appVoiceExperience.Active)
        {
            LogMessage("Deactivating voice before reactivation");
            try 
            {
                appVoiceExperience.Deactivate();
                needsDelay = true;
            }
            catch (Exception ex)
            {
                LogMessage($"Error during deactivation: {ex.Message}", true);
            }
        }
        
        // Delay after deactivation - outside try block to avoid yield in try with catch
        if (needsDelay)
        {
            yield return new WaitForSeconds(deactivationDelay);
        }
        
        // Check and end any microphone recordings
        bool microphoneWasRecording = false;
        
        if (Microphone.IsRecording(null))
        {
            LogMessage("Microphone is already recording. Ending all recordings first", true);
            try
            {
                foreach (string device in Microphone.devices)
                {
                    if (Microphone.IsRecording(device))
                    {
                        Microphone.End(device);
                        microphoneWasRecording = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error ending microphone recording: {ex.Message}", true);
            }
        }
        
        // Delay after stopping microphone - outside try block
        if (microphoneWasRecording)
        {
            yield return new WaitForSeconds(deactivationDelay);
        }
            
        // Now try to activate
        LogMessage("Activating voice recognition now");
        try
        {
            isListening = true;
            appVoiceExperience.Activate();
        }
        catch (Exception ex)
        {
            LogMessage($"Error during activation: {ex.Message}", true);
            isListening = false;
        }
        
        isActivating = false;
    }

    public async Task<string> StartListeningAsync()
    {
        if (appVoiceExperience == null)
        {
            LogMessage("Cannot start listening - AppVoiceExperience is null", true);
            return null;
        }
        
        if (isListening || isActivating)
        {
            LogMessage("Already listening or activating");
            return null;
        }

        LogMessage("Starting listening async");
        currentTranscriptionTask = new TaskCompletionSource<string>();
        StartCoroutine(SafeActivate());

        return await currentTranscriptionTask.Task;
    }

    public void StopListening()
    {
        if (appVoiceExperience == null || !isListening) return;

        try
        {
            appVoiceExperience.Deactivate();
        }
        catch (Exception ex)
        {
            LogMessage($"Error deactivating: {ex.Message}", true);
        }
        
        isListening = false;
        LogMessage("Stopped listening");
    }

    private void OnStartedListening()
    {
        LogMessage("Started listening for voice input");
        if (transcriptionText != null)
        {
            transcriptionText.text = "Listening...";
        }
    }

    private void OnStoppedListening()
    {
        LogMessage("Stopped listening for voice input");
        isListening = false;
    }

    private void OnPartialTranscriptionReceived(string transcription)
    {
        LogMessage($"Partial: {transcription}");
        if (transcriptionText != null)
        {
            transcriptionText.text = $"Hearing: {transcription}";
        }
    }

    private void OnFullTranscriptionReceived(string transcription)
    {
        LogMessage($"Full transcription: {transcription}");
        
        if (transcriptionText != null)
        {
            transcriptionText.text = transcription;
        }

        if (currentTranscriptionTask != null && !currentTranscriptionTask.Task.IsCompleted)
        {
            currentTranscriptionTask.SetResult(transcription);
        }

        isListening = false;
        
        // Debug statement to check if transcription is empty
        if (string.IsNullOrEmpty(transcription))
        {
            LogMessage("WARNING: Empty transcription received!", true);
            return; // Added return to prevent processing empty transcriptions
        }
        
        // Process transcription with character
        if (characterController != null)
        {
            LogMessage($"Sending to character: {transcription}");
            try 
            {
                // Changed to properly await the task
                ProcessTranscriptionAsync(characterController, transcription).ConfigureAwait(false);
                LogMessage("Successfully started processing transcription");
            }
            catch (Exception ex) 
            {
                LogMessage($"ERROR in processing: {ex.Message}", true);
                LogMessage($"Stack trace: {ex.StackTrace}", true);
            }
        }
        else
        {
            LogMessage("No AICharacterController found", true);
            // Try to find character controller again as a last resort
            characterController = FindFirstObjectByType<AICharacterController>();
            if (characterController != null)
            {
                LogMessage("Found AICharacterController, attempting to process transcription");
                ProcessTranscriptionAsync(characterController, transcription).ConfigureAwait(false);
            }
        }
    }

    // Modified to ensure proper async handling
    private async Task ProcessTranscriptionAsync(AICharacterController character, string transcription)
    {
        try
        {
            LogMessage("Starting ProcessUserInput on character");
            await character.ProcessUserInput(transcription);
            LogMessage("Finished ProcessUserInput on character");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing transcription: {ex.Message}", true);
            LogMessage($"Stack trace: {ex.StackTrace}", true);
        }
    }

    private void OnError(string error, string message)
    {
        LogMessage($"Error: {error} - {message}", true);
        
        if (currentTranscriptionTask != null && !currentTranscriptionTask.Task.IsCompleted)
        {
            currentTranscriptionTask.SetException(new Exception($"Voice SDK Error: {error} - {message}"));
        }

        if (transcriptionText != null)
        {
            transcriptionText.text = "Error occurred while listening";
        }

        isListening = false;
    }

    public void LogMessage(string message, bool isError = false)
    {
        if (!enableDebugLogs) return;
        
        if (isError)
            Debug.LogError($"[VoiceSDK] {message}");
        else
            Debug.Log($"[VoiceSDK] {message}");
    }

    private void OnDestroy()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartedListening);
            appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscriptionReceived);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscriptionReceived);
            appVoiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
        }
    }
}