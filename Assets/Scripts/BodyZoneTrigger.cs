using UnityEngine;

public class BodyZoneTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("Tick this only on the rump collider.")]
    public bool isRump = false;

    [Tooltip("Tick this only on the nape/neck collider (upper neck, just behind the head). Drives EmotionalState Happy gating.")]
    public bool isNeck = false;

    [Tooltip("Tick this only on the rear-approach collider (wide capture zone behind the rump, parented under Pelvis). Fires an immediate, unconditional kick on entry — bypasses hasGreeted/RumpTouchIsTrusted/TouchIsSafe entirely, gated only by the existing kick cooldown. Do not also tick isRump or isNeck.")]
    public bool isRearApproach = false;

    private HorseFsm.BodyZone ResolveZone()
    {
        if (isRump) return HorseFsm.BodyZone.Rump;
        if (isNeck) return HorseFsm.BodyZone.Neck;
        if (isRearApproach) return HorseFsm.BodyZone.RearApproach;
        return HorseFsm.BodyZone.Body;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        var zone = ResolveZone();

        // [DEBUG] Identifies exactly which GameObject/collider fired, since several
        // colliders can all resolve to the same zone (e.g. Body). Remove once the
        // neck-race issue is confirmed fixed.
        if (Debug.isDebugBuild)
            Debug.Log($"[COLLIDER-HIT] {gameObject.name} → zone={zone} (isRump={isRump}, isNeck={isNeck}, isRearApproach={isRearApproach}) | t={Time.time:F2}s");

        bool continuous = horseFsm.NotifyZoneEnter(zone);

        if (isRearApproach)
        {
            // Immediate, unconditional kick — no touchedBehind/body-touch bookkeeping,
            // so this zone can never feed RumpTouchIsTrusted/TouchIsSafe for the
            // existing Rump/BehindTrigger kick path.
            horseFsm.TriggerImmediateKick();
        }
        else if (isRump)
        {
            horseFsm.SetTouchedBehind(true, continuous);
        }
        else
        {
            horseFsm.NotifyBodyTouch(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        if (isRearApproach)
        {
            // The kick is a one-shot event on entry, not a maintained state —
            // nothing to do here.
            return;
        }

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