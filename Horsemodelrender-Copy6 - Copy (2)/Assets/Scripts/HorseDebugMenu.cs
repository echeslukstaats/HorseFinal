using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class HorseDebugMenu : MonoBehaviour
{
    [Header("References")]
    public HorseFsm horseFsm;
    public TextMeshProUGUI stateLabel;
    public GameObject menuRoot;

    [Header("Input")]
    public InputActionReference menuToggleAction;

    [Header("Settings")]
    public bool startVisible = true;

    private void Start()
    {
        menuRoot.SetActive(startVisible);

        if (menuToggleAction != null)
        {
            menuToggleAction.action.Enable();
            menuToggleAction.action.performed += OnMenuToggle;
        }
    }

    private void OnDestroy()
    {
        if (menuToggleAction != null)
        {
            menuToggleAction.action.performed -= OnMenuToggle;
        }
    }

    private void OnMenuToggle(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    private void Update()
    {
        if (stateLabel != null)
            stateLabel.text = $"State: {horseFsm.currState}  |  Greeted: {horseFsm.hasGreeted}";
    }

    public void ToggleMenu()
    {
        menuRoot.SetActive(!menuRoot.activeSelf);
    }

    // ── State Triggers ──────────────────────────────────────────

    public void TriggerNone()
    {
        horseFsm.ResetToInitialState();
    }

    public void TriggerFeeding()
    {
        horseFsm.SetHandNearMouth(true);
    }

    public void TriggerAnxious()
    {
        horseFsm.SetSideTouched(1);
        horseFsm.SetHandBehindEar(true);
    }

    public void TriggerWalking()
    {
        horseFsm.SetStartHorseWalk(true);
    }

    // ── Emotion / Touch Events ───────────────────────────────────

    public void TriggerGentlePet()
    {
        horseFsm.OnGentlePet();
    }

    public void TriggerHarshTouch()
    {
        horseFsm.OnHarshTouch();
    }

    public void TriggerDangerTouch()
    {
        horseFsm.OnDangerTouch();
    }

    public void TriggerKick()
    {
        horseFsm.SetTouchedBehind(true);
    }

    public void ResetGreeted()
    {
        horseFsm.hasGreeted = false;
    }

    public void ToggleSide(TextMeshProUGUI label)
    {
        int current = horseFsm.detectSideTouched;
        int next = (current == 1) ? 2 : 1;
        horseFsm.SetSideTouched(next);
        label.text = $"Side: {(next == 1 ? "Right" : "Left")}";
    }

    // ── Reset ────────────────────────────────────────────────────

    public void ResetHorse()
    {
        horseFsm.ResetToInitialState();
    }
}