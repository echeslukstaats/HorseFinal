using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a GameObject that represents one tutorial step.
/// The TutorialManager drives visibility; this component just holds
/// the configuration and exposes the completion flag.
/// </summary>
public class TutorialStep : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("The text component shown for this step. Defaults to TMP_Text on this GameObject.")]
    public TMP_Text label;

    [Header("Content")]
    [TextArea(2, 5)]
    public string instructionText = "Do something...";

    [Header("Completion")]
    [Tooltip("If true, this step watches HorseFsm for the condition below instead of waiting for a manual ReportStepComplete() call.")]
    public bool autoDetect = false;

    public enum HorseCondition
    {
        None,
        HasKicked,
        HasGreeted,
        IsFeeding,
        IsAnxious,
        IsWalking,
        IsCalm,
    }

    [Tooltip("Which HorseFsm condition marks this step complete (only used when autoDetect = true).")]
    public HorseCondition completionCondition = HorseCondition.None;

    [Tooltip("Reference to the HorseFsm. Required only when autoDetect = true.")]
    public HorseFsm horseFsm;

    // ── Runtime state ──────────────────────────────────────────────
    public bool IsComplete { get; private set; } = false;

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (!autoDetect || IsComplete || horseFsm == null) return;

        if (EvaluateCondition())
            ReportStepComplete();
    }

    /// <summary>
    /// Call this from any external script (collision trigger, animation event,
    /// debug button, etc.) to mark the step done and let the TutorialManager advance.
    /// </summary>
    public void ReportStepComplete()
    {
        if (IsComplete) return;
        IsComplete = true;
        Debug.Log($"[TutorialStep] '{instructionText}' — completed.");
    }

    /// <summary>Resets completion state. Called by TutorialManager on full restart.</summary>
    public void ResetStep()
    {
        IsComplete = false;
    }

    // ── Private helpers ────────────────────────────────────────────

    private bool EvaluateCondition()
    {
        return completionCondition switch
        {
            HorseCondition.HasKicked => horseFsm.hasKicked,
            HorseCondition.HasGreeted => horseFsm.hasGreeted,
            HorseCondition.IsFeeding => horseFsm.currState == HorseStates.Feeding,
            HorseCondition.IsAnxious => horseFsm.currState == HorseStates.Anxious,
            HorseCondition.IsWalking => horseFsm.currState == HorseStates.Walking,
            HorseCondition.IsCalm => horseFsm.currState == HorseStates.None,
            _ => false,
        };
    }
}