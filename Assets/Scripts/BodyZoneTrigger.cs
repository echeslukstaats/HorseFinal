using UnityEngine;

public class BodyZoneTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("Tick this only on the rump collider.")]
    public bool isRump = false;

    [Tooltip("Tick this only on the nape/neck collider (upper neck, just behind the head). Drives EmotionalState Happy gating.")]
    public bool isNeck = false;

    [Tooltip("Tick this only on the rear-approach LEFT collider (wide capture zone behind the rump, left side, parented under Pelvis). Fires an immediate, unconditional kick with the LEFT rear leg (BL) on entry — bypasses hasGreeted/RumpTouchIsTrusted/TouchIsSafe entirely, gated only by the existing kick cooldown. Do not also tick isRump, isNeck or isRearApproachRight.")]
    public bool isRearApproachLeft = false;

    [Tooltip("Tick this only on the rear-approach RIGHT collider (wide capture zone behind the rump, right side, parented under Pelvis). Fires an immediate, unconditional kick with the RIGHT rear leg (BR) on entry — bypasses hasGreeted/RumpTouchIsTrusted/TouchIsSafe entirely, gated only by the existing kick cooldown. Do not also tick isRump, isNeck or isRearApproachLeft.")]
    public bool isRearApproachRight = false;

    // Leg indices matching HorseFsm.FireKickTrigger: 1=FL, 2=FR, 3=BL, 4=BR.
    private const int REAR_LEFT_LEG = 3;
    private const int REAR_RIGHT_LEG = 4;

    private HorseFsm.BodyZone ResolveZone()
    {
        if (isRump) return HorseFsm.BodyZone.Rump;
        if (isNeck) return HorseFsm.BodyZone.Neck;
        if (isRearApproachLeft) return HorseFsm.BodyZone.RearApproachLeft;
        if (isRearApproachRight) return HorseFsm.BodyZone.RearApproachRight;
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
            Debug.Log($"[COLLIDER-HIT] {gameObject.name} → zone={zone} (isRump={isRump}, isNeck={isNeck}, isRearApproachLeft={isRearApproachLeft}, isRearApproachRight={isRearApproachRight}) | t={Time.time:F2}s");

        bool continuous = horseFsm.NotifyZoneEnter(zone);

        if (isRearApproachLeft)
        {
            // Immediate, unconditional kick with the LEFT rear leg — no
            // touchedBehind/body-touch bookkeeping, so this zone can never feed
            // RumpTouchIsTrusted/TouchIsSafe for the existing Rump/BehindTrigger
            // kick path.
            horseFsm.TriggerImmediateKick(REAR_LEFT_LEG);
        }
        else if (isRearApproachRight)
        {
            // Same as above, but forces the RIGHT rear leg.
            horseFsm.TriggerImmediateKick(REAR_RIGHT_LEG);
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

        if (isRearApproachLeft || isRearApproachRight)
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