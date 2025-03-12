using UnityEngine;
using System;

/// <summary>
/// Simplified audio manager that directly plays audio without complex queueing or pooling
/// </summary>
public class DirectAudioManager : MonoBehaviour
{
    // Static instance for easy access
    public static DirectAudioManager Instance { get; private set; }
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.8f;
    public bool debugLogs = true;
    
    private void Awake()
    {
        // Simple singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogMessage("DirectAudioManager initialized");
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Play audio at a specified position in 3D space
    /// </summary>
    public void PlaySpatialAudio(AudioClip clip, Transform location)
    {
        if (clip == null || location == null)
        {
            LogMessage("Cannot play audio - clip or location is null", true);
            return;
        }
        
        try
        {
            // Create or get AudioSource for this location
            AudioSource source = GetAudioSourceAt(location);
            
            // Configure and play
            source.clip = clip;
            source.volume = defaultVolume;
            source.spatialBlend = 1.0f; // 3D spatial audio
            source.Play();
            
            LogMessage($"Playing clip '{clip.name}' at {location.name}, length: {clip.length}s");
        }
        catch (Exception ex)
        {
            LogMessage($"Error playing spatial audio: {ex.Message}", true);
        }
    }
    
    /// <summary>
    /// Play audio as a 2D sound (not positioned in space)
    /// </summary>
    public void PlayGlobalAudio(AudioClip clip)
    {
        if (clip == null)
        {
            LogMessage("Cannot play audio - clip is null", true);
            return;
        }
        
        try
        {
            // Create a temporary GameObject for this sound
            GameObject tempGO = new GameObject($"TempAudio_{clip.name}");
            tempGO.transform.parent = transform;
            AudioSource source = tempGO.AddComponent<AudioSource>();
            
            // Configure and play
            source.clip = clip;
            source.volume = defaultVolume;
            source.spatialBlend = 0f; // 2D non-spatial audio
            source.Play();
            
            // Destroy after playing
            Destroy(tempGO, clip.length + 0.1f);
            
            LogMessage($"Playing global clip '{clip.name}', length: {clip.length}s");
        }
        catch (Exception ex)
        {
            LogMessage($"Error playing global audio: {ex.Message}", true);
        }
    }
    
    /// <summary>
    /// Gets or creates an AudioSource component at the specified location
    /// </summary>
    private AudioSource GetAudioSourceAt(Transform location)
    {
        AudioSource source = location.GetComponent<AudioSource>();
        
        // Add AudioSource if it doesn't exist
        if (source == null)
        {
            source = location.gameObject.AddComponent<AudioSource>();
            LogMessage($"Added AudioSource to {location.name}");
        }
        
        // Configure source
        source.spatialBlend = 1.0f; // 3D sound
        source.minDistance = 1.0f;
        source.maxDistance = 30.0f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.playOnAwake = false;
        
        return source;
    }
    
    private void LogMessage(string message, bool isError = false)
    {
        if (!debugLogs && !isError) return;
        
        if (isError)
        {
            Debug.LogError($"[DirectAudioManager] {message}");
        }
        else
        {
            Debug.Log($"[DirectAudioManager] {message}");
        }
    }
}