using UnityEngine;
using UnityEngine.InputSystem;
public class TriggerHorseWalk : MonoBehaviour
{

    public HorseFsm horseFsm;
    public InputActionReference walkButton;

    private bool isWalking = false;

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
        isWalking = !isWalking;

            horseFsm.SetStartHorseWalk(isWalking);

    }
}
