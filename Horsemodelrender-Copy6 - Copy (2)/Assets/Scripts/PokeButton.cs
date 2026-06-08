using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;

[RequireComponent(typeof(PokeInteractable))]
public class PokeButton : MonoBehaviour
{
    private PokeInteractable pokeInteractable;
    private Button button;
    private int prevSelectingCount = 0;

    private void Awake()
    {
        pokeInteractable = GetComponent<PokeInteractable>();
        button = GetComponent<Button>();
    }
    public void InvokeClick()
    {
        button?.onClick.Invoke();
    }
    private void Update()
    {
        int currentCount = 0;
        foreach (var _ in pokeInteractable.SelectingInteractorViews)
        {
            currentCount++;
        }

        // Fire on first frame of contact only
        if (currentCount > 0 && prevSelectingCount == 0)
        {
            Debug.Log("Button poked: " + gameObject.name);
            button?.onClick.Invoke();
        }

        prevSelectingCount = currentCount;
    }
}