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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        sphereCollider = GetComponent<SphereCollider>();
    }

    void FixedUpdate()
    {
        Vector3 targetPosition = trackedHand.position;

        if (hasPreviousPosition)
        {
            SweepCheck(previousPosition, targetPosition);
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(trackedHand.rotation);

        previousPosition = targetPosition;
        hasPreviousPosition = true;
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
                    legDetector.NotifyTouched();
                    continue;
                }

                // Hindquarters (kick) — usually not necessary given the collider size,
                // but it does not hurt to be safe
                var behindTrigger = hit.collider.GetComponent<BehindTrigger>();
                if (behindTrigger != null)
                {
                    behindTrigger.NotifyTouched();
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