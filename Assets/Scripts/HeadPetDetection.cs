using UnityEngine;

public class HeadPetDetection : MonoBehaviour
{
    public HorseFsm horseFsm;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VRHand"))
        {
           horseFsm.NotifyZoneEnter(HorseFsm.BodyZone.Head);

           if(horseFsm.currState == HorseStates.None)
            {
                if(horseFsm.hasGreeted == false)
                {
                    horseFsm.hasGreeted = true;
                    horseFsm.ResetHasGreeted(60f);
                }
            }
        }
    }


}
