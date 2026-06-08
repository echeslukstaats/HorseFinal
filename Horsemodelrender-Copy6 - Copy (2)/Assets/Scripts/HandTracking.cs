using UnityEngine;

public class HandTracking : MonoBehaviour
{
    public Transform trackedHand;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    private Rigidbody rb;
    private SphereCollider sphereCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        sphereCollider = GetComponent<SphereCollider>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(trackedHand.position);
        rb.MoveRotation(trackedHand.rotation);
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