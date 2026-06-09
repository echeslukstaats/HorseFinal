using UnityEngine;

public class LegTouchDetector : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Header("Leg Index")]
    [Tooltip("1=Front Left, 2=Front Right, 3=Back Left, 4=Back Right")]
    public int legIndex;

    [Header("Is this a hoof collider?")]
    public bool isHoof = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Finger")) return;

        if (isHoof)
            horseFsm.SetHoofTouched(legIndex);
        else
            horseFsm.SetLegTouched(legIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Finger")) return;

        if (!isHoof)
            horseFsm.SetLegTouched(0);
    }
}