using UnityEngine;

public class MouthTrigger : MonoBehaviour
{
       
    public HorseFsm horseFsm;

    private void OnTriggerEnter(Collider hand)
    {
        if (hand.CompareTag("VRHand"))
        {
            Debug.Log("hAND ENTERED");
            horseFsm.SetHandNearMouth(true);
        }
    }

    private void OnTriggerExit(Collider hand)
    {
        if (hand.CompareTag("VRHand"))
        {
            Debug.Log("hAND left");
            horseFsm.SetHandNearMouth(false);
        }
    }
}