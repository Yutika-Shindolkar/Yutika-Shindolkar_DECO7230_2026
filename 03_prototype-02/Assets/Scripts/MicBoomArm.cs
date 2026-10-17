using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// The mic clamped to the creation panel's edge. The clamp and the short horizontal arm are
// fixed; only the two joints move, like a boom mic on the side of a desk:
//   - the elbow tilts the upright arm (and the head with it) closer to or further from the panel
//   - the head joint turns the mic itself
// Grab the arm or the head and move the hand; the joint follows, within a limited swing.
// Built by IP2aSceneBuilder.BuildMic.
public class MicBoomArm : MonoBehaviour
{
    public BoomJoint elbow = new BoomJoint();
    public BoomJoint head = new BoomJoint();

    [System.Serializable]
    public class BoomJoint
    {
        public Transform pivot;                 // turns about its own origin
        public XRSimpleInteractable handle;     // what the player grabs
        public float maxSwing = 40f;            // degrees away from the rest pose

        [System.NonSerialized] public bool registered;   // false until the mic has been switched on once - it is still at rest then
        [System.NonSerialized] public Quaternion restRotation;
        [System.NonSerialized] public IXRSelectInteractor holder;
        [System.NonSerialized] public Quaternion startRotation;
        [System.NonSerialized] public Vector3 startDirection;
        [System.NonSerialized] public float rayDistance;
    }

    void Awake()
    {
        Register(elbow);
        Register(head);
    }

    void Register(BoomJoint joint)
    {
        if (joint.pivot == null || joint.handle == null) return;
        joint.registered = true;
        joint.restRotation = joint.pivot.localRotation;
        joint.handle.selectEntered.AddListener(args => Begin(joint, args.interactorObject));
        joint.handle.selectExited.AddListener(args => joint.holder = null);
    }

    // Back to the pose the mic was built in.
    public void ResetPose()
    {
        ResetJoint(elbow);
        ResetJoint(head);
    }

    static void ResetJoint(BoomJoint joint)
    {
        if (joint.registered && joint.pivot != null) joint.pivot.localRotation = joint.restRotation;
    }

    void Begin(BoomJoint joint, IXRSelectInteractor interactor)
    {
        joint.holder = interactor;
        joint.startRotation = joint.pivot.localRotation;
        Transform origin = RayOrigin(interactor);
        if (origin != null)
        {
            // Where the ray hit the arm: keep that distance along the ray as it swings.
            Vector3 hit = joint.handle.transform.position;
            joint.rayDistance = Mathf.Max(0.05f, Vector3.Dot(hit - origin.position, origin.forward));
        }
        joint.startDirection = PullPoint(joint) - joint.pivot.position;
    }

    void LateUpdate()
    {
        Follow(elbow);
        Follow(head);
    }

    void Follow(BoomJoint joint)
    {
        if (joint.holder == null || joint.pivot == null) return;

        Vector3 now = PullPoint(joint) - joint.pivot.position;
        if (now.sqrMagnitude < 1e-6f || joint.startDirection.sqrMagnitude < 1e-6f) return;

        // Turn the joint by however far the hand has swung round it since the grab.
        Transform parent = joint.pivot.parent;
        Quaternion swing = Quaternion.FromToRotation(joint.startDirection, now);
        Quaternion wanted = swing * (parent.rotation * joint.startRotation);
        Quaternion local = Quaternion.Inverse(parent.rotation) * wanted;
        joint.pivot.localRotation = Quaternion.RotateTowards(joint.restRotation, local, joint.maxSwing);
    }

    static Transform RayOrigin(IXRSelectInteractor interactor) =>
        interactor is IXRRayProvider ray && interactor is Component c && c != null ? ray.GetOrCreateRayOrigin() : null;

    static Vector3 PullPointOf(IXRSelectInteractor interactor, float rayDistance, Vector3 fallback)
    {
        Transform origin = RayOrigin(interactor);
        if (origin != null) return origin.position + origin.forward * rayDistance;
        return interactor is Component hand && hand != null ? hand.transform.position : fallback;
    }

    Vector3 PullPoint(BoomJoint joint) =>
        PullPointOf(joint.holder, joint.rayDistance, joint.handle.transform.position);
}

// Sits on a joint's grab area so XRButtonPressRouter doesn't "click" it with a one-shot
// trigger press - turning a joint needs grip held down (see IPressAndHold).
public class MicJointHandle : MonoBehaviour, IPressAndHold { }
