using UnityEngine;
using UnityEngine.Events;

public class FingerTouchTrigger : MonoBehaviour
{
    public UnityEvent onFingerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            onFingerEnter?.Invoke();
        }
    }
}

