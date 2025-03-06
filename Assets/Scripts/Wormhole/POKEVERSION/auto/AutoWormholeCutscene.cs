using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoWormholeCutscene : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip firstSound;      // Plays immediately
    public AudioClip secondSound;     // Plays after delay
    
    [Header("Timing")]
    public float secondSoundDelay = 5f;    // Time before second sound plays
    public float sceneTransitionDelay = 16f; // Total time before scene transition
    
    private AudioSource audioSource;
    private string destinationScene;
    
    void Start()
    {
        // Get destination from player prefs
        if (PlayerPrefs.HasKey("DestinationScene"))
        {
            destinationScene = PlayerPrefs.GetString("DestinationScene");
        }
        else
        {
            // Default to Workshop if no destination was specified
            destinationScene = "Workshop";
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        Debug.Log($"Wormhole cutscene starting, will transition to {destinationScene} after {sceneTransitionDelay} seconds");
        
        // Start the sequence
        StartCoroutine(CutsceneSequence());
    }
    
    private IEnumerator CutsceneSequence()
    {
        // Play first sound immediately
        if (firstSound != null)
        {
            audioSource.clip = firstSound;
            audioSource.Play();
            Debug.Log("Playing first sound");
        }
        
        // Wait before playing second sound
        yield return new WaitForSeconds(secondSoundDelay);
        
        // Play second sound
        if (secondSound != null)
        {
            audioSource.clip = secondSound;
            audioSource.Play();
            Debug.Log("Playing second sound");
        }
        
        // Wait remaining time before scene transition
        float remainingTime = sceneTransitionDelay - secondSoundDelay;
        if (remainingTime > 0)
        {
            Debug.Log($"Waiting {remainingTime} seconds before transitioning to {destinationScene}");
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Load destination scene
        Debug.Log($"Loading destination scene: {destinationScene}");
        SceneManager.LoadScene(destinationScene);
    }
}
