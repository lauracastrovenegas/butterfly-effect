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
    
    [Header("Scene Transition")]
    public string destinationScene = "Workshop"; // Default, will be overridden if passed from previous scene
    
    void Start()
    {
        // Get destination from player prefs (set by the time machine controller)
        if (PlayerPrefs.HasKey("DestinationScene"))
        {
            destinationScene = PlayerPrefs.GetString("DestinationScene");
        }
        
        // Setup audio if needed
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Start the cutscene sequence
        StartCoroutine(CutsceneSequence());
    }
    
    private IEnumerator CutsceneSequence()
    {
        // Play first sound immediately
        if (audioSource != null && firstSound != null)
        {
            audioSource.clip = firstSound;
            audioSource.Play();
        }
        
        // Wait before playing second sound
        yield return new WaitForSeconds(secondSoundDelay);
        
        // Play second sound
        if (audioSource != null && secondSound != null)
        {
            audioSource.clip = secondSound;
            audioSource.Play();
        }
        
        // Wait remaining time before scene transition
        float remainingTime = sceneTransitionDelay - secondSoundDelay;
        yield return new WaitForSeconds(remainingTime);
        
        // Load destination scene
        SceneManager.LoadScene(destinationScene);
    }
}