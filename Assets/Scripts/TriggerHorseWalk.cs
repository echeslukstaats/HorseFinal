using UnityEngine;
using UnityEngine.InputSystem;
public class TriggerHorseWalk : MonoBehaviour
{

    public HorseFsm horseFsm;
    public InputActionReference walkButton;

    private void OnEnable()
    {
        walkButton.action.performed += OnWalkPressed;
        walkButton.action.Enable();
    }

    private void OnDisable()
    {
        walkButton.action.performed -= OnWalkPressed;
        walkButton.action.Disable();
    }

    private void OnWalkPressed(InputAction.CallbackContext context)
    {
        if (horseFsm != null && !horseFsm.MovementAllowed)
        {
            Debug.Log("[MODE] Walk button ignored — horse is in Static mode.");
            return;
        }
        // Read the FSM's own flag instead of a locally-tracked bool, so a
        // Static-mode force-stop (which clears startHorseWalk internally)
        // can never desync from this button's next press.
        horseFsm.SetStartHorseWalk(!horseFsm.startHorseWalk);
    }
}
