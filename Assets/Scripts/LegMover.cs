using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.VisualScripting;
using System.Collections;

public class LegMover : MonoBehaviour
{
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

    private void Start()
    {
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
        if (other.CompareTag("TrackedHand") && !isGrabbed)
        {

            if (IsGripping(other.transform))
            {
                GrabLeg(other.transform);
            }
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
        if (other.transform == leadHand && isGrabbed)
        {
            ReturnLeg(other.transform);

        }
    }

    private void GrabLeg(Transform vrHand)
    {
        if (isGrabbed) return;

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
        difference.y = Mathf.Clamp(difference.y, minY, maxY);
        difference.x = Mathf.Clamp(difference.x, minX, maxX);
        difference.z = Mathf.Clamp(difference.z, minZ, maxZ);


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
                new Vector3(-60f, -30f, -40f),
                new Vector3(60f, 30f, 40f)
            );

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
}
