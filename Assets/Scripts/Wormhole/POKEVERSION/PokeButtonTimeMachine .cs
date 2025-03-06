using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PokeButtonTimeMachine : MonoBehaviour
{
    [Header("Scene Settings")]
    public string wormholeSceneName = "CutSceneWorm";
    public string destinationSceneName; // Will be set automatically based on current scene
    
    [Header("Audio")]
    public AudioClip initializationSound;
    private AudioSource audioSource;
    
    // Flag to prevent multiple activations
    private bool isActivated = false;
    
    private void Awake()
    {
        // Get or add an audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set destination scene based on current scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Lab")
        {
            destinationSceneName = "Workshop";
        }
        else if (currentScene == "Workshop")
        {
            destinationSceneName = "Lab";
        }
        
        // Store destination scene name in PlayerPrefs for the wormhole scene to read
        PlayerPrefs.SetString("DestinationScene", destinationSceneName);
        PlayerPrefs.Save();
    }
    
    // This method will be called by the Poke Interactable
    public void OnButtonPoked()
    {
        if (!isActivated)
        {
            isActivated = true;
            StartCoroutine(TimeMachineSequence());
        }
    }
    
    private IEnumerator TimeMachineSequence()
    {
        // Play initialization sound
        if (audioSource != null && initializationSound != null)
        {
            audioSource.clip = initializationSound;
            audioSource.Play();
            
            // Wait for the sound to complete
            yield return new WaitForSeconds(initializationSound.length);
        }
        else
        {
            // If no sound, just wait a moment
            yield return new WaitForSeconds(1.0f);
        }
        
        // Load the wormhole scene
        SceneManager.LoadScene(wormholeSceneName);
    }
}