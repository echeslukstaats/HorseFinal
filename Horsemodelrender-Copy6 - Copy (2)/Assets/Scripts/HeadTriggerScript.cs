using UnityEngine;

public class HeadTriggerScript : MonoBehaviour
{
    public HorseFsm horsefsm;
    public Animator animator;

    public float requiredHoldTime = 1.0f;

    private float holdTimer = 0f;
    private bool isHandInside = false;
    private bool hasActivated = false;
    private bool isCommitted = false;

    //private float exitGraceTimer = 0f;
    public float exitGraceTime = 0.2f;
    private void OnTriggerEnter(Collider hand)
    {
        if (hand.CompareTag("VRHand"))
        {
            isHandInside = true;
            //exitGraceTime = 0f;

            if (!hasActivated)
            {
                holdTimer = 0f;
            }
        }
    }
    private void OnTriggerExit(Collider hand)
    {
        if (hand.CompareTag("VRHand"))
        {
            isHandInside = false;
        }
    }

    private void Update()
    {
        if (!isHandInside)
        {
           // Debug.Log("hand out head");
            //exitGraceTime += Time.deltaTime;

                holdTimer = 0f;
           // Debug.Log("activated " + hasActivated);
           // Debug.Log("commited " + isCommitted);
            if (hasActivated && !isCommitted)
                {
                    //Debug.Log("there yet");
                    horsefsm.SetTouchedHead(false);
                    hasActivated = false;
                }
            return;
        }

       // exitGraceTimer = 0f;

        if (!hasActivated)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= requiredHoldTime)
            {
                Activate();
            }
        }
    }

    private void Activate()
    {
        hasActivated = true;
        isCommitted = true;
        horsefsm.SetTouchedHead(true);
    }

    public void OnInteractionFinished()
    {
        //Debug.Log("Head interaction finished");

        isCommitted = false;
        hasActivated = false;
        horsefsm.SetTouchedHead(false);
        animator.SetBool("HeadDownActive", false);
        //if (animator.GetLayerWeight(5) > 0f)
        //{
        //    animator.SetLayerWeight(5, 0f);
        //}

    }
}
