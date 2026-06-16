using UnityEngine;

public class HoofRotateLimit : MonoBehaviour
{

    public float minX = -45f;
    public float maxX = 45f;

    public float minY = -40f;
    public float maxY = 40f;

    public float minZ = -25f;
    public float maxZ = 25f;

    private void LateUpdate()
    {
        Vector3 angles = transform.localEulerAngles;

        angles.x = ClampAngle(angles.x, minX, maxX);
        angles.y = ClampAngle(angles.y, minY, maxY);
        angles.z = ClampAngle(angles.z, minZ, maxZ);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        angle = SetCorrectAngle(angle);
        return Mathf.Clamp(angle, min, max);
    }

    private float SetCorrectAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
