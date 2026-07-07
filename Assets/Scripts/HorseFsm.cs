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

    public ChangeLegRigWeight legRigWeight;

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
        LegFL, HoofFL, LegFR, HoofFR, LegBL, HoofBL, LegBR, HoofBR
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
    };

    public enum EmotionalState { Neutral, Happy, Anxious }
    public EmotionalState emotionalState { get; private set; } = EmotionalState.Neutral;
    private string firstTouchZone = null;
    private const int EARS_LAYER = 7;


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
        if (!continuous)
        {
            firstTouchZone = zone.ToString();
            emotionalState = (zone == BodyZone.Neck) ? EmotionalState.Happy : EmotionalState.Anxious;

            Debug.Log($"[EMOTION-STATE] {emotionalState} triggered from {firstTouchZone} | t={Time.time:F2}s | gating reset");

            if (emotionalState == EmotionalState.Happy) TriggerEarsHappy();
            else TriggerEarsAnxious();
        }
        else
        {
            // Continuity holds: the state persists no matter which adjacent zone the
            // hand is now on. This is what lets cross-zone petting (Neck → Body →
            // Neck) keep the horse Happy without re-triggering the gate.
            Debug.Log($"[EMOTION-STATE] {emotionalState} persists (continuous touch, now on {zone}) | firstTouchZone={firstTouchZone} | t={Time.time:F2}s");
        }
    }

    public void TriggerEarsHappy()
    {
        animator.SetBool("isHappy", true);
        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 1f);
        Debug.Log($"[EMOTION-STATE] TriggerEarsHappy() — isHappy=true isAnxious=false layer{EARS_LAYER}.weight=1 | t={Time.time:F2}s");
    }

    public void TriggerEarsAnxious()
    {
        animator.SetBool("isAnxious", true);
        animator.SetBool("isHappy", false);
        animator.SetLayerWeight(EARS_LAYER, 1f);
        Debug.Log($"[EMOTION-STATE] TriggerEarsAnxious() — isAnxious=true isHappy=false layer{EARS_LAYER}.weight=1 | t={Time.time:F2}s");
    }

    private void ResetEmotionalStateToNeutral()
    {
        if (emotionalState == EmotionalState.Neutral) return; // avoid log spam every frame

        emotionalState = EmotionalState.Neutral;
        firstTouchZone = null;
        animator.SetBool("isHappy", false);
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
            if (!hoofTouchWasContinuous && !flinchStarted)
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

                // Ne pilote les oreilles par physique que si aucune couche emotionnelle
                // n'est active, pour eviter un conflit avec le layer 7 (Ears Animator).
                if (emotionalState == EmotionalState.Neutral)
                    ChangeEarRotation(Quaternion.identity, Quaternion.identity);

                if (handNearMouth)
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
                if ((touchedBehind || legTouched != 0) && !RumpTouchIsTrusted && !TouchIsSafe)
                {
                    HorseEmotion = -3f;
                    legRigWeight.ChangeWeight(0f);
                    SwitchState(HorseStates.Anxious);
                    animator.SetInteger("BehaviourStates", (int)currState);

                    if (handBehindEar && !hasGreeted)
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

                if (startHorseWalk)
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

                if (emotionalState == EmotionalState.Neutral)
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

                if (touchedBehind || legTouched != 0)
                {
                    Debug.Log($"[KICK-GATE] touchedBehind={touchedBehind} (continuous={touchedBehindWasContinuous}) | legTouched={legTouched} (continuous={legTouchWasContinuous}) | RumpTouchIsTrusted={RumpTouchIsTrusted} | TouchIsSafe={TouchIsSafe}");
                    // ── Kick guard (collègue : RumpTouchIsTrusted) ───────────
                    if (!RumpTouchIsTrusted && !TouchIsSafe)
                    {
                        legRigWeight.ChangeWeight(0f);

                        if (!kickStarted && !kickLocked)
                        {
                            Debug.Log($"[KICK] Starting kick — hoofTouched={hoofTouched} legTouched={legTouched}");
                            int kickLeg = DetermineKickLeg();
                            Debug.Log($"[KICK] DetermineKickLeg() returned {kickLeg}");
                            FireKickTrigger(kickLeg);
                            lastKickedLeg = kickLeg;
                            animator.SetLayerWeight(4, 1);
                            kickStarted = true;
                            kickLocked = true;
                            hasKicked = true;
                            Debug.Log($"[KICK] Layer 4 weight set to 1. Current state on layer 4: {animator.GetCurrentAnimatorStateInfo(4).fullPathHash}");
                        }
                        /*
                        else if (kickStarted && animator.GetCurrentAnimatorStateInfo(4).normalizedTime >= 1f)
                        {
                            Debug.Log("[KICK] Kick finished, resetting layer 4 weight to 0");
                            animator.SetLayerWeight(4, 0);
                            legRigWeight.ChangeWeight(1f);
                            kickStarted = false;
                            lastKickedLeg = 0;
                        }*/
                        else if (kickStarted)
                        {
                            var info = animator.GetCurrentAnimatorStateInfo(4);

                            if (info.normalizedTime >= 1f)
                            {
                                Debug.Log("[KICK] Kick finished, resetting layer 4 weight to 0");
                                animator.SetLayerWeight(4, 0);
                                legRigWeight.ChangeWeight(1f);
                                kickStarted = false;
                                lastKickedLeg = 0;
                            }
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
                    kickStarted = false;
                    kickLocked = false;
                }

                if (startAnxiousTimer >= 40f || HorseEmotion > 0)
                {
                    legRigWeight.ChangeWeight(1f);
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
        legRigWeight.ChangeWeight(1);
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

        legRigWeight.ChangeWeight(1f);

        SwitchState(HorseStates.None);
        animator.SetInteger("BehaviourStates", (int)HorseStates.None);
        animator.Play("idle1_Baked", 0, 0f);
        hoofWasTouched = false;
        lastZoneTouched = BodyZone.None;
        lastZoneTouchTime = -999f;

        // horse emotion reset
        emotionalState = EmotionalState.Neutral;
        firstTouchZone = null;
        animator.SetBool("isHappy", false);
        animator.SetBool("isAnxious", false);
        animator.SetLayerWeight(EARS_LAYER, 0f);
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