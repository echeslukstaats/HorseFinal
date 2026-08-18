using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ChangeLegRigWeight : MonoBehaviour
{
    private Rig legRig;
    public RigBuilder rigbuilder;
    public Animator animator;

    void Awake()
    {
        legRig = GetComponent<Rig>();
        rigbuilder = GetComponentInParent<RigBuilder>();
    }

    public void ChangeWeight(float newValue)
    {
        Debug.Log($"[LEG-LIFT-DEBUG] ChangeWeight: {legRig.weight} → {newValue} | t={Time.time:F2}s");
        legRig.weight = newValue;
        rigbuilder.Build();
    }
}
