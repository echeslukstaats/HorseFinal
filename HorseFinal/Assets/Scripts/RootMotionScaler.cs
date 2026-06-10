using UnityEngine;

public class RootMotionScaler : MonoBehaviour
{
    public float scale = 4f; 
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorMove()
    {
        float info = animator.GetLayerWeight(3);

        if (info >= 1)
        {
            transform.position += animator.deltaPosition * scale;
            transform.rotation *= animator.deltaRotation;
        }
    }
}