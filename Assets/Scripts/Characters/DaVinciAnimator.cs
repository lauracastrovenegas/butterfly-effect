using UnityEngine;
using System.Collections;

public class DaVinciAnimator : MonoBehaviour
{
    [Header("Animation Parameters")]
    [SerializeField] private string walkingParam = "IsWalking";
    [SerializeField] private string talkingParam = "IsTalking";
    [SerializeField] private string breakdancingParam = "IsBreakdancing";
    [SerializeField] private string rappingParam = "IsRapping";
    [SerializeField] private string backflippingParam = "IsBackflipping";
    
    [Header("Talking Animation Triggers")]
    [SerializeField] private string talking1TriggerParam = "Talking1Trigger";
    [SerializeField] private string talking2TriggerParam = "Talking2Trigger";
    [SerializeField] private string talking3TriggerParam = "Talking3Trigger";

    [Header("Animation Settings")]
    [SerializeField] private float specialMoveDuration = 15f;
    [SerializeField] private float walkingCheckInterval = 0.5f;
    
    private Animator animator;
    private Coroutine currentAnimationCoroutine;
    private Coroutine walkingCoroutine;
    private bool shouldBeWalking = false;

    private void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on GameObject!");
            enabled = false;
            return;
        }
        
        // Subscribe to animation events from ServiceManager
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger += HandleAnimationTrigger;
            Debug.Log("Successfully subscribed to animation events");
        }
        else
        {
            Debug.LogError("ServiceManager not found!");
        }
        
        // Start walking monitoring coroutine
        walkingCoroutine = StartCoroutine(MonitorWalkingState());
    }
    
    private IEnumerator MonitorWalkingState()
    {
        WaitForSeconds wait = new WaitForSeconds(walkingCheckInterval);
        
        while (true)
        {
            // If the current walking state doesn't match what it should be, force update
            if (animator.GetBool(walkingParam) != shouldBeWalking)
            {
                Debug.Log($"Forcing walking animation update - Setting to: {shouldBeWalking}");
                animator.SetBool(walkingParam, shouldBeWalking);
            }
            
            yield return wait;
        }
    }

    private void HandleAnimationTrigger(string marker)
    {
        if (animator == null) return;

        Debug.Log($"Animation trigger received: {marker}");

        // Stop any current animation coroutine
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        // Reset all special animations first
        ResetSpecialAnimations();

        // Set appropriate animation based on marker
        switch (marker)
        {
            case "MONA_LISA":
            case "PAINTING":
            case "VITRUVIAN":
            case "MEASURE":
            case "INVENTION":
            case "NORMAL":
                // Use standard talking animation
                animator.SetBool(talkingParam, true);
                
                // Force talking variation
                TriggerRandomTalkingAnimation();
                break;
                
            case "BREAKDANCE":
                // Use breakdancing animation
                animator.SetBool(breakdancingParam, true);
                currentAnimationCoroutine = StartCoroutine(ResetAfterDuration(breakdancingParam, specialMoveDuration));
                break;
                
            case "RAP":
                // Use rapping animation
                animator.SetBool(rappingParam, true);
                currentAnimationCoroutine = StartCoroutine(ResetAfterDuration(rappingParam, specialMoveDuration));
                break;
                
            case "BACKFLIP":
                // Trigger backflip animation
                animator.SetBool(backflippingParam, true);
                currentAnimationCoroutine = StartCoroutine(ResetAfterDuration(backflippingParam, specialMoveDuration));
                break;
                
            default:
                // Default to talking for any unrecognized marker
                animator.SetBool(talkingParam, true);
                TriggerRandomTalkingAnimation();
                break;
        }
    }

    private void TriggerRandomTalkingAnimation()
    {
        // Reset previous triggers
        animator.ResetTrigger(talking1TriggerParam);
        animator.ResetTrigger(talking2TriggerParam);
        animator.ResetTrigger(talking3TriggerParam);
        
        // Choose a random variation
        int variation = Random.Range(0, 3);
        
        // Set the trigger
        switch (variation)
        {
            case 0:
                animator.SetTrigger(talking1TriggerParam);
                Debug.Log("Set Talking1Trigger");
                break;
            case 1:
                animator.SetTrigger(talking2TriggerParam);
                Debug.Log("Set Talking2Trigger");
                break;
            case 2:
                animator.SetTrigger(talking3TriggerParam);
                Debug.Log("Set Talking3Trigger");
                break;
        }
    }

    private void ResetSpecialAnimations()
    {
        animator.SetBool(breakdancingParam, false);
        animator.SetBool(rappingParam, false);
        animator.SetBool(backflippingParam, false);
    }
    
    private IEnumerator ResetAfterDuration(string paramName, float duration)
    {
        yield return new WaitForSeconds(duration);
        animator.SetBool(paramName, false);
        
        // Go back to talking if needed
        animator.SetBool(talkingParam, true);
        TriggerRandomTalkingAnimation();
    }

    // Public method to set walking animation (called by LeonardoMovementController)
    public void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        
        // Store the desired state
        shouldBeWalking = isWalking;
        
        // Apply immediately and also rely on monitoring coroutine for consistency
        animator.SetBool(walkingParam, isWalking);
        Debug.Log($"Setting walking animation to {isWalking}");
        
        // If we start walking, stop special animations
        if (isWalking)
        {
            ResetSpecialAnimations();
        }
    }

    // Public method to stop talking animation
    public void StopTalking()
    {
        if (animator == null) return;
        animator.SetBool(talkingParam, false);
        Debug.Log("Stopped talking animation");
    }

    // This method can be called from the inspector to manually trigger a talking animation
    [ContextMenu("Trigger Random Talking Animation")]
    public void ManualTriggerTalking()
    {
        if (animator == null) return;
        animator.SetBool(talkingParam, true);
        TriggerRandomTalkingAnimation();
    }
    
    // This method can be called from the inspector to debug walking
    [ContextMenu("Toggle Walking State")]
    public void ToggleWalking()
    {
        SetWalking(!shouldBeWalking);
    }

    private void OnDestroy()
    {
        // Stop all coroutines
        if (walkingCoroutine != null)
        {
            StopCoroutine(walkingCoroutine);
        }
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        
        // Unsubscribe from events
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger -= HandleAnimationTrigger;
        }
    }
}