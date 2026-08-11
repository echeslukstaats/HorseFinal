using UnityEngine;

// Gates the horse's leg-lift interaction according to HorseFsm.CanLiftLeg().
// Attach this to the SAME hoof GameObject as the LegTouchDetector configured
// with isHoof = true for this leg (legIndex here must match).
//
// This script does NOT detect touch itself — it only keeps the actual grab/poke
// interactable component's enabled state in sync with the gate (Static mode:
// always enabled; Dynamic mode: enabled only after HorseFsm.ConfirmLegPetting()
// has unlocked this leg).
public class LegLiftInteractable : MonoBehaviour
{
    public HorseFsm horseFsm;

    [Tooltip("1=Front Left, 2=Front Right, 3=Back Left, 4=Back Right — must match the LegTouchDetector on this same hoof.")]
    public int legIndex;

    [Tooltip("The Meta Interaction SDK component that performs the actual grab/poke lift interaction on this hoof (e.g. a HandGrabInteractable). Its 'enabled' state is toggled by the gate below.")]
    public MonoBehaviour grabInteractable;

    private bool lastAllowedState;

    private void Start()
    {
        SyncInteractableState(force: true);
    }

    private void Update()
    {
        SyncInteractableState(force: false);
    }

    private void SyncInteractableState(bool force)
    {
        if (horseFsm == null || grabInteractable == null) return;

        bool allowed = horseFsm.CanLiftLeg(legIndex);

        if (force || allowed != lastAllowedState)
        {
            grabInteractable.enabled = allowed;
            Debug.Log($"[LEG-LIFT] Leg {legIndex} lift interactable enabled={allowed} (mode={horseFsm.interactionMode})");
            lastAllowedState = allowed;
        }
    }
}