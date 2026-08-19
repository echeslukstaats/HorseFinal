using UnityEngine;

// Single entry point for switching between Static and Dynamic interaction
// mode. Persists the player's choice across sessions and applies it to
// HorseFsm on startup. Visual layers (HUD badge, etc.) should subscribe to
// OnModeChanged rather than polling HorseFsm.interactionMode directly.
public class HorseModeManager : MonoBehaviour
{
    private const string PrefsKey = "HorseInteractionMode";

    [Header("References")]
    public HorseFsm horseFsm;

    [Header("Default")]
    [Tooltip("Used only on first-ever launch, before any preference has been saved.")]
    public HorseFsm.InteractionMode defaultMode = HorseFsm.InteractionMode.Static;

    public HorseFsm.InteractionMode CurrentMode => horseFsm != null
        ? horseFsm.interactionMode
        : defaultMode;

    public event System.Action<HorseFsm.InteractionMode> OnModeChanged;

     private void Start()
    {
        if (horseFsm == null)
        {
            Debug.LogWarning("[MODE] HorseModeManager has no HorseFsm reference assigned.");
            return;
        }

        HorseFsm.InteractionMode startupMode = LoadSavedMode();
        ApplyMode(startupMode, persist: false); // already the persisted value, no need to re-save
    }

    public void SetStaticMode() => ApplyMode(HorseFsm.InteractionMode.Static, persist: true);

    public void SetDynamicMode() => ApplyMode(HorseFsm.InteractionMode.Dynamic, persist: true);

    public void SetMode(HorseFsm.InteractionMode newMode) => ApplyMode(newMode, persist: true);

    private void ApplyMode(HorseFsm.InteractionMode mode, bool persist)
    {
        if (horseFsm == null) return;

        horseFsm.SetInteractionMode(mode);

        if (persist)
            SaveMode(mode);

        Debug.Log($"[MODE] Applied {mode} (persisted={persist}).");
        OnModeChanged?.Invoke(mode);
    }

    private HorseFsm.InteractionMode LoadSavedMode()
    {
        // PlayerPrefs has no enum support; store the enum name as a string so
        // the saved value stays readable in the prefs file and resilient to
        // enum reordering (unlike storing the raw int index would be).
        string saved = PlayerPrefs.GetString(PrefsKey, defaultMode.ToString());

        if (System.Enum.TryParse(saved, out HorseFsm.InteractionMode parsed))
            return parsed;

        Debug.LogWarning($"[MODE] Could not parse saved mode '{saved}', falling back to default ({defaultMode}).");
        return defaultMode;
    }

    private void SaveMode(HorseFsm.InteractionMode mode)
    {
        PlayerPrefs.SetString(PrefsKey, mode.ToString());
        PlayerPrefs.Save();
    }
}