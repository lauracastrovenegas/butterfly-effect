using UnityEngine;
using UnityEngine.Events;

// This is a simplified button for use with Meta Quest Interaction SDK
public class MetaButtonInteractable : MonoBehaviour
{
    [Header("Button Configuration")]
    [Tooltip("How much the button moves when pressed")]
    public float pressDistance = 0.01f;
    
    [Tooltip("An event that's triggered when the button is pressed")]
    public UnityEvent OnButtonPressed;
    
    [Header("Visual Feedback")]
    public MeshRenderer buttonRenderer;
    public Material defaultMaterial;
    public Material pressedMaterial;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonPressSound;
    
    // Internal variables
    private Transform originalTransform;
    private Vector3 startPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    
    private void Start()
    {
        // Store original position
        originalTransform = transform;
        startPosition = transform.localPosition;
        pressedPosition = startPosition - transform.forward * pressDistance;
        
        // Set initial material
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
        
        // Initialize audio source if needed
        if (audioSource == null && buttonPressSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    // This can be called by Meta's Direct Interactor on "Select"
    public void OnButtonPress()
    {
        if (!isPressed)
        {
            // Update state
            isPressed = true;
            
            // Visual feedback
            if (buttonRenderer != null && pressedMaterial != null)
            {
                buttonRenderer.material = pressedMaterial;
            }
            
            // Move button
            transform.localPosition = pressedPosition;
            
            // Play sound
            if (audioSource != null && buttonPressSound != null)
            {
                audioSource.PlayOneShot(buttonPressSound);
            }
            
            // Invoke the event
            OnButtonPressed?.Invoke();
            
            // Reset after a short delay
            Invoke("ResetButton", 0.5f);
        }
    }
    
    private void ResetButton()
    {
        // Reset state
        isPressed = false;
        
        // Reset visuals
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
        
        // Reset position
        transform.localPosition = startPosition;
    }
}