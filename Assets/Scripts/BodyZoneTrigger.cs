using UnityEngine;

public class BodyZoneTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("Tick this only on the rump collider.")]
    public bool isRump = false;

    [Tooltip("Tick this only on the nape/neck collider (upper neck, just behind the head). Drives EmotionalState Happy gating.")]
    public bool isNeck = false;

    private HorseFsm.BodyZone ResolveZone()
    {
        if (isRump) return HorseFsm.BodyZone.Rump;
        if (isNeck) return HorseFsm.BodyZone.Neck;
        return HorseFsm.BodyZone.Body;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        bool continuous = horseFsm.NotifyZoneEnter(ResolveZone());

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

        horseFsm.RefreshZoneTime(ResolveZone());
    }
}