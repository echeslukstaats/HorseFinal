using UnityEngine;

public class HorseIdleController : MonoBehaviour
{
    public Animator animator;
    private int idle1Repeats = 3;

    private int idle1Count = 0;
    private bool isIdle1 = true;

    private void Start()
    {
        PlayIdle1();
    }

    private void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("idle1_Baked") && !stateInfo.IsName("idle2_Baked"))
            return;

        if (stateInfo.normalizedTime >= 1f)
        {
            if (isIdle1)
            {
                idle1Count++;
                if (idle1Count < idle1Repeats)
                {
                    PlayIdle1();
                }
                else
                {
                    PlayIdle2();
                    isIdle1 = false;
                    idle1Count = 0;
                }
            }
            else
            {
                PlayIdle1();
                isIdle1 = true;
            }
        }
    }

    private void PlayIdle1()
    {
        animator.Play("idle1_Baked", 0, 0f);
        Debug.Log("Playing Idle1");
    }

    private void PlayIdle2()
    {
        animator.Play("idle2_Baked", 0, 0f);
        Debug.Log("Playing Idle2");
    }
}