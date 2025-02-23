using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AudioManager : MonoBehaviour
{
    private Dictionary<Transform, AudioSource> audioSources = new Dictionary<Transform, AudioSource>();
    private Queue<AudioRequest> audioQueue = new Queue<AudioRequest>();
    private bool isProcessingQueue = false;

    private class AudioRequest
    {
        public AudioClip Clip { get; set; }
        public Transform Location { get; set; }
        public float Delay { get; set; }
    }

    private void Start()
    {
        // using Unity's built-in spatial audio
    }

    public void PlaySpatialAudio(AudioClip clip, Transform location, float delay = 0f)
    {
        if (clip == null || location == null) return;

        audioQueue.Enqueue(new AudioRequest 
        { 
            Clip = clip, 
            Location = location, 
            Delay = delay 
        });

        if (!isProcessingQueue)
        {
            _ = ProcessAudioQueue();
        }
    }

    private async Task ProcessAudioQueue()
    {
        isProcessingQueue = true;

        while (audioQueue.Count > 0)
        {
            var request = audioQueue.Dequeue();
            
            if (request.Delay > 0)
            {
                await Task.Delay((int)(request.Delay * 1000));
            }

            AudioSource source = GetOrCreateAudioSource(request.Location);
            PlayAudioOnSource(source, request.Clip);
            
            // Wait for clip to finish before processing next in queue
            await Task.Delay((int)(request.Clip.length * 1000));
        }

        isProcessingQueue = false;
    }

    private AudioSource GetOrCreateAudioSource(Transform location)
    {
        if (audioSources.TryGetValue(location, out AudioSource existingSource))
        {
            return existingSource;
        }

        AudioSource newSource = location.gameObject.AddComponent<AudioSource>();
        audioSources[location] = newSource;
        
        // Optimized audio settings
        newSource.spatialBlend = 0.0f;  // Pure 2D audio
        newSource.volume = 0.8f;
        newSource.playOnAwake = false;
        newSource.loop = false;
        newSource.spatialize = false;
        newSource.dopplerLevel = 0.0f;
        newSource.priority = 0; // Highest priority
        newSource.reverbZoneMix = 0f; // No reverb
        newSource.bypassEffects = true;
        newSource.bypassListenerEffects = true;
        newSource.bypassReverbZones = true;
        
        return newSource;
    }

    private void PlayAudioOnSource(AudioSource source, AudioClip clip)
    {
        if (clip == null || source == null) return;

        try
        {
            // Ensure clean playback
            source.Stop();
            source.clip = null;
            source.clip = clip;
            source.Play();

            Debug.Log($"Playing audio clip: Length={clip.length}s, Channels={clip.channels}, Frequency={clip.frequency}Hz");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error playing audio: {e.Message}");
        }
    }

    public void StopAudio(Transform location)
    {
        if (audioSources.TryGetValue(location, out AudioSource source))
        {
            source.Stop();
        }
    }

    public bool IsPlaying(Transform location)
    {
        return audioSources.TryGetValue(location, out AudioSource source) && source.isPlaying;
    }

    private void OnDestroy()
    {
        foreach (var source in audioSources.Values)
        {
            if (source != null)
            {
                Destroy(source);
            }
        }
        audioSources.Clear();
    }
}