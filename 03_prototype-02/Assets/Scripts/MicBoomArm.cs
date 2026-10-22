using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// The mic clamped to the creation panel's edge. The clamp and the short horizontal arm are
// fixed; only the two joints move, like a boom mic on the side of a desk:
//   - the elbow tilts the upright arm (and the head with it) closer to or further from the panel
//   - the head joint turns the mic itself
// Point at the arm or the head (it glows), hold grip and move the hand; the joint follows,
// within a limited swing. In the Editor and development builds the mouse does the same:
// hover to glow, hold the left button and drag.
// Built by IP2aSceneBuilder.BuildMic.
public class MicBoomArm : MonoBehaviour
{
    public BoomJoint elbow = new BoomJoint();
    public BoomJoint head = new BoomJoint();

    [Header("Glow outline")]
    public Color glowDim = new Color(1f, 0.75f, 0.2f);
    public Color glowBright = new Color(1f, 0.95f, 0.6f);
    public float glowPulseSpeed = 6f;

    [System.Serializable]
    public class BoomJoint
    {
        public Transform pivot;                 // turns about its own origin
        public XRSimpleInteractable handle;     // what the player grabs
        public Renderer outline;                // glow around this joint's parts, shown while pointed at or held
        public float maxSwing = 40f;            // degrees away from the rest pose

        [System.NonSerialized] public bool registered;   // false until the mic has been switched on once - it is still at rest then
        [System.NonSerialized] public Quaternion restRotation;
        [System.NonSerialized] public bool hovered;       // a controller ray is on it
        [System.NonSerialized] public bool mouseHovered;
        [System.NonSerialized] public IXRSelectInteractor holder;
        [System.NonSerialized] public bool mouseHeld;
        [System.NonSerialized] public Quaternion startRotation;
        [System.NonSerialized] public Vector3 startDirection;
        [System.NonSerialized] public float rayDistance;

        public bool IsHeld => holder != null || mouseHeld;
        public bool IsPointedAt => hovered || mouseHovered;
    }

    const float MinLever = 0.05f; // metres: grabbing right on a pivot would make the joint twitch

    MaterialPropertyBlock block;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        Register(elbow);
        Register(head);
    }

    void Register(BoomJoint joint)
    {
        if (joint.pivot == null || joint.handle == null) return;
        joint.registered = true;
        joint.restRotation = joint.pivot.localRotation;
        joint.handle.hoverEntered.AddListener(args => joint.hovered = true);
        joint.handle.hoverExited.AddListener(args => joint.hovered = false);
        joint.handle.selectEntered.AddListener(args => BeginHeld(joint, args.interactorObject));
        joint.handle.selectExited.AddListener(args => joint.holder = null);
        if (joint.outline != null) joint.outline.enabled = false;
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

    // ---- Controller ray (grip held) ----

    void BeginHeld(BoomJoint joint, IXRSelectInteractor interactor)
    {
        joint.holder = interactor;
        if (TryGetAim(joint, out Ray aim)) Begin(joint, aim);
    }

    static Transform RayOrigin(IXRSelectInteractor interactor) =>
        interactor is IXRRayProvider ray && interactor is Component c && c != null ? ray.GetOrCreateRayOrigin() : null;

    // The ray the joint's holder is aiming along: the mouse cursor, a controller ray, or from
    // a hand that has no ray toward the joint.
    bool TryGetAim(BoomJoint joint, out Ray aim)
    {
        aim = default;
        if (joint.mouseHeld)
        {
            Camera cam = Camera.main;
            Mouse mouse = Mouse.current;
            if (cam == null || mouse == null) return false;
            aim = cam.ScreenPointToRay(mouse.position.ReadValue());
            return true;
        }
        if (joint.holder == null) return false;
        Transform origin = RayOrigin(joint.holder);
        if (origin != null) { aim = new Ray(origin.position, origin.forward); return true; }
        if (joint.holder is Component hand && hand != null)
        {
            aim = new Ray(hand.transform.position, joint.handle.transform.position - hand.transform.position);
            return true;
        }
        return false;
    }

    // ---- Start and follow a drag along an aim ray ----

    // Public so a test (or any other pointer) can drive a joint without a controller.
    public void PointerDown(BoomJoint joint, Ray aim, bool mouse)
    {
        if (joint.pivot == null) return;
        joint.mouseHeld = mouse;
        Begin(joint, aim);
    }

    public void PointerUp(BoomJoint joint) => joint.mouseHeld = false;

    // Turns the joint by however far the aim has swung round it since PointerDown.
    public void PointerDrag(BoomJoint joint, Ray aim) => Follow(joint, aim);

    void Begin(BoomJoint joint, Ray aim)
    {
        joint.startRotation = joint.pivot.localRotation;

        // Where the aim hit the joint's grab area (its centre if it somehow missed).
        Vector3 grabPoint = joint.handle.transform.position;
        float best = float.MaxValue;
        foreach (Collider col in joint.handle.colliders)
            if (col != null && col.Raycast(aim, out RaycastHit hit, 20f) && hit.distance < best)
            {
                best = hit.distance;
                grabPoint = hit.point;
            }
        joint.rayDistance = Mathf.Max(0.05f, Vector3.Dot(grabPoint - aim.origin, aim.direction));

        Vector3 fromPivot = grabPoint - joint.pivot.position;
        if (fromPivot.magnitude < MinLever)
        {
            Vector3 along = joint.handle.transform.position - joint.pivot.position;
            if (along.sqrMagnitude < 1e-8f) along = aim.direction;
            fromPivot = along.normalized * MinLever;
        }
        joint.startDirection = fromPivot;
    }

    void Follow(BoomJoint joint, Ray aim)
    {
        Vector3 now = aim.origin + aim.direction * joint.rayDistance - joint.pivot.position;
        if (now.sqrMagnitude < 1e-6f || joint.startDirection.sqrMagnitude < 1e-6f) return;

        Transform parent = joint.pivot.parent;
        Quaternion swing = Quaternion.FromToRotation(joint.startDirection, now);
        Quaternion wanted = swing * (parent.rotation * joint.startRotation);
        Quaternion local = Quaternion.Inverse(parent.rotation) * wanted;
        joint.pivot.localRotation = Quaternion.RotateTowards(joint.restRotation, local, joint.maxSwing);
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UpdateMouse();
#endif
    }

    void LateUpdate()
    {
        UpdateJoint(elbow);
        UpdateJoint(head);
    }

    void UpdateJoint(BoomJoint joint)
    {
        if (joint.pivot == null) return;
        if (joint.IsHeld && TryGetAim(joint, out Ray aim)) Follow(joint, aim);

        // Glow while pointed at, brighter and pulsing while held.
        if (joint.outline == null) return;
        bool show = joint.IsPointedAt || joint.IsHeld;
        joint.outline.enabled = show;
        if (!show) return;
        float t = joint.IsHeld ? 0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed) : 0.35f;
        joint.outline.GetPropertyBlock(block);
        block.SetColor("_BaseColor", Color.Lerp(glowDim, glowBright, t));
        joint.outline.SetPropertyBlock(block);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Editor / development builds only: the mouse as a pointer, for testing without a
    // headset. Hover glows the joint under the cursor; hold the left button and drag.
    void UpdateMouse()
    {
        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null) return;

        if (elbow.mouseHeld || head.mouseHeld)
        {
            if (!mouse.leftButton.isPressed) { elbow.mouseHeld = false; head.mouseHeld = false; }
            return;
        }

        Ray aim = cam.ScreenPointToRay(mouse.position.ReadValue());
        BoomJoint under = JointUnder(aim);
        elbow.mouseHovered = under == elbow;
        head.mouseHovered = under == head;
        if (under != null && mouse.leftButton.wasPressedThisFrame) PointerDown(under, aim, true);
    }
#endif

    // The joint whose grab area the ray reaches first (the head wins where they overlap).
    BoomJoint JointUnder(Ray aim)
    {
        BoomJoint best = null;
        float bestDistance = float.MaxValue;
        foreach (BoomJoint joint in new[] { head, elbow })
        {
            if (joint.handle == null) continue;
            foreach (Collider col in joint.handle.colliders)
                if (col != null && col.enabled && col.Raycast(aim, out RaycastHit hit, 20f) && hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = joint;
                }
        }
        return best;
    }
}

// Sits on a joint's grab area so XRButtonPressRouter doesn't "click" it with a one-shot
// trigger press - turning a joint needs grip held down (see IPressAndHold).
public class MicJointHandle : MonoBehaviour, IPressAndHold { }
