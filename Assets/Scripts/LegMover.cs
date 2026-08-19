using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;

public class LegMover : MonoBehaviour
{

    public HorseFsm horseFsm;
    [Tooltip("1=Front Left, 2=Front Right, 3=Back Left, 4=Back Right — must match the LegTouchDetector on this same leg.")]
    public int legIndex;

    public Transform ikRoot;
    public Transform ikTarget;
    public Transform hoofRotator;
    public TwoBoneIKConstraint IKConstraint;

    public float maxY = 0.05f;
    public float minY = -0.3f;
    public float maxZ = 0.15f;
    public float minZ = -0.25f;
    public float maxX = 0.4f;
    public float minX = -0.4f;

    private bool isGrabbed = false;
    private Transform leadHand = null;
    private Vector3 grabDifference;

    private Quaternion initialHandRotation;
    private Vector3 initialLegPosition;

    private bool initialFlinchDone = false;
    private float FlinchTimer = 0f;
    private float FlinchDuration = 0.4f;
    private Vector3 FlinchOffset;

    public bool isFlinching = false;


    public OVRHand leftOVRHand;
    public OVRHand rightOVRHand;

    // ── Leg-petting detection (gates lift in Dynamic mode) ─────────────────
    private struct HandSample { public Vector3 pos; public float time; }
    private Dictionary<Transform, Queue<HandSample>> pettingHistory = new Dictionary<Transform, Queue<HandSample>>();
    private Dictionary<Transform, float> pettingTimes = new Dictionary<Transform, float>();
    private const float PETTING_WINDOW = 0.6f;
    private const float PETTING_GENTLE_MIN = 0.05f;
    private const float PETTING_GENTLE_MAX = 0.5f;
    private const float PETTING_MIN_TIME = 0.5f;

    [Header("Hoof Rotation Clamp (tune per leg — front/back hoof bones may differ in orientation)")]
    public Vector3 rotationClampMin = new Vector3(-60f, -10f, -40f);
    public Vector3 rotationClampMax = new Vector3(60f, 10f, 40f);

    private void Start()
    {
        Debug.Log($"[LEG-LIFT-DEBUG] leg={legIndex} ikRoot WORLD position={ikRoot.position} | ikTarget WORLD position (repos)={ikTarget.position}");
        initialLegPosition = ikTarget.localPosition;
        hoofRotator.localRotation = Quaternion.identity;

    }

    private void Update()
    {

        if (isFlinching) return; // Skip movement if flinching

        if (isGrabbed && leadHand != null)
        {
            if (!IsGripping(leadHand))
            {
                ResetLeg();
                ReturnLeg(leadHand);
                return;
            }
            MoveLeg();
            RotateHoof();
        }
        else
        {
            ResetLeg();
        }

    }
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        // Petting detection always runs (even while grabbed elsewhere), so a
        // player already gripping can still be building up petting credit
        // with their other hand.
        TrackPetting(other.transform);

        if (isGrabbed) return;

        bool allowed = horseFsm == null || horseFsm.CanLiftLeg(legIndex);
        if (!allowed) return; // gate closed: Dynamic mode, not yet petted (or reset by Anxious)

        if (IsGripping(other.transform))
        {
            GrabLeg(other.transform);
        }
    }

    private bool IsGripping(Transform handTransform)
    {
        bool isLeft = handTransform.name.ToLower().Contains("left");
        bool isRight = handTransform.name.ToLower().Contains("right");


        if (isLeft && leftOVRHand != null && leftOVRHand.IsTracked)
            return leftOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (isRight && rightOVRHand != null && rightOVRHand.IsTracked)
            return rightOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (isLeft)
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) > 0.7f;

        if (isRight)
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) > 0.7f;

        return false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        pettingHistory.Remove(other.transform);
        pettingTimes.Remove(other.transform);

        if (other.transform == leadHand && isGrabbed)
        {
            ReturnLeg(other.transform);
        }
    }

    private void GrabLeg(Transform vrHand)
    {
        if (isGrabbed) return;

        Debug.Log($"[LEG-LIFT-DEBUG] GrabLeg() STARTED leg={legIndex} by {vrHand.name}");

        leadHand = vrHand;
        isGrabbed = true;
        grabDifference = ikTarget.position - vrHand.position;
        initialHandRotation = vrHand.rotation;

        initialFlinchDone = false;
        FlinchTimer = 0f;
        FlinchOffset = new Vector3(0f, Random.Range(0.05f, 0.1f), 0f);
    }

    private Vector3 InitialReflex()
    {
        if (initialFlinchDone) return Vector3.zero;

        FlinchTimer += Time.deltaTime;
        float t = FlinchTimer / FlinchDuration;
        float jerkY = FlinchOffset.y * (1f - Mathf.Pow(t, 2));

        if (FlinchTimer >= FlinchDuration)
            initialFlinchDone = true;

        return new Vector3(0f, jerkY, 0f);
    }

    private void MoveLeg()
    {
        Vector3 handTargetPos = leadHand.position + grabDifference;
        handTargetPos += InitialReflex();

        Vector3 difference = handTargetPos - ikRoot.position;
        Debug.Log($"[LEG-LIFT-DEBUG] leg={legIndex} RAW difference={difference}");
        
        difference.y = Mathf.Clamp(difference.y, minY, maxY);
        difference.x = Mathf.Clamp(difference.x, minX, maxX);
        difference.z = Mathf.Clamp(difference.z, minZ, maxZ);
        Debug.Log($"[LEG-LIFT-DEBUG] leg={legIndex} CLAMPED difference={difference} (minY={minY} maxY={maxY})");


        ikTarget.position = ikRoot.position + difference;
        Vector3 finalPos = ikTarget.position;
        finalPos.y = Mathf.Clamp(ikTarget.position.y, 0, 0.6f);
        ikTarget.position = finalPos;
    }

    private void RotateHoof()
    {

        float liftAmount = initialLegPosition.y - ikTarget.localPosition.y;

        float liftAngle = Mathf.Clamp(liftAmount * 320f, -95f, 45f);


        Quaternion liftQuat = Quaternion.identity;

        if (gameObject.name.Contains("FrontL"))
        {
            liftQuat = Quaternion.Euler(-liftAngle, 0f, 0f);
        }
        else if (gameObject.name.Contains("Back"))
        {
            liftQuat = Quaternion.Euler(liftAngle, 0f, 0f);
        }

        Quaternion finalRotation = liftQuat;


        if (isGrabbed && leadHand != null)
        {
            Quaternion handOffset = leadHand.rotation * Quaternion.Inverse(initialHandRotation);

            handOffset = ClampEulerAngles(
                handOffset,
                rotationClampMin,
                rotationClampMax    
            );
            Debug.Log($"[LEG-LIFT-DEBUG-TWIST] leg={legIndex} handOffset euler (avant clamp)={handOffset.eulerAngles}");

            finalRotation = liftQuat * handOffset;
        }

        hoofRotator.localRotation = finalRotation;
    }

    private void ReturnLeg(Transform vrHand)
    {
        if (vrHand != leadHand)
        {
            return;
        }
        isGrabbed = false;
        leadHand = null;
    }

    private void ResetLeg()
    {
        ikTarget.localPosition = Vector3.Lerp(
            ikTarget.localPosition,
            initialLegPosition,
             Time.deltaTime * 2f);

        hoofRotator.localRotation = Quaternion.Slerp(
            hoofRotator.localRotation,
            Quaternion.identity,
            Time.deltaTime * 2f
        );

    }

    private Quaternion ClampEulerAngles(Quaternion q, Vector3 minAngles, Vector3 maxAngles)
    {
        Vector3 euler = q.eulerAngles;

        euler.x = (euler.x > 180f) ? euler.x - 360f : euler.x;
        euler.y = (euler.y > 180f) ? euler.y - 360f : euler.y;
        euler.z = (euler.z > 180f) ? euler.z - 360f : euler.z;

        euler.x = Mathf.Clamp(euler.x, minAngles.x, maxAngles.x);
        euler.y = Mathf.Clamp(euler.y, minAngles.y, maxAngles.y);
        euler.z = Mathf.Clamp(euler.z, minAngles.z, maxAngles.z);
        return Quaternion.Euler(euler);
    }

    private void TrackPetting(Transform hand)
    {
        if (!pettingHistory.ContainsKey(hand))
        {
            pettingHistory[hand] = new Queue<HandSample>();
            pettingTimes[hand] = 0f;
        }

        var history = pettingHistory[hand];
        history.Enqueue(new HandSample { pos = hand.position, time = Time.time });
        while (history.Count > 0 && Time.time - history.Peek().time > PETTING_WINDOW)
            history.Dequeue();

        if (history.Count < 2) return;

        float distance = Vector3.Distance(hand.position, history.Peek().pos);
        float elapsed = Time.time - history.Peek().time;
        float speed = elapsed > 0f ? distance / elapsed : 0f;

        if (speed > PETTING_GENTLE_MIN && speed < PETTING_GENTLE_MAX)
        {
            pettingTimes[hand] += Time.deltaTime;
            if (pettingTimes[hand] >= PETTING_MIN_TIME && horseFsm != null)
            {
                horseFsm.ConfirmLegPetting(legIndex);
                pettingTimes[hand] = Mathf.Max(0f, pettingTimes[hand] - Time.deltaTime);
            }
        }
        else
        {
            pettingTimes[hand] = Mathf.Max(0f, pettingTimes[hand] - Time.deltaTime * 0.5f);
        }
    }
}
