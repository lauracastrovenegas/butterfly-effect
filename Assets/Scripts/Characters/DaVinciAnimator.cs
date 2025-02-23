using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DaVinciAnimator : MonoBehaviour
{
    private Animator animator;
    private ServiceManager serviceManager;

    private void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        serviceManager = ServiceManager.Instance;
        
        if (serviceManager != null)
        {
            // Subscribe to animation events
            serviceManager.OnAnimationTrigger += HandleAnimationTrigger;
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

        // Reset all triggers first
        animator.ResetTrigger("PaintingTrigger");
        animator.ResetTrigger("MeasuringTrigger");
        animator.ResetTrigger("InventingTrigger");
        animator.ResetTrigger("TalkingTrigger");

        // Set appropriate trigger based on marker
        switch (marker)
        {
            case "MONA_LISA":
            case "PAINTING":
                animator.SetTrigger("PaintingTrigger");
                break;
            
            case "VITRUVIAN":
            case "MEASURE":
                animator.SetTrigger("MeasuringTrigger");
                break;
            
            case "INVENTION":
                animator.SetTrigger("InventingTrigger");
                break;
            
            case "NORMAL":
                animator.SetTrigger("TalkingTrigger");
                break;

            default:
                Debug.Log($"Unknown animation marker: {marker}");
                break;
        }
    }

    private void OnDestroy()
    {
        if (serviceManager != null)
        {
            serviceManager.OnAnimationTrigger -= HandleAnimationTrigger;
        }
    }
}