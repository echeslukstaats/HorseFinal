using UnityEngine;

public class BodyZoneTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("Tick this only on the rump collider.")]
    public bool isRump = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        bool continuous = horseFsm.NotifyZoneEnter(isRump ? HorseFsm.BodyZone.Rump : HorseFsm.BodyZone.Body);

        if (isRump)
            horseFsm.SetTouchedBehind(true, continuous);
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

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        horseFsm.RefreshZoneTime(isRump ? HorseFsm.BodyZone.Rump : HorseFsm.BodyZone.Body);
    }
}