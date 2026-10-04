using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Detects a "clap": both hands closing fast and ending up close together.
// Assign the two controller/hand transforms from the XR rig in the Inspector.
// In the Editor, pressing N also counts as a clap, since the XR Interaction Simulator
// can only move one controller at a time. (N is not used by the simulator's controls.)
public class ClapGestureDetector : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    public float clapDistanceThreshold = 0.12f;
    public float minClosingSpeed = 0.6f;   // metres/second the hands must be closing at
    public float cooldownSeconds = 1f;

    public UnityEvent onClap;

    float previousDistance;
    float cooldownTimer;
    bool hasPreviousDistance;

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            onClap.Invoke();
#endif

        if (leftHand == null || rightHand == null) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        float distance = Vector3.Distance(leftHand.position, rightHand.position);

        if (hasPreviousDistance)
        {
            float closingSpeed = (previousDistance - distance) / Time.deltaTime;

            if (cooldownTimer <= 0f
                && distance <= clapDistanceThreshold
                && closingSpeed >= minClosingSpeed)
            {
                cooldownTimer = cooldownSeconds;
                onClap.Invoke();
            }
        }

        previousDistance = distance;
        hasPreviousDistance = true;
    }
}
