using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WormholeEffect : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private string destinationScene = "DaVinciWorkshop";
    [SerializeField] private float totalTravelDuration = 8.0f;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem mainParticles;
    [SerializeField] private ParticleSystem secondaryParticles; // Optional
    
    [Header("Audio")]
    [SerializeField] private AudioSource wormholeAudioSource;
    [SerializeField] private AudioClip wormholeSound;
    [SerializeField] private float audioVolume = 1.0f;
    
    [Header("Controller Feedback")]
    [SerializeField] private float maxShakeIntensity = 0.02f;
    
    // Private variables
    private Vector3 originalCameraPosition;
    
    void Start()
    {
        // Store original camera position
        originalCameraPosition = Camera.main.transform.localPosition;
        
        // Start the time travel sequence
        StartCoroutine(WormholeSequence());
    }
    
    private IEnumerator WormholeSequence()
    {
        // Start audio
        if (wormholeAudioSource != null && wormholeSound != null)
        {
            wormholeAudioSource.clip = wormholeSound;
            wormholeAudioSource.loop = true;
            wormholeAudioSource.volume = audioVolume;
            wormholeAudioSource.Play();
        }
        
        // Main travel phase
        yield return StartCoroutine(TravelPhase());
        
        // Load destination
        SceneManager.LoadScene(destinationScene);
    }
    
    private IEnumerator TravelPhase()
    {
        float startTime = Time.time;
        
        // Start particle systems
        if (mainParticles != null)
            mainParticles.Play();
            
        if (secondaryParticles != null)
            secondaryParticles.Play();
        
        // Travel effect progression
        while (Time.time < startTime + totalTravelDuration)
        {
            float progress = (Time.time - startTime) / totalTravelDuration;
            
            // Update main particles to increase speed over time
            if (mainParticles != null)
            {
                var main = mainParticles.main;
                
                // Speed starts low and increases, then decreases at the end
                if (progress < 0.7f)
                {
                    // Accelerate
                    main.startSpeedMultiplier = Mathf.Lerp(1f, 15f, progress / 0.7f);
                }
                else
                {
                    // Decelerate for the final approach
                    float endProgress = (progress - 0.7f) / 0.3f; // 0 to 1 in the last 30% of time
                    main.startSpeedMultiplier = Mathf.Lerp(15f, 2f, endProgress);
                }
                
                // Adjust emission rate for more intensity in the middle
                var emission = mainParticles.emission;
                float intensityCurve = Mathf.Sin(progress * Mathf.PI); // Peaks in the middle
                emission.rateOverTimeMultiplier = Mathf.Lerp(500f, 1000f, intensityCurve);
            }
            
            // Update secondary particles (if available)
            if (secondaryParticles != null)
            {
                var main = secondaryParticles.main;
                
                // Different speed pattern for variety
                if (progress < 0.6f)
                {
                    main.startSpeedMultiplier = Mathf.Lerp(5f, 20f, progress / 0.6f);
                }
                else
                {
                    float endProgress = (progress - 0.6f) / 0.4f;
                    main.startSpeedMultiplier = Mathf.Lerp(20f, 3f, endProgress);
                }
            }
            
            // Apply controller haptic feedback for Quest 2
            if (progress > 0.2f && Time.frameCount % 30 == 0)
            {
                // Simple haptic pulse - intensity increases then decreases
                float hapticIntensity = 0.3f;
                if (progress < 0.7f)
                {
                    hapticIntensity = Mathf.Lerp(0.1f, 0.5f, progress / 0.7f);
                }
                else
                {
                    float endProgress = (progress - 0.7f) / 0.3f;
                    hapticIntensity = Mathf.Lerp(0.5f, 0.1f, endProgress);
                }
                
                OVRInput.SetControllerVibration(1.0f, hapticIntensity, OVRInput.Controller.RTouch);
                OVRInput.SetControllerVibration(1.0f, hapticIntensity, OVRInput.Controller.LTouch);
            }
            
            // Subtle camera shake
            if (maxShakeIntensity > 0)
            {
                // Shake intensity follows a bell curve - more in the middle, less at start/end
                float shakeAmount = maxShakeIntensity * Mathf.Sin(progress * Mathf.PI);
                float shakeX = Mathf.PerlinNoise(Time.time * 10f, 0) * 2 - 1;
                float shakeY = Mathf.PerlinNoise(0, Time.time * 10f) * 2 - 1;
                
                Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0) * shakeAmount;
                Camera.main.transform.localPosition = originalCameraPosition + shakeOffset;
            }
            
            yield return null;
        }
        
        // Reset camera position
        Camera.main.transform.localPosition = originalCameraPosition;
        
        // Stop particle systems
        if (mainParticles != null)
            mainParticles.Stop();
            
        if (secondaryParticles != null)
            secondaryParticles.Stop();
            
        // Stop audio with a fade out
        if (wormholeAudioSource != null)
        {
            float fadeTime = 1.0f;
            float startVolume = wormholeAudioSource.volume;
            
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                wormholeAudioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            wormholeAudioSource.Stop();
        }
    }
}