using UnityEngine;
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
        // No need to set spatializer plugin - we'll use Unity's built-in spatial audio
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
        
        // Configure spatial audio settings
        newSource.spatialBlend = 1.0f;
        newSource.spread = 60.0f;
        newSource.dopplerLevel = 0.0f;
        newSource.rolloffMode = AudioRolloffMode.Custom;
        newSource.maxDistance = 10.0f;
        newSource.minDistance = 1.0f;
        
        return newSource;
    }

    private void PlayAudioOnSource(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.Play();
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