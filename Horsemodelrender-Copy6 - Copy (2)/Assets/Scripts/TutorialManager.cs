using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Drives an ordered sequence of TutorialSteps.
///
/// Setup:
///   1. Create a child GameObject per tutorial step.
///   2. Add a TMP_Text and a TutorialStep component to each.
///   3. Drag them into the `steps` list in order.
///   4. Either enable autoDetect on each TutorialStep, or call
///      TutorialStep.ReportStepComplete() from your own game logic.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Steps (ordered)")]
    public List<TutorialStep> steps = new();

    [Header("Display Options")]
    [Tooltip("Seconds to wait after a step completes before showing the next one.")]
    public float stepTransitionDelay = 0.5f;

    [Tooltip("Fade duration in seconds (0 = instant).")]
    public float fadeDuration = 0.3f;

    [Header("Completion")]
    [Tooltip("Optional GameObject shown when all steps are finished.")]
    public GameObject completionMessage;

    // ── Runtime ────────────────────────────────────────────────────
    public int CurrentStepIndex { get; private set; } = -1;
    public bool IsFinished { get; private set; } = false;

    private Coroutine _watchCoroutine;

    // ── Lifecycle ──────────────────────────────────────────────────

    private void Start()
    {
        HideAll();
        if (completionMessage != null)
            completionMessage.SetActive(false);

        if (steps.Count > 0)
            StartTutorial();
    }

    // ── Public API ─────────────────────────────────────────────────

    /// <summary>Begins the tutorial from step 0.</summary>
    public void StartTutorial()
    {
        IsFinished = false;
        CurrentStepIndex = -1;
        AdvanceToStep(0);
    }

    /// <summary>
    /// Restarts the tutorial from the beginning and resets all step completion flags.
    /// Wire this to your reset/replay button.
    /// </summary>
    public void RestartTutorial()
    {
        if (_watchCoroutine != null)
        {
            StopCoroutine(_watchCoroutine);
            _watchCoroutine = null;
        }

        foreach (var step in steps)
            step.ResetStep();

        HideAll();

        if (completionMessage != null)
            completionMessage.SetActive(false);

        IsFinished = false;
        CurrentStepIndex = -1;

        Debug.Log("[TutorialManager] Restarted.");
        AdvanceToStep(0);
    }

    /// <summary>
    /// Manually completes the current step and advances.
    /// Useful for skipping or debug buttons.
    /// </summary>
    public void CompleteCurrentStep()
    {
        if (IsFinished || CurrentStepIndex < 0 || CurrentStepIndex >= steps.Count) return;
        steps[CurrentStepIndex].ReportStepComplete();
    }

    /// <summary>Skips directly to a specific step index (0-based). Useful for debug.</summary>
    public void SkipToStep(int index)
    {
        if (index < 0 || index >= steps.Count) return;

        if (_watchCoroutine != null)
        {
            StopCoroutine(_watchCoroutine);
            _watchCoroutine = null;
        }

        HideAll();
        AdvanceToStep(index);
    }

    // ── Private logic ──────────────────────────────────────────────

    private void AdvanceToStep(int index)
    {
        if (index >= steps.Count)
        {
            FinishTutorial();
            return;
        }

        CurrentStepIndex = index;
        TutorialStep step = steps[index];

        // Apply instruction text if the label exists
        if (step.label != null)
            step.label.text = step.instructionText;

        ShowStep(step);

        _watchCoroutine = StartCoroutine(WatchForCompletion(step));
        Debug.Log($"[TutorialManager] Step {index + 1}/{steps.Count}: {step.instructionText}");
    }

    /// <summary>
    /// Polls the current step each frame until it reports complete,
    /// then triggers the transition to the next step.
    /// </summary>
    private IEnumerator WatchForCompletion(TutorialStep step)
    {
        yield return new WaitUntil(() => step.IsComplete);

        yield return new WaitForSeconds(stepTransitionDelay);

        yield return StartCoroutine(FadeOutStep(step));

        AdvanceToStep(CurrentStepIndex + 1);
    }

    private void FinishTutorial()
    {
        IsFinished = true;

        if (completionMessage != null)
            completionMessage.SetActive(true);

        Debug.Log("[TutorialManager] Tutorial complete!");
    }

    // ── Show / Hide ────────────────────────────────────────────────

    private void HideAll()
    {
        foreach (var step in steps)
            SetStepVisible(step, false, instant: true);
    }

    private void ShowStep(TutorialStep step)
    {
        SetStepVisible(step, true, instant: fadeDuration <= 0f);

        if (fadeDuration > 0f)
            StartCoroutine(FadeInStep(step));
    }

    private void SetStepVisible(TutorialStep step, bool visible, bool instant = false)
    {
        if (step.label == null) return;

        if (instant)
        {
            var c = step.label.color;
            step.label.color = new Color(c.r, c.g, c.b, visible ? 1f : 0f);
            step.label.enabled = visible;
            return;
        }

        step.label.enabled = true;
    }

    private IEnumerator FadeInStep(TutorialStep step)
    {
        if (step.label == null) yield break;

        step.label.enabled = true;
        float elapsed = 0f;
        Color c = step.label.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            step.label.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        step.label.color = new Color(c.r, c.g, c.b, 1f);
    }

    private IEnumerator FadeOutStep(TutorialStep step)
    {
        if (step.label == null || fadeDuration <= 0f)
        {
            SetStepVisible(step, false, instant: true);
            yield break;
        }

        float elapsed = 0f;
        Color c = step.label.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            step.label.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        SetStepVisible(step, false, instant: true);
    }
}