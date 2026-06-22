using UnityEngine;

public class BehindTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VRHand")) NotifyTouched();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("VRHand")) horseFsm.SetTouchedBehind(false);
    }

    public void NotifyTouched() => horseFsm.SetTouchedBehind(true);

    public void OnTouched() => NotifyTouched();
}