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

    private void Start()
    {
        animator.SetInteger("BehaviourStates", (int)currState);
    }

    private void Update()
    {
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

                if ((handBehindEar && !hasGreeted) || touchedBehind)
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

        startFeedingTimer = 0f;
        exitFeedingTimer = 0f;
        startAnxiousTimer = 0f;

        HorseEmotion = 0f;

        for (int i = 1; i <= 5; i++)
            animator.SetLayerWeight(i, 0f);

        legRigWeight.ChangeWeight(1f);

        SwitchState(HorseStates.None);
        animator.SetInteger("BehaviourStates", (int)HorseStates.None);
        animator.Play("idle1_Baked", 0, 0f);
    }

    public void SetHandNearMouth(bool handNearMouth)
    {
        this.handNearMouth = handNearMouth;
    }

    public void SetHandBehindEar(bool handBehindEar)
    {
        this.handBehindEar = handBehindEar;
    }

    public void SetSideStepDone(bool sideStepDone)
    {
        this.sideStepDone = sideStepDone;
    }

    public void SetTouchedBehind(bool touchedBehind)
    {
        this.touchedBehind = touchedBehind;
    }

    public void SetStartHorseWalk(bool startHorseWalk)
    {
        this.startHorseWalk = startHorseWalk;
    }

    public void SetTouchedHead(bool touchedHead)
    {
        this.touchedHead = touchedHead;
    }

    public void SetSideTouched(int sideTouched)
    {
        this.detectSideTouched = sideTouched;
    }

    private void ChangeEarRotation(Quaternion rightRotation, Quaternion leftRotation)
    {
        float noise1 = Mathf.PerlinNoise(Time.time * twitchSpeed, 0f);
        float angleOffset1 = (noise1 - 0.5f) * twitchAmount;
        Quaternion twitch1 = Quaternion.Euler(angleOffset1, 0f, 0f);

        rightEar.targetRotation = rightRotation * twitch1;
        leftEar.targetRotation = leftRotation * twitch1;
    }

    public void OnGentlePet()
    {
        Debug.Log("gentle pet" + HorseEmotion);
        HorseEmotion += 0.05f;
    }

    public void OnHarshTouch()
    {
        Debug.Log("bad pet" + HorseEmotion);
        HorseEmotion -= 0.2f;
        startAnxiousTimer -= 2;
    }

    public void OnDangerTouch()
    {
        Debug.Log("danger pet" + HorseEmotion);
        HorseEmotion -= 0.4f;
        startAnxiousTimer -= 2;
    }
}