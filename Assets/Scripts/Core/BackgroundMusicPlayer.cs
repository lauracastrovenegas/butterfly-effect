using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;

/// <summary>
/// Simple component to play background music in a looping fashion.
/// Attach this script to a dedicated GameObject in your scene.
/// </summary>
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("The audio file to play as background music")]
    [SerializeField] private AudioClip musicClip;
    
    [Tooltip("Path to the audio file (relative to StreamingAssets folder)")]
    [SerializeField] private string musicFilePath = "Music/background_music.mp3";
    
    [Tooltip("Volume of the background music (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.3f;
    
    [Tooltip("Should the music start automatically when the scene loads?")]
    [SerializeField] private bool playOnAwake = true;
    
    [Tooltip("Should the music loop continuously?")]
    [SerializeField] private bool loop = true;
    
    [Tooltip("Fade in time in seconds (0 for no fade)")]
    [SerializeField] private float fadeInTime = 2.0f;
    
    [Header("Advanced Settings")]
    [Tooltip("Load music from file path if no clip is assigned")]
    [SerializeField] private bool loadFromFile = false;
    
    // The AudioSource component that will play our music
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Make this object persistent across scene loads if needed
        // DontDestroyOnLoad(gameObject);
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure the AudioSource
        ConfigureAudioSource();
        
        if (playOnAwake)
        {
            if (musicClip != null)
            {
                StartMusic();
            }
            else if (loadFromFile)
            {
                // Start loading music from file
                _ = LoadMusicFromFileAsync();
            }
            else
            {
                Debug.LogWarning("[BackgroundMusicPlayer] No music clip assigned and not loading from file.");
            }
        }
    }
    
    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = fadeInTime > 0 ? 0 : volume; // Start at 0 if fading
        audioSource.spatialBlend = 0; // Pure 2D sound
        audioSource.priority = 0; // Highest priority
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
    }
    
    private async Task LoadMusicFromFileAsync()
    {
        try
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, musicFilePath);
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.MPEG))
            {
                Debug.Log($"[BackgroundMusicPlayer] Loading music from: {fullPath}");
                
                // Send the request and await completion
                var asyncOp = www.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    await Task.Delay(100);
                }
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[BackgroundMusicPlayer] Failed to load music: {www.error}");
                    return;
                }
                
                musicClip = DownloadHandlerAudioClip.GetContent(www);
                
                Debug.Log($"[BackgroundMusicPlayer] Successfully loaded music. Length: {musicClip.length}s");
                
                // Start playing the loaded music
                StartMusic();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BackgroundMusicPlayer] Error loading music: {e.Message}");
        }
    }
    
    public void StartMusic()
    {
        if (audioSource == null || musicClip == null) return;
        
        audioSource.clip = musicClip;
        audioSource.Play();
        
        if (fadeInTime > 0)
        {
            StartCoroutine(FadeIn());
        }
        
        Debug.Log("[BackgroundMusicPlayer] Started playing background music");
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        float startTime = Time.time;
        float endTime = startTime + fadeInTime;
        
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / fadeInTime;
            audioSource.volume = Mathf.Lerp(0, volume, t);
            yield return null;
        }
        
        audioSource.volume = volume;
    }
    
    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    public void PauseMusic()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    // Editor-only method to help with setup
#if UNITY_EDITOR
    [ContextMenu("Setup Audio Source")]
    private void EditorSetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureAudioSource();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[BackgroundMusicPlayer] Audio source configured in editor");
    }
#endif
}