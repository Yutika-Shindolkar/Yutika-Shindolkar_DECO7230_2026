using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A spot on the floor ahead of the player that appears while a note is being held: lower the
// held note over it and let go to use it (the trash bin deletes, the archive tray stores).
//
// It's placed once when the hold starts (in the direction the player is facing) and then
// stays put, so it doesn't slide around as they turn their head. 2 m ahead puts it inside
// the bottom of the headset's view while looking straight ahead; much closer and it sits
// below the view. While a held note is over it, it lights up with a glowing outline and the
// holding controller gets a light tick.
public abstract class NoteDropZone : MonoBehaviour
{
    public Transform playerRoot;      // XR Origin: its height is the floor (Tracking Origin Mode: Floor)
    public Transform headTransform;   // the VR camera: where the player is and which way they face
    public float footOffsetY = 0f;    // tweak if the zone should sit slightly above/below the floor

    [Header("Placement and drop zone")]
    public float distanceAhead = 2f;      // metres in front of the player, on the floor
    public float dropRadius = 0.3f;       // how far from the centre (sideways) still counts
    public float dropHeight = 0.6f;       // how high above the floor the note can be and still count

    [Header("Feedback")]
    public Renderer visual;   // the model
    public Renderer outline;  // glow around the model, shown while a held note is over it
    public Color glowDim = new Color(0.85f, 0.05f, 0.05f);
    public Color glowBright = new Color(1f, 0.35f, 0.35f);
    public float glowPulseSpeed = 6f;

    [Header("Haptics")]
    public float hoverAmplitude = 0.25f;  // a light tick when the note moves over the zone
    public float hoverDuration = 0.05f;

    Renderer[] renderers;
    Collider zoneCollider;
    MaterialPropertyBlock block;
    int activeHolds = 0;
    int readyFrame = -10;

    // Where this zone sits relative to the spot straight ahead of the player, in the
    // player's facing direction (x = to their right). Zero puts it right on that spot.
    protected virtual Vector3 PlacementOffset => Vector3.zero;

    protected virtual void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        SetVisible(false);
    }

    void Update()
    {
        if (activeHolds <= 0 || outline == null) return;
        bool ready = Time.frameCount - readyFrame <= 1;
        outline.enabled = ready;
        if (!ready) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed);
        outline.GetPropertyBlock(block);
        block.SetColor("_BaseColor", Color.Lerp(glowDim, glowBright, t));
        outline.SetPropertyBlock(block);
    }

    // On the floor, distanceAhead in front of where the player is looking (ignoring tilt).
    Vector3 PositionAhead()
    {
        Vector3 standing = headTransform != null ? headTransform.position : playerRoot.position;
        Vector3 facing = headTransform != null ? headTransform.forward : playerRoot.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) facing = playerRoot.forward;
        Vector3 pos = standing + facing.normalized * distanceAhead;
        pos.y = playerRoot.position.y + footOffsetY;
        return pos;
    }

    public void RegisterHoldStart()
    {
        if (activeHolds == 0 && playerRoot != null)
        {
            Vector3 ahead = PositionAhead();
            Vector3 facing = ahead - (headTransform != null ? headTransform.position : playerRoot.position);
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing) : transform.rotation;
            transform.position = ahead + rotation * PlacementOffset;
            transform.rotation = rotation;
        }
        activeHolds++;
        SetVisible(true);
    }

    public void RegisterHoldEnd()
    {
        activeHolds = Mathf.Max(0, activeHolds - 1);
        if (activeHolds == 0) SetVisible(false);
    }

    // Is a held note at worldPos over the zone: within dropRadius sideways and no higher
    // than dropHeight above the floor?
    public bool IsPositionOver(Vector3 worldPos)
    {
        if (activeHolds <= 0) return false;
        Vector3 offset = worldPos - transform.position;
        float height = offset.y;
        offset.y = 0f;
        return offset.magnitude <= dropRadius && height <= dropHeight && height >= -0.2f;
    }

    // Called each frame by a held note that's over the zone, so it lights up.
    public void ShowReady() => readyFrame = Time.frameCount;

    // Haptics run from here rather than the note, since the note may be gone by the time
    // the second pulse of a pattern plays.
    public void PulseHover(Object interactor)
    {
        if (interactor is XRBaseInputInteractor input) input.SendHapticImpulse(hoverAmplitude, hoverDuration);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible && r != outline;
        if (zoneCollider != null) zoneCollider.enabled = visible;
    }
}
