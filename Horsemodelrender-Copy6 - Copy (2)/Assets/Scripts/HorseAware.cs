using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;
public class HorseAware : MonoBehaviour
{
    public Transform userBody;
    public Transform headTarget;
    public MultiAimConstraint headMac;
    public Transform head;
    public Transform body;
    public Animator animator;

    private bool isAcknowledged = false;
    public bool isLooking = false;

    public void AttemptLook()
    {
        if (isAcknowledged || isLooking || userBody == null) return;

        if (IsVisible())
        {
            animator.Play("pauseIdle_baked2 0");
            Debug.Log("IS VISIBLE");
            StartCoroutine(Look());
            isAcknowledged=true ;
        }
        else
        {
            Debug.Log("IS NOT VISIBLE");
        }
    }

    bool IsVisible()
    {
        Vector3 direction = (userBody.position - head.position).normalized;
        float headDot = Vector3.Dot(-head.right, direction);
        float bodyDot = Vector3.Dot(-body.right, direction);
        return headDot > 0.01f && bodyDot > 0f;
    }

    IEnumerator Look()
    {
        isLooking = true;
        float duration = 3f;
        float elapsed = 0f;
        Vector3 startPos = headTarget.position;
        Vector3 targetPos = userBody.position;

        targetPos.z = Mathf.Lerp(startPos.z, targetPos.z, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            headMac.weight = Mathf.Lerp(headMac.weight, 1f, Time.deltaTime * 8f);
            headTarget.position = Vector3.Lerp(headTarget.position, userBody.position, Time.deltaTime * 1f);


            yield return null;
        }
        headTarget.position =targetPos;
        headMac.weight = 1f;

        yield return new WaitForSeconds(1.5f);

        elapsed = 0f;
        Vector3 returnPos = startPos;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            headTarget.position = Vector3.Lerp(targetPos, returnPos, Time.deltaTime * 1f);
            headMac.weight = Mathf.Lerp(headMac.weight, 0f, t);
            animator.Play("idle1_Baked");

            yield return null;
        }
        isLooking=false;
    }
}
