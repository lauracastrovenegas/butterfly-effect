using UnityEngine;
using UnityEngine.Events;

// This script extends the Meta Poke Interactable with additional functionality
public class PokeInteractableHelper : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnPoked = new UnityEvent();
    
    [Header("Visual Feedback")]
    public Material defaultMaterial;
    public Material highlightedMaterial;
    public Material pokedMaterial;
    public MeshRenderer buttonRenderer;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pokeSound;
    
    [Header("Animation")]
    public bool animateOnPoke = true;
    public float moveDistance = 0.005f;
    public float returnDelay = 0.2f;
    
    private Vector3 originalPosition;
    private Vector3 pokedPosition;
    private bool isPoking = false;
    
    private void Start()
    {
        // Store original position
        originalPosition = transform.localPosition;
        pokedPosition = originalPosition - transform.forward * moveDistance;
        
        // Set default material
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
        
        // Setup audio source if needed
        if (audioSource == null && pokeSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    // Called by Meta Poke Interactable's events
    public void OnPokeEnter()
    {
        if (!isPoking)
        {
            isPoking = true;
            
            // Change material
            if (buttonRenderer != null && pokedMaterial != null)
            {
                buttonRenderer.material = pokedMaterial;
            }
            
            // Move button
            if (animateOnPoke)
            {
                transform.localPosition = pokedPosition;
            }
            
            // Play sound
            if (audioSource != null && pokeSound != null)
            {
                audioSource.PlayOneShot(pokeSound);
            }
            
            // Invoke the poked event
            OnPoked?.Invoke();
            
            // Return the button after a delay
            Invoke("ResetButton", returnDelay);
        }
    }
    
    public void OnPokeHover()
    {
        if (!isPoking && buttonRenderer != null && highlightedMaterial != null)
        {
            buttonRenderer.material = highlightedMaterial;
        }
    }
    
    public void OnPokeExit()
    {
        if (!isPoking && buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
    }
    
    private void ResetButton()
    {
        isPoking = false;
        
        // Reset button position
        if (animateOnPoke)
        {
            transform.localPosition = originalPosition;
        }
        
        // Reset material
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
    }
}