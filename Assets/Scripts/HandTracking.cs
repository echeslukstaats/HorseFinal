using UnityEngine;

public class HandTracking : MonoBehaviour
{
    public Transform trackedHand;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Anti-tunneling detection")]
    [Tooltip("Layers to include in the SphereCast (legs, hooves, hindquarters...)")]
    public LayerMask sweepLayers = ~0;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Vector3 previousPosition;
    private bool hasPreviousPosition = false;

    [Header("Debug")]
    [Tooltip("Logs trackedHand source position vs. applied collider position every N FixedUpdate calls. Set to 0 to disable.")]
    public int debugLogEveryNFrames = 30;
    private int debugFrameCounter = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        sphereCollider = GetComponent<SphereCollider>();

        if (trackedHand == null)
            Debug.LogError($"[HandTracking] {gameObject.name}: trackedHand is NOT assigned in the inspector.");

        if (sphereCollider == null)
            Debug.LogError($"[HandTracking] {gameObject.name}: no SphereCollider found on this GameObject — SweepCheck will throw.");
    }

    void FixedUpdate()
    {
        if (trackedHand == null) return; // avoid NRE spam if unassigned; already logged in Awake

        Vector3 targetPosition = trackedHand.position;

        if (hasPreviousPosition)
        {
            SweepCheck(previousPosition, targetPosition);
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(trackedHand.rotation);

        previousPosition = targetPosition;
        hasPreviousPosition = true;

        if (debugLogEveryNFrames > 0)
        {
            debugFrameCounter++;
            if (debugFrameCounter >= debugLogEveryNFrames)
            {
                debugFrameCounter = 0;
                float drift = Vector3.Distance(rb.position, trackedHand.position);
                Debug.Log($"[HAND-POS] {gameObject.name} | trackedHand({trackedHand.name})={trackedHand.position:F3} | rb.position={rb.position:F3} | drift={drift:F4}m | t={Time.time:F2}s");
            }
        }
    }

    private void SweepCheck(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;

        if (distance < 0.0001f) return; // hand is nearly stationary, nothing to catch up on

        Vector3 direction = delta / distance;
        float radius = sphereCollider.radius;

        // SphereCastAll: capture everything the sphere should have touched
        // between the previous and new position, even if it "jumped" over it
        RaycastHit[] hits = Physics.SphereCastAll(
            from, radius, direction, distance, sweepLayers, QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag("VRHand") && hit.collider.gameObject != gameObject)
            {
                // Leg / hoof
                var legDetector = hit.collider.GetComponent<LegTouchDetector>();
                if (legDetector != null)
                {
                    legDetector.OnTouched();
                    continue;
                }

                // Hindquarters (kick) — usually not necessary given the collider size,
                // but it does not hurt to be safe
                var behindTrigger = hit.collider.GetComponent<BehindTrigger>();
                if (behindTrigger != null)
                {
                    behindTrigger.OnTouched();
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null) return;

        // Account for the collider's center offset and lossy scale
        float scaledRadius = sphereCollider.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        );
        Vector3 worldCenter = transform.TransformPoint(sphereCollider.center);

        // Draw filled sphere
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(worldCenter, scaledRadius);

        // Draw wireframe outline on top
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(worldCenter, scaledRadius);
    }
}