using UnityEngine;

public class BodyZoneTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("Tick this only on the rump collider.")]
    public bool isRump = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        horseFsm.NotifyZoneEnter(isRump ? HorseFsm.BodyZone.Rump : HorseFsm.BodyZone.Body);

        if (isRump)
            horseFsm.SetTouchedBehind(true);
        else
            horseFsm.NotifyBodyTouch(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        if (isRump)
            horseFsm.SetTouchedBehind(false);
        else
            horseFsm.NotifyBodyTouch(false);
    }
}