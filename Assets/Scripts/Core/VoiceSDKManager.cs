using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Json;
using System;
using System.Threading.Tasks;
using TMPro;

[RequireComponent(typeof(AppVoiceExperience))]
public class VoiceSDKManager : MonoBehaviour
{
    private AppVoiceExperience appVoiceExperience;
    private bool isListening = false;
    private TaskCompletionSource<string> currentTranscriptionTask;

    public TextMeshProUGUI transcriptionText; // Optional: For debugging/UI

    private void Awake()
    {
        appVoiceExperience = GetComponent<AppVoiceExperience>();
        
        // Set up voice experience callbacks
        appVoiceExperience.events.OnStartListening.AddListener(OnStartedListening);
        appVoiceExperience.events.OnStoppedListening.AddListener(OnStoppedListening);
        appVoiceExperience.events.OnFullTranscription.AddListener(OnFullTranscriptionReceived);
        appVoiceExperience.events.OnPartialTranscription.AddListener(OnPartialTranscriptionReceived);
        appVoiceExperience.events.OnError.AddListener(OnError);
    }

    public async Task<string> StartListeningAsync()
    {
        if (isListening)
        {
            Debug.LogWarning("Already listening for voice input");
            return null;
        }

        currentTranscriptionTask = new TaskCompletionSource<string>();
        isListening = true;
        appVoiceExperience.Activate();

        return await currentTranscriptionTask.Task;
    }

    public void StopListening()
    {
        if (!isListening) return;

        appVoiceExperience.Deactivate();
        isListening = false;
    }

    private void OnStartedListening()
    {
        Debug.Log("Started listening for voice input");
        if (transcriptionText != null)
        {
            transcriptionText.text = "Listening...";
        }
    }

    private void OnStoppedListening()
    {
        Debug.Log("Stopped listening for voice input");
        isListening = false;
    }

    private void OnPartialTranscriptionReceived(string transcription)
    {
        if (transcriptionText != null)
        {
            transcriptionText.text = $"Hearing: {transcription}";
        }
    }

    private void OnFullTranscriptionReceived(string transcription)
    {
        Debug.Log($"Full transcription received: {transcription}");
        
        if (transcriptionText != null)
        {
            transcriptionText.text = transcription;
        }

        if (currentTranscriptionTask != null && !currentTranscriptionTask.Task.IsCompleted)
        {
            currentTranscriptionTask.SetResult(transcription);
        }

        isListening = false;
    }

    private void OnError(string error, string message)
    {
        Debug.LogError($"Voice SDK Error: {error} - {message}");
        
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

    private void OnDestroy()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.events.OnStartListening.RemoveListener(OnStartedListening);
            appVoiceExperience.events.OnStoppedListening.RemoveListener(OnStoppedListening);
            appVoiceExperience.events.OnFullTranscription.RemoveListener(OnFullTranscriptionReceived);
            appVoiceExperience.events.OnPartialTranscription.RemoveListener(OnPartialTranscriptionReceived);
            appVoiceExperience.events.OnError.RemoveListener(OnError);
        }
    }
}