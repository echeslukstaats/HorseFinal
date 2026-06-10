using UnityEngine;

public class HeadDownEBehaviour : StateMachineBehaviour
{
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        HeadTriggerScript trigger = animator.GetComponentInChildren<HeadTriggerScript>();
        if (trigger != null)
        {
            trigger.OnInteractionFinished();
        }
    }
}
