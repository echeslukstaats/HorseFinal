using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HorseTargetFollow : MonoBehaviour
{
    public Transform target;
    public Transform hand;
    public HorseFsm horseFsm;
    public HorseStates states;
    public MultiAimConstraint headMac;
    private float followSpeed = 3f;
    private float weightSpeed = 2f;
    private Vector3 ogPosition;

    //public float targetWeight;

    private void Start()
    {
        ogPosition = target.position;
    }

    private void Update()
    {
       
        if (hand == null ||  horseFsm == null) return;
        if (horseFsm.currState != HorseStates.Feeding)
        {
            //moves target back to og position
            target.position = Vector3.Lerp(target.position, ogPosition, Time.deltaTime * followSpeed);
            //converts constraint weight to 0
            headMac.weight =Mathf.Lerp(headMac.weight, 0f, Time.deltaTime * weightSpeed);
            return;
        }

            headMac.weight = Mathf.Lerp(headMac.weight, 1f, Time.deltaTime * weightSpeed);
            target.position = Vector3.Lerp(target.position, hand.position, Time.deltaTime * followSpeed);
        


    }
}
