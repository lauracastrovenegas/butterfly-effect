using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Test system for the Da Vinci AI character interaction.
/// This script manages user input, displays responses, and coordinates
/// audio playback for testing the entire AI interaction system.
/// </summary>
public class AITestSystem : MonoBehaviour
{
    // Core character references
    [Header("Character Setup")]
    [Tooltip("The main AI character controller - this handles all AI interactions")]
    public AICharacterController characterController;

    // UI Elements for interaction
    [Header("Input Elements")]
    [Tooltip("The input field where users type their messages")]
    public TMP_InputField inputField;
    [Tooltip("Button that sends the current input text")]
    public Button sendButton;
    [Tooltip("Button that cycles through test phrases")]
    public Button testButton;

    // UI Elements for displaying responses
    [Header("Display Elements")]
    [Tooltip("Shows the AI's text responses")]
    public TextMeshProUGUI responseText;
    [Tooltip("Shows what action the AI is performing")]
    public TextMeshProUGUI markerText;
    [Tooltip("Shows the current system state")]
    public TextMeshProUGUI stateText;

    // Test phrases for quick testing
    [Header("Test Configuration")]
    [SerializeField]
    private string[] testPhrases = new string[]
    {
        "What are you working on right now?",
        "Tell me about the Mona Lisa",
        "How are the measurements coming along?",
        "What inventions are you designing?",
        "Can you measure my proportions?",
        "Why did you choose to paint her?"
    };

    // Internal state tracking
    private int currentPhraseIndex = 0;
    private bool isProcessing = false;

    private void Start()
    {
        InitializeSystem();
        SetupEventListeners();
        UpdateUIState("System Ready");
    }

    /// <summary>
    /// Sets up the initial system state and validates components
    /// </summary>
    private void InitializeSystem()
    {
        // Verify essential components
        if (characterController == null)
        {
            Debug.LogError("[AITestSystem] No Character Controller assigned! The system won't function without it.");
            enabled = false;
            return;
        }

        // Configure input field
        if (inputField != null)
        {
            inputField.characterLimit = 200;
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Ask Da Vinci something...";
        }

        // Enable UI elements
        EnableUserInput(true);
    }

    /// <summary>
    /// Sets up all the event listeners for UI interactions
    /// </summary>
    private void SetupEventListeners()
    {
        // Setup send button
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(HandleSendButton);
        }

        // Setup test button
        if (testButton != null)
        {
            testButton.onClick.AddListener(HandleTestButton);
        }

        // Setup input field submission
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(HandleUserInput);
        }

        // Listen for animation markers
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger += HandleAnimationMarker;
        }
    }

    /// <summary>
    /// Handles the send button click
    /// </summary>
    private void HandleSendButton()
    {
        if (inputField != null && !string.IsNullOrEmpty(inputField.text))
        {
            HandleUserInput(inputField.text);
        }
    }

    /// <summary>
    /// Handles the test button click, cycling through test phrases
    /// </summary>
    private void HandleTestButton()
    {
        if (isProcessing || testPhrases == null || testPhrases.Length == 0) 
            return;

        string phrase = testPhrases[currentPhraseIndex];
        currentPhraseIndex = (currentPhraseIndex + 1) % testPhrases.Length;

        if (inputField != null)
        {
            inputField.text = phrase;
        }
        
        HandleUserInput(phrase);
    }

    /// <summary>
    /// Processes user input and manages the response flow
    /// </summary>
    private async void HandleUserInput(string input)
    {
        if (isProcessing || string.IsNullOrEmpty(input))
            return;

        try
        {
            // Disable input while processing
            EnableUserInput(false);
            UpdateUIState("Processing...");

            // Show thinking state
            if (responseText != null)
            {
                responseText.text = "Thinking...";
            }

            // Process the input
            await ProcessInput(input);

            // Clear and refocus input field
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }
        finally
        {
            // Always re-enable input
            EnableUserInput(true);
            UpdateUIState("Ready");
        }
    }

    /// <summary>
    /// Core processing function that handles the AI interaction
    /// </summary>
    private async Task ProcessInput(string input)
    {
        try
        {
            // Get response from character
            await characterController.ProcessUserInput(input);
            
            // The audio will be handled by the character's AudioSource component
            // We just need to update the UI state
            UpdateUIState("Response complete");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AITestSystem] Error processing input: {e.Message}");
            UpdateUIState("Error occurred");
            if (responseText != null)
            {
                responseText.text = "Sorry, there was an error processing your request.";
            }
        }
    }

    /// <summary>
    /// Handles animation markers from the AI system
    /// </summary>
    private void HandleAnimationMarker(string marker)
    {
        if (markerText != null)
        {
            markerText.text = $"Current Action: {marker}";
        }
    }

    /// <summary>
    /// Updates the UI state display
    /// </summary>
    private void UpdateUIState(string state)
    {
        if (stateText != null)
        {
            stateText.text = $"System State: {state}";
        }
    }

    /// <summary>
    /// Enables or disables user input elements
    /// </summary>
    private void EnableUserInput(bool enable)
    {
        isProcessing = !enable;
        
        if (sendButton != null) sendButton.interactable = enable;
        if (testButton != null) testButton.interactable = enable;
        if (inputField != null) inputField.interactable = enable;
    }

    private void OnDestroy()
    {
        // Clean up all event listeners
        if (sendButton != null)
            sendButton.onClick.RemoveAllListeners();
        
        if (testButton != null)
            testButton.onClick.RemoveAllListeners();
        
        if (inputField != null)
            inputField.onSubmit.RemoveAllListeners();
        
        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnAnimationTrigger -= HandleAnimationMarker;
    }

    // Editor-only functionality
    #if UNITY_EDITOR
    private void Reset()
    {
        // Try to find components in the scene
        if (characterController == null)
            characterController = Object.FindFirstObjectByType<AICharacterController>();
    }
    #endif
}