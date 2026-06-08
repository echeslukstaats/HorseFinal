using UnityEngine;

public class BehindTrigger : MonoBehaviour 
{
    public HorseFsm horseFsm;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VRHand"))
        {
            horseFsm.SetTouchedBehind(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("VRHand"))
        {
            horseFsm.SetTouchedBehind(false);
        }
    }
}
