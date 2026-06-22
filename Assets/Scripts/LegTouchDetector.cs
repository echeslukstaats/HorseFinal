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
        if (!other.CompareTag("VRHand")) return;

        OnTouched();
    }

    public void OnTouched()
    {
        HorseFsm.BodyZone zone = ZoneFromLeg(legIndex, isHoof);
        bool continuous = horseFsm.NotifyZoneEnter(zone);

        if (isHoof)
            horseFsm.SetHoofTouched(legIndex, continuous);
        else
            horseFsm.SetLegTouched(legIndex);
    }

    private static HorseFsm.BodyZone ZoneFromLeg(int legIndex, bool isHoof)
    {
        switch (legIndex)
        {
            case 1: return isHoof ? HorseFsm.BodyZone.HoofFL : HorseFsm.BodyZone.LegFL;
            case 2: return isHoof ? HorseFsm.BodyZone.HoofFR : HorseFsm.BodyZone.LegFR;
            case 3: return isHoof ? HorseFsm.BodyZone.HoofBL : HorseFsm.BodyZone.LegBL;
            case 4: return isHoof ? HorseFsm.BodyZone.HoofBR : HorseFsm.BodyZone.LegBR;
            default: return HorseFsm.BodyZone.None;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("VRHand")) return;

        if (isHoof)
            horseFsm.SetHoofTouched(0);
    }
}