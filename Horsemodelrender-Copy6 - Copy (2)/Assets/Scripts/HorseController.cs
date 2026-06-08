using UnityEngine;
using TMPro;
using System.Collections;

public class HorseFsmDebugController : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("The GameObject that holds the HorseFsm component.")]
    public HorseFsm horseFsm;

    [Header("Debug Display")]
    [Tooltip("TextMeshPro text element that will display the current horse state.")]
    public TMP_Text stateDisplayText;

    private HorseStates lastState;

    // ─────────────────────────────────────────────
    //  State Display
    // ─────────────────────────────────────────────

    void Update()
    {
        UpdateStateDisplay();
    }

    private void UpdateStateDisplay()
    {
        if (stateDisplayText == null || !Validate()) return;

        HorseStates currentState = horseFsm.currState;
        if (currentState == lastState) return;

        lastState = currentState;
        stateDisplayText.text = $"Horse State: {currentState}";
    }

    // ─────────────────────────────────────────────
    //  State triggers — all parameterless for Button OnClick()
    // ─────────────────────────────────────────────

    /// <summary>Clears all trigger flags, returning the FSM to its idle/None path.</summary>
    public void TriggerNone()
    {
        if (!Validate()) return;

        horseFsm.SetHandNearMouth(false);
        horseFsm.SetHandBehindEar(false);
        horseFsm.SetStartHorseWalk(false);
        horseFsm.SetTouchedBehind(false);

        Debug.Log("[HorseDebug] → None (flags cleared)");
    }

    /// <summary>Simulates a hand being held near the horse's mouth to trigger Feeding.</summary>
    public void TriggerFeeding()
    {
        if (!Validate()) return;

        horseFsm.SetHandNearMouth(true);
        Debug.Log("[HorseDebug] → Feeding (handNearMouth = true; call TriggerNone to release)");
    }

    /// <summary>Triggers Anxious state from the right side (sideTouched = 1).</summary>
    public void TriggerAnxiousRight()
    {
        if (!Validate()) return;
        TriggerAnxiousSide(right: true);
    }

    /// <summary>Triggers Anxious state from the left side (sideTouched = 2).</summary>
    public void TriggerAnxiousLeft()
    {
        if (!Validate()) return;
        TriggerAnxiousSide(right: false);
    }

    /// <summary>Default Anxious trigger (right side). Safe for single-button setups.</summary>
    public void TriggerAnxious()
    {
        TriggerAnxiousRight();
    }

    /// <summary>Sets startHorseWalk = true to enter the Walking state.</summary>
    public void TriggerWalking()
    {
        if (!Validate()) return;

        horseFsm.SetStartHorseWalk(true);
        Debug.Log("[HorseDebug] → Walking (startHorseWalk = true; call TriggerNone to stop)");
    }

    // ─────────────────────────────────────────────
    //  Touch / emotion triggers
    // ─────────────────────────────────────────────

    /// <summary>Fires a gentle pet event (+0.05 emotion). Can calm an Anxious horse over time.</summary>
    public void TriggerGentlePet()
    {
        if (!Validate()) return;

        horseFsm.OnGentlePet();
        Debug.Log("[HorseDebug] Gentle pet fired.");
    }

    /// <summary>Fires a harsh touch event (-0.2 emotion, -2s anxious timer).</summary>
    public void TriggerHarshTouch()
    {
        if (!Validate()) return;

        horseFsm.OnHarshTouch();
        Debug.Log("[HorseDebug] Harsh touch fired.");
    }

    /// <summary>Fires a danger touch event (-0.4 emotion, -2s anxious timer).</summary>
    public void TriggerDangerTouch()
    {
        if (!Validate()) return;

        horseFsm.OnDangerTouch();
        Debug.Log("[HorseDebug] Danger touch fired.");
    }

    /// <summary>
    /// Briefly sets touchedBehind = true to trigger a kick, then clears it next frame.
    /// The Anxious state handles the actual kick animation.
    /// </summary>
    public void TriggerKick()
    {
        if (!Validate()) return;

        horseFsm.SetTouchedBehind(true);
        Debug.Log("[HorseDebug] Kick triggered (touchedBehind pulse).");
        StartCoroutine(ClearTouchedBehind());
    }

    // ─────────────────────────────────────────────
    //  Reset
    // ─────────────────────────────────────────────

    /// <summary>
    /// Fully resets the horse to its initial calm state. Stops all coroutines on HorseFsm,
    /// resets all flags and timers, and replays the idle animation from the start.
    /// Safe to call from a Button OnClick().
    /// </summary>
    public void ResetHorse()
    {
        if (!Validate()) return;

        StopAllCoroutines();          // stop any ClearTouchedBehind that might re-set a flag
        horseFsm.ResetToInitialState();

        lastState = HorseStates.None; // sync display without waiting for next Update
        if (stateDisplayText != null)
            stateDisplayText.text = "Horse State: None";

        Debug.Log("[HorseDebug] Horse fully reset to calm/None state.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private void TriggerAnxiousSide(bool right)
    {
        horseFsm.SetSideTouched(right ? 1 : 2);
        horseFsm.SetHandBehindEar(true);
        Debug.Log($"[HorseDebug] → Anxious ({(right ? "right" : "left")} side)");
    }

    private IEnumerator ClearTouchedBehind()
    {
        yield return null; // wait one frame so HorseFsm.Update() sees the flag as true
        horseFsm.SetTouchedBehind(false);
    }

    private bool Validate()
    {
        if (horseFsm != null) return true;

        Debug.LogWarning("[HorseDebug] HorseFsm reference is missing! Assign it in the Inspector.");
        return false;
    }
}