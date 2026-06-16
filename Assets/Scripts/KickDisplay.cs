using UnityEngine;
using TMPro;

public class HorseKickDisplay : MonoBehaviour
{
    public HorseFsm horseFsm;
    private TextMeshProUGUI label;

    private void Start()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (horseFsm.hasKicked)
        {
            label.text = "Has kicked: True";
            label.color = Color.green;
        }
        else
        {
            label.text = "Has kicked: False";
            label.color = Color.red;
        }
    }
}
