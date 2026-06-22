using System.Collections.Generic;
using UnityEngine;

public class HorsePettingScript : MonoBehaviour
{
    public HorseFsm horseFsm;

    public enum PetZoneType
    {
        GoodPet,
        DangerZone
    }

    public PetZoneType petZoneType;

    private struct HandSample
    {
        public Vector3 pos;
        public float time;
    }

    private Dictionary<Transform, Queue<HandSample>> handHistory = new Dictionary<Transform, Queue<HandSample>>();
    private Dictionary<Transform, float> petTimes = new Dictionary<Transform, float>();
    private Dictionary<Transform, float> badPetTimers = new Dictionary<Transform, float>();

    // Tracks which hands are currently inside this collider so we can
    // correctly call NotifyBodyTouch(false) on exit without double-counting.
    private HashSet<Transform> handsInside = new HashSet<Transform>();

    private float windowTime = 0.6f;
    private float badPetThreshold = 0.5f;

    private float dangerCooldown = 0f;
    private float dangerCooldownTime = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("TrackedHand")) return;

        Transform hand = other.transform;

        if (handsInside.Add(hand))
        {
            horseFsm.NotifyZoneEnter(HorseFsm.BodyZone.Body);
            horseFsm.NotifyBodyTouch(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Run in any state — gentle petting in None/Feeding builds trust,
        // anxious handling still works as before.
        if (!other.CompareTag("TrackedHand")) return;

        Transform hand = other.transform;

        // Catch any hand that entered without firing OnTriggerEnter
        // (e.g. hand spawned inside the collider).
        if (handsInside.Add(hand))
        {
            horseFsm.NotifyZoneEnter(HorseFsm.BodyZone.Body);
            horseFsm.NotifyBodyTouch(true);
        }

        if (!handHistory.ContainsKey(hand))
        {
            handHistory[hand] = new Queue<HandSample>();
            petTimes[hand] = 0f;
            badPetTimers[hand] = 0f;
        }

        Queue<HandSample> history = handHistory[hand];

        history.Enqueue(new HandSample
        {
            pos = hand.position,
            time = Time.time
        });

        while (history.Count > 0 && Time.time - history.Peek().time > windowTime)
            history.Dequeue();

        float speed = CalculateSpeed(history, hand.position);

        HandleTouch(hand, speed);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("TrackedHand")) return;

        Transform hand = other.transform;

        if (handsInside.Remove(hand))
            horseFsm.NotifyBodyTouch(false);

        handHistory.Remove(hand);
        petTimes.Remove(hand);
        badPetTimers.Remove(hand);
    }

    private float CalculateSpeed(Queue<HandSample> history, Vector3 currentPos)
    {
        if (history.Count < 2) return 0f;

        HandSample oldest = history.Peek();
        float distance = Vector3.Distance(currentPos, oldest.pos);
        float time = Time.time - oldest.time;

        if (time <= 0f) return 0f;

        return distance / time;
    }

    private void HandleTouch(Transform hand, float speed)
    {
        float gentleMin = 0.05f;
        float gentleMax = 0.5f;
        float harshMin = 1f;
        float minPetTime = 0.5f;

        if (!petTimes.ContainsKey(hand)) petTimes[hand] = 0f;
        if (!badPetTimers.ContainsKey(hand)) badPetTimers[hand] = 0f;

        if (petZoneType == PetZoneType.GoodPet)
        {
            if (speed > harshMin)
            {
                badPetTimers[hand] += Time.deltaTime;

                if (badPetTimers[hand] >= badPetThreshold)
                {
                    // Only penalise emotion if the horse is already anxious —
                    // a harsh touch during None state doesn't feel bad enough
                    // to warrant a negative reaction yet.
                    if (horseFsm.currState == HorseStates.Anxious)
                        horseFsm.OnHarshTouch();

                    badPetTimers[hand] = 0f;
                }

                return;
            }

            if (speed > gentleMin && speed < gentleMax)
            {
                petTimes[hand] += Time.deltaTime;

                if (petTimes[hand] >= minPetTime)
                {
                    horseFsm.OnGentlePet();
                    petTimes[hand] = Mathf.Max(0f, petTimes[hand] - Time.deltaTime);
                }
            }
            else
            {
                petTimes[hand] = Mathf.Max(0f, petTimes[hand] - Time.deltaTime * 0.5f);
            }
        }
        else if (petZoneType == PetZoneType.DangerZone)
        {
            dangerCooldown -= Time.deltaTime;

            if (speed > gentleMax && dangerCooldown <= 0f)
            {
                horseFsm.OnDangerTouch();
                dangerCooldown = dangerCooldownTime;
            }
        }
    }
}