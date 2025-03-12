using UnityEngine;

public class SimpleMusicPlayer : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.3f;
    public bool playOnStart = true;

    private AudioSource musicSource;
    
    private void Awake()
    {
        // Create dedicated audio source for music
        musicSource = gameObject.AddComponent<AudioSource>();
        Debug.Log("SimpleMusicPlayer: Created dedicated AudioSource for music");
    }

    private void Start()
    {
        if (musicClip == null)
        {
            Debug.LogError("SimpleMusicPlayer: No music clip assigned!");
            return;
        }

        // Configure the audio source
        musicSource.clip = musicClip;
        musicSource.volume = volume;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D audio
        
        Debug.Log($"SimpleMusicPlayer: Configured with clip '{musicClip.name}' at volume {volume}");
        
        // Auto-play if enabled
        if (playOnStart)
        {
            PlayMusic();
        }
    }
    
    public void PlayMusic()
    {
        if (musicSource != null && musicClip != null && !musicSource.isPlaying)
        {
            musicSource.Play();
            Debug.Log($"SimpleMusicPlayer: Started playing '{musicClip.name}'");
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("SimpleMusicPlayer: Stopped music");
        }
    }
    
    public void SetVolume(float newVolume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(newVolume);
            Debug.Log($"SimpleMusicPlayer: Volume set to {musicSource.volume}");
        }
    }
}