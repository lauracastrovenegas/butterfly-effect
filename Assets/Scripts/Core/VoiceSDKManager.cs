using UnityEngine;
using Meta.Voice;
using Meta.WitAi;
using Meta.WitAi.Json;
using System;
using System.Threading.Tasks;
using TMPro;
using Meta.WitAi.Requests;
using Oculus.Voice;

public class VoiceSDKManager : MonoBehaviour
{
    private AppVoiceExperience voiceExperience;
    private bool isListening = false;
    private bool isProcessing = false; // Track when we're processing input
    private TaskCompletionSource<string> currentTranscriptionTask;
    
    // Add tracking for last processed transcription to prevent duplicates
    private string lastProcessedTranscription = "";
    private float duplicatePreventionTimeWindow = 3.0f;
    private float lastTranscriptionTime = 0;
    
    [Header("Debug Settings")]
    [SerializeField] private bool autoActivateVoice = true;
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float activationInterval = 0.5f; // Reduced from 5f to 0.5f for quicker reactivation
    private float activationTimer = 0f;
    [SerializeField] private float reactivationDelay = 0.2f; // Short delay before reactivating

    [Header("UI References")]
    public TextMeshProUGUI transcriptionText; // Optional: For debugging

    private void Start()
    {
        // Get the AppVoiceExperience component
        voiceExperience = GetComponent<AppVoiceExperience>();
        if (voiceExperience == null)
        {
            LogMessage("AppVoiceExperience component not found!", true);
            enabled = false;
            return;
        }

        // Setup voice callbacks
        SetupVoiceCallbacks();
        
        LogMessage("VoiceSDKManager initialized");
        LogMessage($"Available microphones: {string.Join(", ", Microphone.devices)}");
        
        // Activate voice input immediately at start
        ActivateVoiceInput();
    }

    private void SetupVoiceCallbacks()
    {
        voiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartedListening);
        voiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscriptionReceived);
        voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscriptionReceived);
        voiceExperience.VoiceEvents.OnError.AddListener(OnError);
    }

    private void Update()
    {
        // Only try to reactivate if not currently listening or processing
        if (autoActivateVoice && !isListening && !isProcessing)
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
        if (isListening || isProcessing)
        {
            LogMessage("Already listening or processing");
            return;
        }

        isListening = true;
        voiceExperience.Activate();
        LogMessage("Voice recognition activated");
    }

    public async Task<string> StartListeningAsync()
    {
        if (isListening)
        {
            LogMessage("Already listening for voice input");
            return null;
        }

        LogMessage("Starting listening async");
        currentTranscriptionTask = new TaskCompletionSource<string>();
        isListening = true;

        voiceExperience.Activate();

        return await currentTranscriptionTask.Task;
    }

    public void StopListening()
    {
        if (!isListening) return;

        voiceExperience.Deactivate();
        isListening = false;
        LogMessage("Stopped listening");
        
        // Schedule reactivation after a short delay
        if (autoActivateVoice)
        {
            StartImmediateReactivation();
        }
    }
    
    // New method to ensure quick reactivation
    private async void StartImmediateReactivation()
    {
        try
        {
            // Wait a short time before reactivating
            await Task.Delay((int)(reactivationDelay * 1000));
            
            // Only reactivate if we're not already listening or processing
            if (!isListening && !isProcessing)
            {
                LogMessage("Re-activating voice recognition after processing");
                ActivateVoiceInput();
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error during reactivation: {ex.Message}", true);
        }
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
        
        // Check for duplicate transcriptions
        if (IsDuplicateTranscription(transcription))
        {
            LogMessage("Skipping duplicate transcription");
            // Even for duplicates, ensure we're continuously listening
            StartImmediateReactivation();
            return;
        }
        
        // Automatically process transcription with character
        if (!string.IsNullOrEmpty(transcription))
        {
            var character = FindFirstObjectByType<AICharacterController>();
            if (character != null)
            {
                LogMessage($"Sending to character: {transcription}");
                isProcessing = true; // Mark that we're processing input
                // Fix for CS4014 warning - explicitly ignore the task with discard operator
                _ = ProcessTranscriptionAsync(character, transcription);
            }
            else
            {
                LogMessage("No AICharacterController found", true);
                // If no character was found, make sure we're still listening
                StartImmediateReactivation();
            }
        }
        else
        {
            // If empty transcription, make sure we're still listening
            StartImmediateReactivation();
        }
    }
    
    private bool IsDuplicateTranscription(string transcription)
    {
        // Check if this is the same as the last transcription and within time window
        if (transcription == lastProcessedTranscription && 
            (Time.time - lastTranscriptionTime) < duplicatePreventionTimeWindow)
        {
            return true;
        }
        
        // Update tracking
        lastProcessedTranscription = transcription;
        lastTranscriptionTime = Time.time;
        return false;
    }

    // Modified helper method to handle the async operation properly and ensure reactivation
    private async Task ProcessTranscriptionAsync(AICharacterController character, string transcription)
    {
        try
        {
            await character.ProcessUserInput(transcription);
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing transcription: {ex.Message}", true);
        }
        finally
        {
            isProcessing = false; // Mark that we're done processing
            // Ensure we return to listening state
            StartImmediateReactivation();
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
        isProcessing = false;
        
        // Even on error, ensure we return to listening state
        StartImmediateReactivation();
    }

    private void LogMessage(string message, bool isError = false)
    {
        if (!enableDebugLogs && !isError) return;
        
        if (isError)
            Debug.LogError($"[VoiceSDK] {message}");
        else
            Debug.Log($"[VoiceSDK] {message}");
    }

    private void OnDestroy()
    {
        if (voiceExperience != null)
        {
            voiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartedListening);
            voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscriptionReceived);
            voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscriptionReceived);
            voiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
        }
    }
}