using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleTimeMachine : MonoBehaviour
{
    [Header("Scene Names")]
    public string wormholeSceneName = "CutSceneWorm";
    public string destinationSceneName; // Will be set automatically based on current scene
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip initializationSound;
    
    private void Awake()
    {
        // Setup audio source if needed
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
    }
    
    // This will be called when button is pressed
    public void ActivateTimeMachine()
    {
        StartCoroutine(TransitionSequence());
    }
    
    private IEnumerator TransitionSequence()
    {
        // Play initialization sound
        if (audioSource != null && initializationSound != null)
        {
            audioSource.clip = initializationSound;
            audioSource.Play();
            
            // Wait for sound to complete
            yield return new WaitForSeconds(initializationSound.length);
        }
        
        // Load wormhole scene
        SceneManager.LoadScene(wormholeSceneName);
    }
}