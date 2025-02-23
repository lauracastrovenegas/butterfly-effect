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
    private TaskCompletionSource<string> currentTranscriptionTask;

    public TextMeshProUGUI transcriptionText; // Optional: For debugging

    private void Start()
    {
        // Get or add AppVoiceExperience
        voiceExperience = gameObject.GetComponent<AppVoiceExperience>();
        if (voiceExperience == null)
        {
            voiceExperience = gameObject.AddComponent<AppVoiceExperience>();
        }

        // Setup voice callbacks
        voiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartedListening);
        voiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscriptionReceived);
        voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscriptionReceived);
        voiceExperience.VoiceEvents.OnError.AddListener(OnError);
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

        voiceExperience.Activate();

        return await currentTranscriptionTask.Task;
    }

    public void StopListening()
    {
        if (!isListening) return;

        voiceExperience.Deactivate();
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