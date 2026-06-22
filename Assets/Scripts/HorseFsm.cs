using UnityEngine;
using System.Collections;

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

    // ── Body-touch ref-count (collègue) ──────────────────────────────────────
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

    // ── Flinch system (Khalis) ────────────────────────────────────────────────
    private bool flinchStarted = false;
    private bool flinchPlaying = false;
    private int legTouchedFirst = 0; // 0=none 1=FL 2=FR 3=BL 4=BR
    private int hoofTouched = 0;     // 0=none 1=FL 2=FR 3=BL 4=BR
    private bool hoofWasTouched = false;
    private float legPrepTimer = 0f;
    private const float LEG_PREP_TIMOUT = 3f;

    private void Start()
    {
        animator.SetInteger("BehaviourStates", (int)currState);
    }

    private void Update()
    {
        // Flinch runs independently of the current horse state.
        if (legTouchedFirst != 0)
        {
            legPrepTimer += Time.deltaTime;
            if (legPrepTimer >= LEG_PREP_TIMOUT)
            {
                legTouchedFirst = 0;
                legPrepTimer = 0f;
            }
        }

        bool hoofIsTouchedNow = hoofTouched != 0;

        if (hoofIsTouchedNow && !hoofWasTouched)
        {
            Debug.Log("Hoof touched: " + hoofTouched + " legTouchedFirst: " + legTouchedFirst + " flinchStarted: " + flinchStarted);

            bool cameFromLeg = (legTouchedFirst == hoofTouched);

            if (!cameFromLeg && !flinchStarted)
            {
                animator.SetInteger("FlinchState", hoofTouched);
                animator.SetLayerWeight(6, 1);
                flinchStarted = true;
                flinchPlaying = false;
            }

            legTouchedFirst = 0;
            legPrepTimer = 0f;
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

        switch (currState)
        {
            case HorseStates.None:

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
                if ((handBehindEar && !hasGreeted) || (touchedBehind && !RumpTouchIsTrusted))
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

                if (touchedBehind)
                {
                    // ── Kick guard (collègue : RumpTouchIsTrusted) ───────────
                    if (!RumpTouchIsTrusted)
                    {
                        legRigWeight.ChangeWeight(0f);

                        if (!kickStarted)
                        {
                            animator.Play("Horse|kick_baked", 4, 0f);
                            animator.SetLayerWeight(4, 1);
                            kickStarted = true;
                            hasKicked = true;
                        }

                        startAnxiousTimer = 1;

                        if (animator.GetCurrentAnimatorStateInfo(4).normalizedTime >= 1f)
                        {
                            animator.SetLayerWeight(4, 0);
                            legRigWeight.ChangeWeight(1f);
                            kickStarted = false;
                        }
                    }
                    else
                    {
                        kickStarted = false;
                    }
                }
                else
                {
                    kickStarted = false;
                }

                if (startAnxiousTimer >= 40f || HorseEmotion > 0)
                {
                    legRigWeight.ChangeWeight(1f);
                    animator.SetLayerWeight(4, 0);
                    kickStarted = false;
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
        bodyTouchCount = 0;
        lastBodyTouchTime = -999f;
        flinchStarted = false;
        flinchPlaying = false;
        legTouchedFirst = 0;
        hoofTouched = 0;
        legPrepTimer = 0f;

        startFeedingTimer = 0f;
        exitFeedingTimer = 0f;
        startAnxiousTimer = 0f;
        HorseEmotion = 0f;

        for (int i = 1; i <= 6; i++)
            animator.SetLayerWeight(i, 0f);

        legRigWeight.ChangeWeight(1f);

        SwitchState(HorseStates.None);
        animator.SetInteger("BehaviourStates", (int)HorseStates.None);
        animator.Play("idle1_Baked", 0, 0f);
        hoofWasTouched = false;
    }

    // ── Setters ──────────────────────────────────────────────────────────────
    public void SetHandNearMouth(bool v) { handNearMouth = v; }
    public void SetHandBehindEar(bool v) { handBehindEar = v; }
    public void SetSideStepDone(bool v) { sideStepDone = v; }
    public void SetTouchedBehind(bool v) { touchedBehind = v; }
    public void SetStartHorseWalk(bool v) { startHorseWalk = v; }
    public void SetTouchedHead(bool v) { touchedHead = v; }
    public void SetSideTouched(int v) { detectSideTouched = v; }

    // ── Flinch setters (Khalis) ───────────────────────────────────────────────
    public void SetLegTouched(int legIndex)
    {
        Debug.Log("SetLegTouched called with: " + legIndex);
        legTouchedFirst = legIndex;
        legPrepTimer = 0f;
    }

    public void SetHoofTouched(int hoofIndex)
    {
        Debug.Log("SetHoofTouched called with: " + hoofIndex + " legTouchedFirst: " + legTouchedFirst);
        hoofTouched = hoofIndex;
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
