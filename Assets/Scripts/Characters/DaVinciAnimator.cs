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

    [Header("Animation Settings")]
    [SerializeField] private float specialMoveDuration = 15f;
    
    private Animator animator;
    private Coroutine currentAnimationCoroutine;

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
                // Use standard talking animation - state machine will pick variation
                animator.SetBool(talkingParam, true);
                break;
                
            case "BREAKDANCE":
                // Use breakdancing animation
                animator.SetBool(breakdancingParam, true);
                StartCoroutine(ResetAfterDuration(breakdancingParam, specialMoveDuration));
                break;
                
            case "RAP":
                // Use rapping animation
                animator.SetBool(rappingParam, true);
                StartCoroutine(ResetAfterDuration(rappingParam, specialMoveDuration));
                break;
                
            case "BACKFLIP":
                // Trigger backflip animation
                animator.SetBool(backflippingParam, true);
                StartCoroutine(ResetAfterDuration(backflippingParam, specialMoveDuration));
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
    }

    // Public method to set walking animation (called by LeonardoMovementController)
    public void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        animator.SetBool(walkingParam, isWalking);
        
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
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ServiceManager.Instance != null)
        {
            ServiceManager.Instance.OnAnimationTrigger -= HandleAnimationTrigger;
        }
    }
}