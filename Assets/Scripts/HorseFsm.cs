using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum HorseStates
{
    None,
    Feeding,
    Anxious,
    Walking,
}

public class HorseFsm : MonoBehaviour
{
    public Animator animator;

    public HorseStates currState { get; private set; } = HorseStates.None;
    [HideInInspector]
    public bool handNearMouth { get; private set; } = false;
    public bool handBehindEar { get; private set; } = false;
    public bool touchedBehind { get; private set; } = false;
    public int detectSideTouched { get; private set; } = 0;
    public bool sideStepDone { get; private set; } = true;
    public bool startHorseWalk { get; private set; } = false;
    public bool touchedHead { get; private set; } = false;
    public bool hasKicked = false;

    public ConfigurableJoint rightEar;
    public ConfigurableJoint leftEar;
    private Vector3 rEarRotation = new Vector3(340f, 354f, 290f);
    private Vector3 lEarRotation = new Vector3(340f, 354f, 70f);

    private float startFeedingTimer = 0;
    private float exitFeedingTimer = 0;
    private float startAnxiousTimer = 0;

    public ChangeLegRigWeight[] legRigWeights = new ChangeLegRigWeight[4];
    private ChangeLegRigWeight GetLegRig(int legIndex)
    {
        if (legIndex < 1 || legIndex > 4) return null;
        return legRigWeights[legIndex - 1];
    }

    private void SetAllLegRigWeights(float value)
    {
        foreach (var rig in legRigWeights)
        {
            if (rig != null) rig.ChangeWeight(value);
        }
    }
    private float twitchAmount = 120f;
    private float twitchSpeed = 1f;

    private float HorseEmotion = 0f;
    public bool hasGreeted = false;
    private bool kickStarted = false;
    private bool kickLocked = false;

    // ── Per-leg kick targeting ─────────────────────────────────────────────
    public int lastKickedLeg { get; private set; } = 0;
    private const int DEFAULT_KICK_LEG = 4; // Back Right — fallback if no leg/hoof touch is on record

    // Thigh-level touch (no hoof contact yet). Used as a secondary signal for
    // DetermineKickLeg() when hoofTouched is 0. Expires after LEG_TOUCH_TIMEOUT
    // so an old touch can't silently dictate a kick minutes later.
    private int legTouched = 0; // 0=none 1=FL 2=FR 3=BL 4=BR
    private float legTouchTimer = 0f;
    private const float LEG_TOUCH_TIMEOUT = 3f;
    private float hoofTouchTimer = 0f;
    private const float HOOF_TOUCH_TIMEOUT = 3f;

    // ── Body-touch ref-count  ──────────────────────────────────────
    // Handles overlapping colliders cleanly; handOnBody is true as long as
    // at least one body collider contains the hand.
    private int bodyTouchCount = 0;
    public bool handOnBody => bodyTouchCount > 0;
    public float lastBodyTouchTime = -999f;
    private const float BODY_TOUCH_GRACE = 1f;

    private bool RumpTouchIsTrusted
    {
        get
        {
            float elapsed = Time.time - lastBodyTouchTime;
            Debug.Log($"RumpTouchIsTrusted — hasGreeted:{hasGreeted} handOnBody:{handOnBody} elapsed:{elapsed:F2}s grace:{BODY_TOUCH_GRACE}s");
            return hasGreeted && (handOnBody || elapsed <= BODY_TOUCH_GRACE);
        }
    }

    // ── Flinch system ────────────────────────────────────────────────
    private bool flinchStarted = false;
    private bool flinchPlaying = false;
    private int hoofTouched = 0;     // 0=none 1=FL 2=FR 3=BL 4=BR
    private bool hoofWasTouched = false;
    private bool hoofTouchWasContinuous = false;

    // ── Continuous pet kick system ────────────────────────────────────────────────
    private bool touchedBehindWasContinuous = false;
    private bool legTouchWasContinuous = false;

    private bool TouchIsSafe =>
        (touchedBehind && touchedBehindWasContinuous) ||
        (legTouched != 0 && legTouchWasContinuous);

    // ── Body zone tracking ─────────────────────────────────────────
    public enum BodyZone
    {
        None, Head, Neck, Body, Rump,
        LegFL, HoofFL, LegFR, HoofFR, LegBL, HoofBL, LegBR, HoofBR,
        RearApproachLeft, RearApproachRight
    }

    private BodyZone lastZoneTouched = BodyZone.None;
    private float lastZoneTouchTime = -999f;
    private const float ZONE_CONTINUITY_WINDOW = 1.5f;

    private static readonly Dictionary<BodyZone, BodyZone[]> ZoneAdjacency = new Dictionary<BodyZone, BodyZone[]>
    {
        { BodyZone.Head,   new[] { BodyZone.Body, BodyZone.Neck } },
        { BodyZone.Neck,   new[] { BodyZone.Head, BodyZone.Body } },
        { BodyZone.Body,   new[] { BodyZone.Head, BodyZone.Neck, BodyZone.Rump, BodyZone.LegFL, BodyZone.LegFR, BodyZone.LegBL, BodyZone.LegBR } },
        { BodyZone.Rump,   new[] { BodyZone.Body, BodyZone.LegBL, BodyZone.LegBR } },
        { BodyZone.LegFL,  new[] { BodyZone.Body, BodyZone.HoofFL } },
        { BodyZone.HoofFL, new[] { BodyZone.LegFL } },
        { BodyZone.LegFR,  new[] { BodyZone.Body, BodyZone.HoofFR } },
        { BodyZone.HoofFR, new[] { BodyZone.LegFR } },
        { BodyZone.LegBL,  new[] { BodyZone.Rump, BodyZone.Body, BodyZone.HoofBL } },
        { BodyZone.HoofBL, new[] { BodyZone.LegBL } },
        { BodyZone.LegBR,  new[] { BodyZone.Rump, BodyZone.Body, BodyZone.HoofBR } },
        { BodyZone.HoofBR, new[] { BodyZone.LegBR } },
        { BodyZone.RearApproachLeft,  new[] { BodyZone.Rump, BodyZone.Body } },
        { BodyZone.RearApproachRight, new[] { BodyZone.Rump, BodyZone.Body } },
    };

    public enum EmotionalState { Neutral, Happy, Anxious }
    public EmotionalState emotionalState { get; private set; } = EmotionalState.Neutral;

    public enum InteractionMode { Static, Dynamic }

    [Header("Interaction Mode")]
    public InteractionMode interactionMode = InteractionMode.Static;

    // Dynamic-only gate for free locomotion (walking) and the anxious-triggered
    // sidestep recoil. Static mode keeps the horse fixed in place: petting,
    // touch responses, and the kick/flinch safety reactions stay active in both
    // modes, but the horse itself never leaves its spot.
    public bool MovementAllowed => interactionMode == InteractionMode.Dynamic;
    public void SetInteractionMode(InteractionMode newMode)
    {
        if (newMode == interactionMode) return;

        Debug.Log($"[LEG-LIFT] Interaction mode changed {interactionMode} → {newMode}, resetting leg-lift petting gate.");
        interactionMode = newMode;
        ResetLegLiftGate();

        if (interactionMode == InteractionMode.Static)
        {
            StopAllReactionsImmediate();
        }
    }

    // Forces the horse out of any in-progress reaction when Static mode starts.
    private void StopAllReactionsImmediate()
    {
        bool wasWalking = currState == HorseStates.Walking;
        bool wasFeeding = currState == HorseStates.Feeding;
        bool wasAnxious = currState == HorseStates.Anxious;
        bool wasSideStepping = animator.GetLayerWeight(2) > 0f || animator.GetLayerWeight(5) > 0f;
        bool wasKicking = animator.GetLayerWeight(4) > 0f || kickStarted;

        if (!wasWalking && !wasFeeding && !wasAnxious && !wasSideStepping && !wasKicking) return;

        Debug.Log($"[MODE] Static mode engaged mid-reaction (walking={wasWalking}, feeding={wasFeeding}, anxious={wasAnxious}, sideStepping={wasSideStepping}, kicking={wasKicking}) — stopping immediately.");

        startHorseWalk = false;
        animator.SetLayerWeight(3, 0);
        animator.SetLayerWeight(1, 0);

        animator.SetLayerWeight(2, 0);
        animator.SetLayerWeight(5, 0);
        animator.SetInteger("SideStepDone", 3);
        SetSideStepDone(true);
        SetSideTouched(0);

        animator.SetLayerWeight(4, 0);
        SetAllLegRigWeights(1f);
        animator.ResetTrigger("KickFrontLeft");
        animator.ResetTrigger("KickFrontRight");
        animator.ResetTrigger("KickBackLeft");
        animator.ResetTrigger("KickBackRight");
        kickStarted = false;
        kickLocked = false;
        lastKickedLeg = 0;

        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 0f);

        if (wasWalking || wasFeeding || wasAnxious || wasSideStepping || wasKicking)
        {
            SwitchState(HorseStates.None);
            animator.SetInteger("BehaviourStates", (int)currState);
        }
    }

    // ── Leg-lift petting gate (Dynamic mode only) ──────────────────────────
    // Index 1=FL, 2=FR, 3=BL, 4=BR (index 0 unused, kept for parity with legIndex convention).
    private bool[] legLiftGate = new bool[5];

    public bool CanLiftLeg(int legIndex)
    {
        if (legIndex < 1 || legIndex > 4) return false;

        // Static mode: always allowed, no petting prerequisite.
        if (interactionMode == InteractionMode.Static) return true;

        // Dynamic mode: only allowed once this specific leg's gate was unlocked.
        return legLiftGate[legIndex];
    }

    // Called by a leg-specific petting zone once HorsePettingScript confirms
    // continuous gentle petting near that leg.
    public void ConfirmLegPetting(int legIndex)
    {
        if (legIndex < 1 || legIndex > 4) return;
        //if (emotionalState == EmotionalState.Anxious) return; // don't unlock while spooked
        if (legLiftGate[legIndex]) return; // already unlocked, avoid log spam

        legLiftGate[legIndex] = true;
        Debug.Log($"[LEG-LIFT] Continuous petting confirmed for leg {legIndex} — leg-lift gate unlocked.");
    }

    public void ResetLegLiftGate()
    {
        Debug.Log("[LEG-LIFT] Petting gate reset for all legs.");
        for (int i = 0; i < legLiftGate.Length; i++)
            legLiftGate[i] = false;
    }

    private string firstTouchZone = null;
    private const int EARS_LAYER = 7;

    // ── Track player ─────────────────────────────────────────
    [Header("Player Tracking")]
    [Tooltip("Transform representing the horse's center of gravity (e.g., the Hips bone, or an empty point placed at the center of the body).")]
    public Transform centerOfGravity;

    public void RefreshZoneTime(BodyZone zone)
    {
        if (zone == lastZoneTouched)
            lastZoneTouchTime = Time.time;

        // Keep touch timers alive while the hand remains inside the trigger.
        switch (zone)
        {
            case BodyZone.LegFL:
            case BodyZone.LegFR:
            case BodyZone.LegBL:
            case BodyZone.LegBR:
                if (legTouched != 0) legTouchTimer = 0f;
                break;
            case BodyZone.HoofFL:
            case BodyZone.HoofFR:
            case BodyZone.HoofBL:
            case BodyZone.HoofBR:
                if (hoofTouched != 0) hoofTouchTimer = 0f;
                break;
        }
    }

    // Returns true if the new touch is considered a "continuous" interaction with the same body part
    public bool NotifyZoneEnter(BodyZone zone)
    {
        float elapsed = Time.time - lastZoneTouchTime;

        bool sameZone = zone == lastZoneTouched && elapsed <= ZONE_CONTINUITY_WINDOW;

        bool continuous = sameZone
            || (lastZoneTouched != BodyZone.None
            && ZoneAdjacency.TryGetValue(zone, out var neighbours)
            && System.Array.IndexOf(neighbours, lastZoneTouched) >= 0
            && elapsed <= ZONE_CONTINUITY_WINDOW);

        Debug.Log($"[ZONE] Enter={zone} | Previous={lastZoneTouched} | Elapsed={elapsed:F2}s (max {ZONE_CONTINUITY_WINDOW}s) | SameZone={sameZone} | Continuous={continuous}");

        // ── Emotional gating  ─────────────
        UpdateEmotionalState(zone, continuous);

        lastZoneTouched = zone;
        lastZoneTouchTime = Time.time;

        return continuous;
    }

    private void UpdateEmotionalState(BodyZone zone, bool continuous)
    {
        if (!MovementAllowed) return;

        if (continuous)
            return;

        bool isFirstTouch = lastZoneTouched == BodyZone.None;
        EmotionalState previousState = emotionalState;

        if (isFirstTouch)
        {
            firstTouchZone = zone.ToString();
            emotionalState = (zone == BodyZone.Neck) ? EmotionalState.Happy : EmotionalState.Anxious;
            Debug.Log($"[EMOTION-STATE] {emotionalState} triggered from {firstTouchZone} | t={Time.time:F2}s | first touch");
        }
        else
        {
            emotionalState = EmotionalState.Anxious;
            Debug.Log($"[EMOTION-STATE] {emotionalState} triggered (continuity broken) | firstTouchZone={firstTouchZone} | t={Time.time:F2}s");
        }

        // Petting gate resets whenever we newly enter Anxious from Happy/Neutral.
        if (emotionalState == EmotionalState.Anxious && previousState != EmotionalState.Anxious)
            ResetLegLiftGate();

        if (emotionalState == EmotionalState.Anxious) TriggerEarsAnxious();
        else TriggerEarsNeutral();
    }

    // HappyEars was removed from the emotional state logic: it never matched
    // actual game behavior (ears stayed neutral instead of transitioning to
    // happy). Only AnxiousEars remains; the Happy emotional state now simply
    // leaves the ears in their neutral state.
    public void TriggerEarsNeutral()
    {
        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 0f);
        Debug.Log($"[EMOTION-STATE] TriggerEarsNeutral() — isAnxious=false layer{EARS_LAYER}.weight=0 | t={Time.time:F2}s");
    }

    public void TriggerEarsAnxious()
    {
        animator.SetBool("isAnxious", true);
        animator.SetLayerWeight(EARS_LAYER, 1f);
        Debug.Log($"[EMOTION-STATE] TriggerEarsAnxious() — isAnxious=true layer{EARS_LAYER}.weight=1 | t={Time.time:F2}s");
    }

    private void ResetEmotionalStateToNeutral()
    {
        if (emotionalState == EmotionalState.Neutral) return; // avoid log spam every frame

        emotionalState = EmotionalState.Neutral;
        firstTouchZone = null;
        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 0f);
        Debug.Log($"[EMOTION-STATE] Neutral (idle, no active contact) | t={Time.time:F2}s");
    }

    private void Start()
    {
        animator.SetInteger("BehaviourStates", (int)currState);
    }

    private void Update()
    {
        if (legTouched != 0)
        {
            legTouchTimer += Time.deltaTime;
            if (legTouchTimer >= LEG_TOUCH_TIMEOUT)
            {
                legTouched = 0;
                legTouchTimer = 0f;
            }
        }

        if (hoofTouched != 0)
        {
            hoofTouchTimer += Time.deltaTime;
            if (hoofTouchTimer >= HOOF_TOUCH_TIMEOUT)
            {
                hoofTouched = 0;
                hoofTouchTimer = 0f;
            }
        }

        bool hoofIsTouchedNow = hoofTouched != 0;

        if (hoofIsTouchedNow && !hoofWasTouched)
        {
            if (!hoofTouchWasContinuous && !flinchStarted && MovementAllowed)
            {
                animator.SetInteger("FlinchState", hoofTouched);
                animator.SetLayerWeight(6, 1);
                flinchStarted = true;
                flinchPlaying = false;
            }
        }

        hoofWasTouched = hoofIsTouchedNow;

        if (flinchStarted)
        {
            if (!animator.GetCurrentAnimatorStateInfo(6).IsName("Idle"))
            {
                flinchPlaying = true;
            }
            else if (flinchPlaying)
            {
                animator.SetLayerWeight(6, 0);
                animator.SetInteger("FlinchState", 0);
                flinchStarted = false;
                flinchPlaying = false;
            }
        }

        if (lastZoneTouched != BodyZone.None
            && Time.time - lastZoneTouchTime > ZONE_CONTINUITY_WINDOW)
        {
            ResetEmotionalStateToNeutral();
            lastZoneTouched = BodyZone.None;
        }

        switch (currState)
        {
            case HorseStates.None:

                // Ne pilote les oreilles par physique que si la couche Ears Animator
                // (layer 7) n'est pas active (isAnxious=false), pour eviter un conflit.
                // Happy est traite comme Neutral pour les oreilles: HappyEars a ete
                // retire, donc l'etat Happy laisse les oreilles en position neutre.
                if (emotionalState == EmotionalState.Neutral || emotionalState == EmotionalState.Happy)
                    ChangeEarRotation(Quaternion.identity, Quaternion.identity);

                if (handNearMouth && MovementAllowed)
                {
                    startFeedingTimer += Time.deltaTime;
                    if (startFeedingTimer >= 1f)
                    {
                        SwitchState(HorseStates.Feeding);
                        animator.SetInteger("BehaviourStates", (int)currState);
                        animator.SetLayerWeight(1, 1);
                        hasGreeted = true;
                        StartCoroutine(ResetHasGreeted(60f));
                    }
                }
                else
                {
                    startFeedingTimer = 0;
                }

                // ── Anxious trigger (collègue : RumpTouchIsTrusted) ──────────
                if ((touchedBehind || legTouched != 0) && !RumpTouchIsTrusted && !TouchIsSafe && MovementAllowed)
                {
                    HorseEmotion = -3f;
                    int kickLeg = DetermineKickLeg();
                    GetLegRig(kickLeg)?.ChangeWeight(0f); 
                    SwitchState(HorseStates.Anxious);
                    animator.SetInteger("BehaviourStates", (int)currState);

                    if (handBehindEar && !hasGreeted && MovementAllowed)
                    {
                        animator.SetInteger("SideStepDone", 0);
                        SetSideStepDone(false);

                        if (detectSideTouched == 1)
                        {
                            animator.Play("sideStepR_baked1", 2, 0f);
                            animator.SetLayerWeight(2, 1);
                        }
                        else
                        {
                            animator.Play("SideStepL_baked", 5, 0f);
                            animator.SetLayerWeight(5, 1);
                        }
                    }
                }

                if (startHorseWalk && MovementAllowed)
                {
                    SwitchState(HorseStates.Walking);
                    animator.SetInteger("BehaviourStates", (int)currState);
                    animator.SetLayerWeight(3, 1);
                }

                break;

            case HorseStates.Feeding:
                if (!handNearMouth) exitFeedingTimer += Time.deltaTime;
                else exitFeedingTimer = 0;

                if (exitFeedingTimer >= 0.5f)
                {
                    SwitchState(HorseStates.None);
                    animator.SetInteger("BehaviourStates", (int)currState);
                    animator.SetLayerWeight(1, 0);
                }
                break;

            case HorseStates.Anxious:

                if (emotionalState == EmotionalState.Neutral || emotionalState == EmotionalState.Happy)
                    ChangeEarRotation(Quaternion.Euler(rEarRotation), Quaternion.Euler(lEarRotation));

                AnimatorStateInfo animatorInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (sideStepDone && animatorInfo.IsName("idle1_Baked") && animator.GetInteger("SideStepDone") == 1)
                {
                    animator.SetInteger("SideStepDone", 3);

                    if (detectSideTouched == 1)
                        StartCoroutine(FinishAnim(2, animator.GetCurrentAnimatorStateInfo(2).length));
                    else
                        StartCoroutine(FinishAnim(5, animator.GetCurrentAnimatorStateInfo(5).length));

                    startAnxiousTimer += Time.deltaTime;
                    SetSideStepDone(false);
                    SetSideTouched(0);
                }

                startAnxiousTimer += Time.deltaTime;

                // ── Kick finish/reset — runs every frame regardless of what started
                // the kick (Rump/leg touch OR RearApproachLeft/Right), since the
                // latter never sets touchedBehind/legTouched and must still get its
                // layer-4 weight and leg rig weight cleaned up on completion. ──────
                if (kickStarted)
                {
                    var kickInfo = animator.GetCurrentAnimatorStateInfo(4);
                    if (kickInfo.normalizedTime >= 1f)
                    {
                        Debug.Log("[KICK] Kick finished, resetting layer 4 weight to 0");
                        animator.SetLayerWeight(4, 0);
                        GetLegRig(lastKickedLeg)?.ChangeWeight(1f); 
                        kickStarted = false;
                        lastKickedLeg = 0;
                    }
                }

                if ((touchedBehind || legTouched != 0) && MovementAllowed)
                {
                    Debug.Log($"[KICK-GATE] touchedBehind={touchedBehind} (continuous={touchedBehindWasContinuous}) | legTouched={legTouched} (continuous={legTouchWasContinuous}) | RumpTouchIsTrusted={RumpTouchIsTrusted} | TouchIsSafe={TouchIsSafe}");
                    if (!RumpTouchIsTrusted && !TouchIsSafe)
                    {
                        if (!kickStarted && !kickLocked)
                        {
                            Debug.Log($"[KICK] Starting kick — hoofTouched={hoofTouched} legTouched={legTouched}");
                            int kickLeg = DetermineKickLeg();
                            Debug.Log($"[KICK] DetermineKickLeg() returned {kickLeg}");
                            GetLegRig(kickLeg)?.ChangeWeight(0f);
                            FireKickTrigger(kickLeg);
                            lastKickedLeg = kickLeg;
                            animator.SetLayerWeight(4, 1);
                            kickStarted = true;
                            kickLocked = true;
                            hasKicked = true;
                            Debug.Log($"[KICK] Layer 4 weight set to 1. Current state on layer 4: {animator.GetCurrentAnimatorStateInfo(4).fullPathHash}");
                        }
                        startAnxiousTimer = 1;
                    }
                    else
                    {
                        kickStarted = false;
                    }
                }
                else
                {
                    // Only release the cooldown once the kick has actually finished
                    // playing (kickStarted was cleared above). Clearing kickLocked
                    // while kickStarted is still true would let a new kick fire
                    // mid-animation — this is exactly what was happening for
                    // RearApproachLeft/Right, which never set touchedBehind/legTouched
                    // and so always fell into this branch on the very next frame.
                    if (!kickStarted)
                        kickLocked = false;
                }

                if (startAnxiousTimer >= 40f || HorseEmotion > 0)
                {
                    SetAllLegRigWeights(1f);
                    animator.SetLayerWeight(4, 0);
                    kickStarted = false;
                    kickLocked = false;
                    SwitchState(HorseStates.None);
                    animator.SetInteger("BehaviourStates", (int)currState);
                }
                break;

            case HorseStates.Walking:
                if (!startHorseWalk)
                {
                    SwitchState(HorseStates.None);
                    animator.SetInteger("BehaviourStates", (int)currState);
                    animator.SetLayerWeight(3, 0);
                }
                break;
        }
    }

    // ── Body-touch ref-count API (collègue) ──────────────────────────────────
    public void NotifyBodyTouch(bool entering)
    {
        bodyTouchCount = Mathf.Max(0, bodyTouchCount + (entering ? 1 : -1));
        lastBodyTouchTime = Time.time;
        Debug.Log($"HorseFsm: body touch count={bodyTouchCount} at {Time.time}");
    }

    // ── Per-leg kick targeting ─────────────────────────────────────────────
    // Picks which leg should perform the kick. hoofTouched is preferred since
    // it reflects the leg currently being held at the hoof; legTouched covers
    // the case where the hand is on the thigh (prep phase) but hasn't reached
    // the hoof. Falls back to DEFAULT_KICK_LEG if neither is set (e.g. kick
    // triggered purely from a rump touch with no leg contact at all).
    private int DetermineKickLeg()
    {
        if (hoofTouched != 0) return hoofTouched;
        if (legTouched != 0) return legTouched;
        return DEFAULT_KICK_LEG;
    }

    // ── Rear-approach immediate kick (no state gating) ──────────────────────
    // Called directly by BodyZoneTrigger when the player body proxy collider
    // enters a RearApproach zone.
    // existing kickStarted/kickLocked cooldown. Forcing SwitchState(Anxious)
    // means the end-of-kick reset already handled in the Anxious block of
    // Update() (normalizedTime >= 1f on layer 4) applies unchanged, so no
    // reset logic is duplicated here.
    //
    // forcedLeg lets the caller pin which leg kicks (3=BL, 4=BR) instead of
    // falling back to DetermineKickLeg()/DEFAULT_KICK_LEG. This is what lets
    // RearApproachLeft/RearApproachRight each fire the correct side even when
    // there's no hoof/leg touch on record to infer it from. Pass 0 (default)
    // to keep the old auto-detect behaviour.
    public void TriggerImmediateKick(int forcedLeg = 0)
    {
        Debug.Log($"[KICK] TriggerImmediateKick() called | forcedLeg={forcedLeg} | kickLocked={kickLocked} kickStarted={kickStarted} | t={Time.time:F2}s");

        if (!MovementAllowed) return;

        if (kickStarted || kickLocked) return;

        SwitchState(HorseStates.Anxious);
        animator.SetInteger("BehaviourStates", (int)currState);
        int kickLeg = forcedLeg != 0 ? forcedLeg : DetermineKickLeg();
        GetLegRig(kickLeg)?.ChangeWeight(0f);
        FireKickTrigger(kickLeg);
        lastKickedLeg = kickLeg;
        animator.SetLayerWeight(4, 1);
        kickStarted = true;
        kickLocked = true;
        hasKicked = true;
    }

    // Fires the Animator trigger matching legIndex (1=FL, 2=FR, 3=BL, 4=BR).
    // Trigger names must match the 4 kick states on the Animator Controller's
    // kick layer (layer 4).
    private void FireKickTrigger(int legIndex)
    {
        string triggerName;
        switch (legIndex)
        {
            case 1: triggerName = "KickFrontLeft"; break;
            case 2: triggerName = "KickFrontRight"; break;
            case 3: triggerName = "KickBackLeft"; break;
            case 4: triggerName = "KickBackRight"; break;
            default: triggerName = "KickBackRight"; break;
        }
        Debug.Log($"[KICK] Firing trigger: {triggerName} (legIndex={legIndex})");
        animator.SetTrigger(triggerName);
    }

    private void SwitchState(HorseStates newState)
    {
        currState = newState;
        startFeedingTimer = 0;
        exitFeedingTimer = 0;

        if (newState == HorseStates.None)
        {
            startAnxiousTimer = 0;
            HorseEmotion = 0f;
        }
    }

    IEnumerator FinishAnim(int layer, float length)
    {
        yield return new WaitForSeconds(length);
        animator.SetLayerWeight(layer, 0);
        SetAllLegRigWeights(1f);
    }

    public IEnumerator ResetHasGreeted(float delay)
    {
        yield return new WaitForSeconds(delay);
        hasGreeted = false;
    }

    public void ResetToInitialState()
    {
        StopAllCoroutines();

        handNearMouth = false;
        handBehindEar = false;
        touchedBehind = false;
        touchedHead = false;
        detectSideTouched = 0;
        sideStepDone = true;
        startHorseWalk = false;
        hasGreeted = false;
        hasKicked = false;
        kickStarted = false;
        kickLocked = false;
        bodyTouchCount = 0;
        lastBodyTouchTime = -999f;
        flinchStarted = false;
        flinchPlaying = false;
        hoofTouched = 0;
        hoofTouchTimer = 0f;
        hoofTouchWasContinuous = false;
        touchedBehindWasContinuous = false;
        legTouchWasContinuous = false;
        legTouched = 0;
        legTouchTimer = 0f;
        lastKickedLeg = 0;

        startFeedingTimer = 0f;
        exitFeedingTimer = 0f;
        startAnxiousTimer = 0f;
        HorseEmotion = 0f;

        for (int i = 1; i <= 6; i++)
            animator.SetLayerWeight(i, 0f);

        animator.ResetTrigger("KickFrontLeft");
        animator.ResetTrigger("KickFrontRight");
        animator.ResetTrigger("KickBackLeft");
        animator.ResetTrigger("KickBackRight");

        SetAllLegRigWeights(1f);

        SwitchState(HorseStates.None);
        animator.SetInteger("BehaviourStates", (int)HorseStates.None);
        animator.Play("idle1_Baked", 0, 0f);
        hoofWasTouched = false;
        lastZoneTouched = BodyZone.None;
        lastZoneTouchTime = -999f;

        // horse emotion reset
        emotionalState = EmotionalState.Neutral;
        firstTouchZone = null;
        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 0f);

        //Leg lifting reset 
        ResetLegLiftGate();
    }

    // ── Setters ──────────────────────────────────────────────────────────────
    public void SetHandNearMouth(bool v) { handNearMouth = v; }
    public void SetHandBehindEar(bool v) { handBehindEar = v; }
    public void SetSideStepDone(bool v) { sideStepDone = v; }
    public void SetTouchedBehind(bool v, bool cameFromContinuousCaress = false)
    {
        touchedBehind = v;
        if (v) touchedBehindWasContinuous = cameFromContinuousCaress;
    }
    public void SetStartHorseWalk(bool v) { startHorseWalk = v; }
    public void SetTouchedHead(bool v) { touchedHead = v; }
    public void SetSideTouched(int v) { detectSideTouched = v; }

    public void SetLegTouched(int legIndex, bool cameFromContinuousCaress = false)
    {
        Debug.Log($"[KICK] SetLegTouched called with: {legIndex}");
        legTouched = legIndex;
        legTouchTimer = 0f;
        if (legIndex != 0) legTouchWasContinuous = cameFromContinuousCaress;
    }

    public void SetHoofTouched(int hoofIndex, bool cameFromContinuousCaress = false)
    {
        Debug.Log($"[KICK] SetHoofTouched called with: {hoofIndex} (continuous={cameFromContinuousCaress})");
        hoofTouched = hoofIndex;
        hoofTouchTimer = 0f;
        if (hoofIndex != 0)
            hoofTouchWasContinuous = cameFromContinuousCaress;
    }

    private void ChangeEarRotation(Quaternion rightRotation, Quaternion leftRotation)
    {
        float noise1 = Mathf.PerlinNoise(Time.time * twitchSpeed, 0f);
        float angleOffset1 = (noise1 - 0.5f) * twitchAmount;
        Quaternion twitch1 = Quaternion.Euler(angleOffset1, 0f, 0f);

        rightEar.targetRotation = rightRotation * twitch1;
        leftEar.targetRotation = leftRotation * twitch1;
    }

    public void OnGentlePet() { HorseEmotion += 0.05f; }
    public void OnHarshTouch() { HorseEmotion -= 0.2f; startAnxiousTimer -= 2; }
    public void OnDangerTouch() { HorseEmotion -= 0.4f; startAnxiousTimer -= 2; }
}