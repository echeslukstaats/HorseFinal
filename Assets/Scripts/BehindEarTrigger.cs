using UnityEngine;

public class BehindEarTrigger : MonoBehaviour
{
    public HorseFsm horseFsm;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VRHand"))
        {
            Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

            if (localPos.z > 0)
            {
                horseFsm.SetSideTouched( 1);
                Debug.Log("sidestepR");
               
            }
            else
            {
                horseFsm.SetSideTouched(2);
                Debug.Log("sidestepL");

            }
            horseFsm.SetHandBehindEar(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("VRHand"))
        {
            
            horseFsm.SetHandBehindEar(false);
        }
    }
}
