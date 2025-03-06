using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSceneChanger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load after timer expires")]
    public string targetSceneName = "CutSceneWorm";
    
    [Tooltip("Time to wait before changing scenes (seconds)")]
    public float waitTime = 30f;
    
    [Header("Optional")]
    [Tooltip("Audio clip to play when timer starts")]
    public AudioClip startSound;
    
    [Tooltip("Audio clip to play before scene change")]
    public AudioClip endSound;
    
    [Tooltip("Time before scene change to play the end sound")]
    public float endSoundTime = 5f;
    
    // Private variables
    private AudioSource audioSource;
    private float timer = 0f;
    private bool endSoundPlayed = false;
    private bool isChangingScene = false;
    
    private void Start()
    {
        // Set destination scene in PlayerPrefs (for the wormhole scene to read)
        string currentScene = SceneManager.GetActiveScene().name;
        string destinationScene = (currentScene == "Lab") ? "Workshop" : "Lab";
        PlayerPrefs.SetString("DestinationScene", destinationScene);
        PlayerPrefs.Save();
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (startSound != null || endSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Play start sound if available
        if (audioSource != null && startSound != null)
        {
            audioSource.clip = startSound;
            audioSource.Play();
        }
        
        Debug.Log($"AutoSceneChanger will load {targetSceneName} after {waitTime} seconds");
    }
    
    private void Update()
    {
        if (isChangingScene)
            return;
            
        // Update timer
        timer += Time.deltaTime;
        
        // Play end sound if almost time to change scenes
        if (!endSoundPlayed && timer >= waitTime - endSoundTime && audioSource != null && endSound != null)
        {
            audioSource.clip = endSound;
            audioSource.Play();
            endSoundPlayed = true;
        }
        
        // Change scene when timer expires
        if (timer >= waitTime)
        {
            isChangingScene = true;
            Debug.Log($"Timer expired - loading {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
