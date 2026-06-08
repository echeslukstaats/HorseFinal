using UnityEngine;


public class TransformMirror : MonoBehaviour
{
    [Tooltip("The object that will mirror horizontal movement of this object.")]
    public Transform target;

    // World-space position of this object at the previous frame
    private Vector3 _previousPosition;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"[TransformMirror] No target assigned on '{name}'. Script will do nothing.", this);
            enabled = false;
            return;
        }

        // Record starting position — no movement applied yet
        _previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        // Calculate how far this object moved since the last frame
        Vector3 delta = transform.position - _previousPosition;

        // Only apply horizontal movement (X and Z), ignore Y
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);

        if (horizontalDelta != Vector3.zero)
        {
            target.position += horizontalDelta * 2f;
        }

        _previousPosition = transform.position;
    }
}