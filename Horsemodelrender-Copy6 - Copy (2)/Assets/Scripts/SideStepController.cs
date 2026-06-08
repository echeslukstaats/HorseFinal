using UnityEngine;

public class SideStepController : MonoBehaviour
{

    public Animator animator;
    public HorseFsm horsefsm;

    private void Update()
    {

        AnimatorStateInfo animatorInfo = animator.GetCurrentAnimatorStateInfo(2);
        AnimatorStateInfo animatorInfo2 = animator.GetCurrentAnimatorStateInfo(5);

        if (animatorInfo.normalizedTime >= 1f && animator.GetInteger("SideStepDone") != 3 &&horsefsm.detectSideTouched ==1 )
        {
            horsefsm.SetSideStepDone(true);
            animator.SetInteger("SideStepDone", 1);
        }else if  (animatorInfo2.normalizedTime >= 1f && animator.GetInteger("SideStepDone") != 3 && horsefsm.detectSideTouched == 2)
            {
                horsefsm.SetSideStepDone(true);
                animator.SetInteger("SideStepDone", 1);
            }
    }
}
