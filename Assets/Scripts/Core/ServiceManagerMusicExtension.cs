using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ServiceManagerMusicExtension : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip workshopMusic;
    [SerializeField] private float musicVolume = 0.3f;
    [SerializeField] private bool playMusicOnStart = true;
    
    private AudioSource audioSource;
    
    private void Start()
    {
        // Get or add audio source component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source
        audioSource.clip = workshopMusic;
        audioSource.volume = musicVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        Debug.Log($"Music Extension initialized with clip: {(workshopMusic ? workshopMusic.name : "none")}");
        
        // Play music if enabled
        if (playMusicOnStart && workshopMusic != null)
        {
            audioSource.Play();
            Debug.Log("Started playing workshop music");
        }
    }
    
    // Helper methods for controlling music
    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("Music playback started");
        }
    }
    
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log("Music playback paused");
        }
    }
    
    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Music playback stopped");
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            Debug.Log($"Music volume set to {volume}");
        }
    }
    
    public void ChangeMusic(AudioClip newMusic)
    {
        if (audioSource != null && newMusic != null)
        {
            bool wasPlaying = audioSource.isPlaying;
            audioSource.Stop();
            audioSource.clip = newMusic;
            
            if (wasPlaying)
            {
                audioSource.Play();
            }
            
            Debug.Log($"Changed music to {newMusic.name}");
        }
    }
}