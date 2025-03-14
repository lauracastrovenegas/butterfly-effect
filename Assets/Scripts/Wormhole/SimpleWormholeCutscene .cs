using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleWormholeCutscene : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip firstSound;      // Plays immediately
    public AudioClip secondSound;     // Plays after delay
    
    [Header("Timing")]
    public float secondSoundDelay = 5f;    // Time before second sound plays
    public float sceneTransitionDelay = 16f; // Total time before scene transition
    
    [Header("Visuals")]
    public GameObject wormholeEffect;
    
    private string destinationScene;
    
    void Start()
    {
        // Get destination from player prefs (set by the time machine controller)
        if (PlayerPrefs.HasKey("DestinationScene"))
        {
            destinationScene = PlayerPrefs.GetString("DestinationScene");
        }
        else
        {
            // Default to Workshop if no destination was specified
            destinationScene = "Workshop";
        }
        
        // Setup audio source if needed
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Start the cutscene sequence
        StartCoroutine(CutsceneSequence());
    }
    
    private IEnumerator CutsceneSequence()
    {
        // Play first sound immediately
        if (firstSound != null)
        {
            audioSource.clip = firstSound;
            audioSource.Play();
        }
        
        // Wait before playing second sound
        yield return new WaitForSeconds(secondSoundDelay);
        
        // Play second sound
        if (secondSound != null)
        {
            audioSource.clip = secondSound;
            audioSource.Play();
        }
        
        // Wait remaining time before scene transition
        float remainingTime = sceneTransitionDelay - secondSoundDelay;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Load destination scene
        SceneManager.LoadScene(destinationScene);
    }
}