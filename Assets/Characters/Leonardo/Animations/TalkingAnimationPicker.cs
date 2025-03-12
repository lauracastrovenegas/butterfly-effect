using UnityEngine;
using System.Collections;

public class TalkingAnimationPicker : StateMachineBehaviour 
{
    // Duration to stay in this state before forcing a talking state
    public float timeoutDuration = 0.2f;
    
    private bool transitionTriggered = false;
    private float stateTime = 0f;
    
    // Called when entering the state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Reset state
        transitionTriggered = false;
        stateTime = 0f;
        
        // Clear any lingering triggers
        animator.ResetTrigger("Talking1Trigger");
        animator.ResetTrigger("Talking2Trigger");
        animator.ResetTrigger("Talking3Trigger");
        
        // Set a random trigger
        int talkingVariation = Random.Range(0, 3);
        switch (talkingVariation)
        {
            case 0:
                animator.SetTrigger("Talking1Trigger");
                Debug.Log("[TalkingAnimationPicker] Set Talking1Trigger - OnStateEnter");
                break;
            case 1:
                animator.SetTrigger("Talking2Trigger");
                Debug.Log("[TalkingAnimationPicker] Set Talking2Trigger - OnStateEnter");
                break;
            case 2:
                animator.SetTrigger("Talking3Trigger");
                Debug.Log("[TalkingAnimationPicker] Set Talking3Trigger - OnStateEnter");
                break;
        }
    }
    
    // Called on each update frame
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Track time in this state
        stateTime += Time.deltaTime;
        
        // If we've been in this state too long without transitioning, force it
        if (!transitionTriggered && stateTime >= timeoutDuration)
        {
            Debug.Log("[TalkingAnimationPicker] Timed out - forcing transition");
            ForceTransitionToTalkingState(animator);
            transitionTriggered = true;
        }
    }
    
    // Helper to force transition when needed
    private void ForceTransitionToTalkingState(Animator animator)
    {
        // Reset any existing triggers first
        animator.ResetTrigger("Talking1Trigger");
        animator.ResetTrigger("Talking2Trigger");
        animator.ResetTrigger("Talking3Trigger");
        
        // Choose a random talking state
        int talkingState = Random.Range(0, 3);
        
        switch (talkingState)
        {
            case 0:
                animator.SetTrigger("Talking1Trigger");
                Debug.Log("[TalkingAnimationPicker] Forced Talking1Trigger");
                break;
            case 1:
                animator.SetTrigger("Talking2Trigger");
                Debug.Log("[TalkingAnimationPicker] Forced Talking2Trigger");
                break;
            case 2:
                animator.SetTrigger("Talking3Trigger");
                Debug.Log("[TalkingAnimationPicker] Forced Talking3Trigger");
                break;
        }
    }
    
    // Called when exiting the state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Clean up any triggers when exiting
        animator.ResetTrigger("Talking1Trigger");
        animator.ResetTrigger("Talking2Trigger");
        animator.ResetTrigger("Talking3Trigger");
        
        Debug.Log("[TalkingAnimationPicker] Exited state");
    }
}