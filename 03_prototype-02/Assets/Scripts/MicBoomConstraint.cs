using UnityEngine;

// Keeps a mic model tethered to a fixed mount point on the panel. The head has a
// normal XRGrabInteractable on it, which handles grab detection and its own default
// follow-the-hand movement; this script just clamps the head back onto a sphere
// around the mount every frame (LateUpdate, so it runs after XRI moves the head),
// so it can be pulled toward or pushed away from the panel but never fully detaches.
// The arm segment rotates to visually track wherever the head ends up.
//
// This is a scripted constraint, not a physics joint - simpler to reason about,
// nothing to tune (spring/damper values, breakForce, etc.), stable for desktop
// testing with the XR Device Simulator.
public class MicBoomConstraint : MonoBehaviour
{
    [Header("References")]
    public Transform mount;      // fixed pivot point on the panel - this object itself, usually
    public Transform arm;        // visual arm segment (a pivot; rotates to point at the head)
    public Transform head;       // the grabbable end - has the XRGrabInteractable

    [Header("Reach limits, in metres from the mount")]
    public float minDistance = 0.05f;
    public float maxDistance = 0.14f;

    void LateUpdate()
    {
        if (mount == null || head == null) return;

        Vector3 toHead = head.position - mount.position;
        float distance = toHead.magnitude;

        if (distance > 0.0001f)
        {
            float clamped = Mathf.Clamp(distance, minDistance, maxDistance);
            if (!Mathf.Approximately(clamped, distance))
            {
                Vector3 direction = toHead / distance;
                head.position = mount.position + direction * clamped;
            }
        }
        else
        {
            // Degenerate case (head sitting exactly on the mount) - push it back out
            // along the mount's own forward so it doesn't just disappear.
            head.position = mount.position + mount.forward * minDistance;
        }

        if (arm != null)
        {
            Vector3 armDirection = head.position - mount.position;
            if (armDirection.sqrMagnitude > 0.0001f)
                arm.rotation = Quaternion.LookRotation(armDirection.normalized, Vector3.up);
        }
    }
}
