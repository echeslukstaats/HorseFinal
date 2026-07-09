using UnityEngine;

// Physics proxy that represents the PLAYER'S BODY position (not their hands),
// following the same Rigidbody+MovePosition pattern as HandTracking.cs so it
// behaves consistently with the existing trigger-based zone system.
//
// Used for zones where what matters is "the player walked into this space"
// regardless of where their hands currently are (e.g. RearApproachLeft/Right —
// a player can walk behind the horse with arms crossed or hands lowered and
// still be in physical danger of getting kicked).
//
// X/Z follow the head anchor (room-scale movement). Y is locked to a fixed
// body height above the floor reference, so crouching or looking down/up
// doesn't make the collider bob in and out of a trigger zone.
[RequireComponent(typeof(Rigidbody))]
public class PlayerBodyTracker : MonoBehaviour
{
    [Tooltip("Usually the OVRCameraRig's CenterEyeAnchor. Only its X/Z position is used — Y is locked below via fixedBodyHeight.")]
    public Transform headAnchor;

    [Tooltip("Transform whose Y position is treated as the floor (e.g. the OVRCameraRig root, or the horse's own root if the play space floor is at the same Y as the horse). Leave empty to use world Y=0 as the floor.")]
    public Transform floorReference;

    [Tooltip("Height above the floor at which the body collider is kept, regardless of head height (crouch/lean/head-bob).")]
    public float fixedBodyHeight = 1f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void FixedUpdate()
    {
        if (headAnchor == null) return;

        float floorY = floorReference != null ? floorReference.position.y : 0f;

        Vector3 targetPosition = new Vector3(
            headAnchor.position.x,
            floorY + fixedBodyHeight,
            headAnchor.position.z
        );

        rb.MovePosition(targetPosition);
    }
}