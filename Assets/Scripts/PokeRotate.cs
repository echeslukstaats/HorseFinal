using UnityEngine;
using Oculus.Interaction;

public class PokeRotate : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PokeInteractor>() != null)
        {
            transform.Rotate(0f, 45f, 0f);
        }
    }
}