using UnityEngine;
using UnityEngine.UI;

// Single wrist-mounted toggle button: no label, just a colour swap between
// Static (green) and Dynamic (orange). Parent the GameObject holding this
// script under the hand/wrist anchor in the scene hierarchy.
[RequireComponent(typeof(Image))]
public class WristModeButton : MonoBehaviour
{
    public HorseModeManager modeManager;

    [Header("Colours")]
    public Color staticColor = new Color(0.20f, 0.70f, 0.20f);  // green
    public Color dynamicColor = new Color(0.90f, 0.50f, 0.10f); // orange

    private Image image;
    private Button button;

    private void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (modeManager == null)
        {
            Debug.LogWarning("[MODE] WristModeButton has no HorseModeManager assigned.");
            return;
        }

        modeManager.OnModeChanged += UpdateColor;
        if (button != null) button.onClick.AddListener(modeManager.ToggleMode);

        // Reflect whatever mode is already active — covers this button
        // enabling after HorseModeManager.Start() has already applied one.
        UpdateColor(modeManager.CurrentMode);
    }

    private void OnDisable()
    {
        if (modeManager == null) return;

        modeManager.OnModeChanged -= UpdateColor;
        if (button != null) button.onClick.RemoveListener(modeManager.ToggleMode);
    }

    private void UpdateColor(HorseFsm.InteractionMode mode)
    {
        image.color = mode == HorseFsm.InteractionMode.Static ? staticColor : dynamicColor;
    }
}