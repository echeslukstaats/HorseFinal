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

        var zone = ResolveZone();

        // [DEBUG] Identifies exactly which GameObject/collider fired, since several
        // colliders can all resolve to the same zone (e.g. Body). Remove once the
        // neck-race issue is confirmed fixed.
        Debug.Log($"[COLLIDER-HIT] {gameObject.name} → zone={zone} (isRump={isRump}, isNeck={isNeck}) | t={Time.time:F2}s");

        bool continuous = horseFsm.NotifyZoneEnter(zone);

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