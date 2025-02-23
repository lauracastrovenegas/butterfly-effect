using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AITestSystem : MonoBehaviour
{
    [Header("Character References")]
    public AICharacterController characterController;

    [Header("Test UI")]
    public Button testButton;
    public TMP_InputField userInputField;
    public TextMeshProUGUI responseText;
    public TextMeshProUGUI markerText;
    public TextMeshProUGUI stateText;

    [Header("Quick Test Phrases")]
    [SerializeField] private string[] testPhrases = new string[]
    {
        "What are you working on right now?",
        "Tell me about the Mona Lisa",
        "How are the measurements coming along?",
        "What inventions are you designing?"
    };
    private int currentPhraseIndex = 0;

    private void Start()
    {
        // Setup basic UI functionality
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestNextPhrase);
        }

        // Add keyboard input listener
        if (userInputField != null)
        {
            userInputField.onSubmit.AddListener(OnUserInput);
        }

        // Subscribe to ServiceManager events for markers
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger += OnMarkerReceived;
        }

        UpdateStateDisplay();
    }

    public void TestNextPhrase()
    {
        if (testPhrases == null || testPhrases.Length == 0) return;

        string phrase = testPhrases[currentPhraseIndex];
        currentPhraseIndex = (currentPhraseIndex + 1) % testPhrases.Length;

        if (userInputField != null)
        {
            userInputField.text = phrase;
        }
        
        ProcessInput(phrase);
    }

    public void OnUserInput(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            ProcessInput(input);
        }
    }

    private async void ProcessInput(string input)
    {
        if (characterController == null)
        {
            Debug.LogError("No AICharacterController assigned!");
            return;
        }

        if (responseText != null)
        {
            responseText.text = "Processing...";
        }

        await characterController.ProcessUserInput(input);
    }

    private void OnMarkerReceived(string marker)
    {
        if (markerText != null)
        {
            markerText.text = $"Current Action: {marker}";
        }
    }

    private void UpdateStateDisplay()
    {
        if (stateText != null)
        {
            stateText.text = $"Test System Ready\nUse input field or test button";
        }
    }

    private void OnDestroy()
    {
        if (testButton != null)
        {
            testButton.onClick.RemoveListener(TestNextPhrase);
        }

        if (userInputField != null)
        {
            userInputField.onSubmit.RemoveListener(OnUserInput);
        }

        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger -= OnMarkerReceived;
        }
    }
}