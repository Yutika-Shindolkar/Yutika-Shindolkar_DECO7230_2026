using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Decides whether a hand has made the "pull a drawer" gesture: the hand is held out in front
// of the body with grip pressed over empty space, then pulled back toward the body. It is
// plain logic so it can be tested without a headset.
public class DrawerPullDetector
{
    public float minReach = 0.3f;      // metres ahead of the head the hand must be when grip is pressed
    public float pullDistance = 0.25f; // metres the hand must come back from its furthest point

    bool wasHeld;
    bool armed;
    float peak;

    // How far the hand has been pulled back so far, 0 until a pull has started.
    public float Progress { get; private set; }

    // `reach`: how far ahead of the head the hand is. `gripHeld`: grip is down. `blocked`: the
    // hand is hovering or holding something, so the grip belongs to that. Returns true on the
    // frame the pull is completed (once per grip press).
    public bool Step(float reach, bool gripHeld, bool blocked)
    {
        if (!gripHeld)
        {
            wasHeld = false;
            armed = false;
            Progress = 0f;
            return false;
        }
        if (!wasHeld)
        {
            // A new press: only counts if the hand is out in front and the grip is free.
            wasHeld = true;
            armed = !blocked && reach >= minReach;
            peak = reach;
        }
        if (blocked) armed = false;
        if (!armed) { Progress = 0f; return false; }

        peak = Mathf.Max(peak, reach);
        Progress = peak - reach;
        if (Progress < pullDistance) return false;
        armed = false;
        return true;
    }
}

// Watches both controllers for the drawer-pull gesture and raises Pulled with the hand that
// made it. The archive window opens or closes on it. A light tick every few centimetres of
// the pull and a firmer pulse when it completes.
public class ArchiveDrawerGesture : MonoBehaviour
{
    public static event Action<Transform> Pulled;

    public Transform headTransform;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Gesture")]
    public float minReach = 0.3f;
    public float pullDistance = 0.25f;

    [Header("Haptics")]
    public float tickEvery = 0.05f;      // metres of pull between ticks
    public float tickAmplitude = 0.15f;
    public float tickDuration = 0.03f;
    public float doneAmplitude = 0.6f;
    public float doneDuration = 0.1f;

    class Hand
    {
        public Transform transform;
        public InputAction grip;
        public DrawerPullDetector detector = new DrawerPullDetector();
        public int ticks;
    }
    Hand left, right;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents() => Pulled = null;

    void OnEnable()
    {
        left = MakeHand(leftHand, "<XRController>{LeftHand}/gripPressed");
        right = MakeHand(rightHand, "<XRController>{RightHand}/gripPressed");
    }

    void OnDisable()
    {
        left?.grip.Disable();
        right?.grip.Disable();
    }

    Hand MakeHand(Transform t, string path)
    {
        Hand h = new Hand { transform = t, grip = new InputAction("ArchiveDrawerGrip", InputActionType.Button, path) };
        h.grip.Enable();
        return h;
    }

    void Update()
    {
        if (headTransform == null) return;
        Step(left);
        Step(right);
    }

    void Step(Hand h)
    {
        if (h.transform == null) return;
        h.detector.minReach = minReach;
        h.detector.pullDistance = pullDistance;

        bool fired = h.detector.Step(ReachOf(h.transform), h.grip.IsPressed(), IsBusy(h.transform));
        int ticks = Mathf.FloorToInt(h.detector.Progress / tickEvery);
        if (ticks > h.ticks && !fired) Haptic(h.transform, tickAmplitude, tickDuration);
        h.ticks = ticks;
        if (!fired) return;

        Haptic(h.transform, doneAmplitude, doneDuration);
        RaisePulled(h.transform);
    }

    // How far ahead of the head (along where it faces, level) the hand is.
    float ReachOf(Transform hand)
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return 0f;
        return Vector3.Dot(hand.position - headTransform.position, forward.normalized);
    }

    // The grip belongs to something else while the hand is holding or pointing at it. The
    // archive window doesn't count, so the same pull can close it while pointing at it.
    static bool IsBusy(Transform hand)
    {
        foreach (XRBaseInteractor interactor in hand.GetComponentsInChildren<XRBaseInteractor>())
        {
            foreach (IXRHoverInteractable hovered in interactor.interactablesHovered)
                if (!IsArchiveWindow(hovered)) return true;
            foreach (IXRSelectInteractable selected in interactor.interactablesSelected)
                if (!IsArchiveWindow(selected)) return true;
        }
        return false;
    }

    static bool IsArchiveWindow(IXRInteractable interactable) =>
        interactable is Component c && c.GetComponent<ArchiveScrollSurface>() != null;

    // Raises Pulled without a controller, for tests.
    public static void RaisePulled(Transform hand) => Pulled?.Invoke(hand);

    static void Haptic(Transform hand, float amplitude, float duration)
    {
        XRBaseInputInteractor input = hand.GetComponentInChildren<XRBaseInputInteractor>();
        if (input != null) input.SendHapticImpulse(amplitude, duration);
    }
}
