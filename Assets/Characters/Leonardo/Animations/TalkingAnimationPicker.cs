using UnityEngine;

public class TalkingAnimationPicker : StateMachineBehaviour
{
    // Each animation has equal chance (33%)
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Generate random number between 0-2
        int talkingVariation = Random.Range(0, 3);
        
        // Choose destination state based on random number
        switch (talkingVariation)
        {
            case 0:
                animator.SetTrigger("Talking1Trigger");
                break;
            case 1:
                animator.SetTrigger("Talking2Trigger");
                break;
            case 2:
                animator.SetTrigger("Talking3Trigger");
                break;
        }
    }
}