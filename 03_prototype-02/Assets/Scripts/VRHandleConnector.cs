using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// One connection handle on a note (each note has one per edge). The interaction, agreed
// with Yutika:
//  1. Ray on a note: its handles appear and gently breathe (NoteHandles.Revealed).
//  2. Ray on one handle: it turns yellow (grabbable).
//  3. Hold grip: a line comes out, its loose end following the ray.
//  4. Ray reaches another note: that note's handles appear and breathe too.
//  5. Ray within the magnet radius of one of its handles: that handle AND this one turn
//     green and the line's end snaps onto it.
//  6. Let go on green: connected. Anywhere else: the line disappears.
// A handle can have any number of lines. Handles with a line attached stay visible.
// With a poke or close-up grab (no ray), the loose end follows the hand instead.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRHandleConnector : MonoBehaviour, IPressAndHold
{
    public GameObject linePrefab;
    public GameObject labelPrefab;
    public Transform visual;        // the dark dot
    public GameObject glowOutline;  // halo behind the dot: yellow (hovered) or green (forming)
    public float magnetRadius = 0.08f; // how close the ray must pass to a target handle to snap

    public static readonly Color HoverColor = new Color(1f, 0.85f, 0.2f, 0.6f);
    public static readonly Color FormingColor = new Color(0.3f, 0.85f, 0.4f, 0.7f);

    public enum State { Hidden, Idle, Hovered, Forming }
    public State VisualState { get; private set; } = State.Hidden;

    // While the ray (or a hand) is on this handle, or it's the source of a drag.
    public bool IsHoveredOrDragging => interactable != null && (interactable.isHovered || currentLine != null);

    public int ConnectionCount
    {
        get
        {
            int count = 0;
            foreach (ConnectionLine line in ConnectionLine.All)
                if (line.pointB != null && (line.pointA == transform || line.pointB == transform)) count++;
            return count;
        }
    }

    XRSimpleInteractable interactable;
    NoteHandles note;
    Vector3 visualBaseScale, glowBaseScale;
    float shownAmount;                 // 0 hidden .. 1 fully shown (smoothed)
    Renderer visualRenderer, glowRenderer;
    MaterialPropertyBlock glowBlock;
    bool forming;                      // part of a connection that's about to form

    // While dragging a connector out of this handle:
    GameObject currentLineObj;
    ConnectionLine currentLine;
    IXRSelectInteractor dragger;
    Transform looseEnd;
    float rayDistance;
    VRHandleConnector snapTarget;
    readonly List<VRHandleConnector> candidates = new List<VRHandleConnector>();

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        note = GetComponentInParent<NoteHandles>();
        if (visual != null)
        {
            visualBaseScale = visual.localScale;
            visualRenderer = visual.GetComponent<Renderer>();
        }
        if (glowOutline != null)
        {
            glowBaseScale = glowOutline.transform.localScale;
            glowRenderer = glowOutline.GetComponent<Renderer>();
        }
        glowBlock = new MaterialPropertyBlock();
        ApplyVisuals(0f, 1f, State.Hidden);
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrabbed);
        interactable.selectExited.RemoveListener(OnReleased);
        CancelDrag();
    }

    void Update()
    {
        if (currentLine != null) UpdateLooseEnd();
        UpdateVisuals();
    }

    // ---------- Appearance ----------

    void UpdateVisuals()
    {
        bool revealed = note != null && note.Revealed;
        State state;
        if (forming) state = State.Forming;
        else if (interactable.isHovered || currentLine != null) state = State.Hovered;
        else if (revealed || ConnectionCount > 0) state = State.Idle;
        else state = State.Hidden;
        VisualState = state;

        // Smoothly pop in / fade out, so handles don't blink on and off.
        float target = state == State.Hidden ? 0f : 1f;
        shownAmount = Mathf.MoveTowards(shownAmount, target, Time.deltaTime * 6f);

        // Gentle breathing while the note is revealed and this handle is just waiting;
        // steady and a bit bigger once it's the one under the ray or forming a connection.
        float size;
        if (state == State.Hovered || state == State.Forming) size = 1.25f;
        else if (revealed) size = 1f + 0.1f * Mathf.Sin(Time.time * Mathf.PI * 2f / 1.6f);
        else size = 1f;

        ApplyVisuals(shownAmount, size, state);
    }

    void ApplyVisuals(float shown, float size, State state)
    {
        float s = Mathf.SmoothStep(0f, 1f, shown) * size;
        bool visible = s > 0.01f;
        if (visual != null)
        {
            visual.localScale = visualBaseScale * s;
            if (visualRenderer != null) visualRenderer.enabled = visible;
        }
        if (glowOutline != null)
        {
            bool glow = visible && (state == State.Hovered || state == State.Forming);
            glowOutline.SetActive(glow);
            if (glow)
            {
                glowOutline.transform.localScale = glowBaseScale * s;
                glowBlock.SetColor("_BaseColor", state == State.Forming ? FormingColor : HoverColor);
                glowRenderer.SetPropertyBlock(glowBlock);
            }
        }
    }

    // ---------- Dragging a connector ----------

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (VRTourMode.IsTouring || currentLine != null) return;

        dragger = args.interactorObject;
        looseEnd = new GameObject("ConnectorLooseEnd").transform;
        looseEnd.position = transform.position;

        Transform rayOrigin = RayOrigin();
        rayDistance = rayOrigin != null ? Vector3.Distance(rayOrigin.position, transform.position) : 0f;

        candidates.Clear();
        foreach (VRHandleConnector h in FindObjectsByType<VRHandleConnector>(FindObjectsSortMode.None))
            if (h.note != note) candidates.Add(h); // never connect a note to itself

        currentLineObj = Instantiate(linePrefab);
        currentLine = currentLineObj.GetComponent<ConnectionLine>();
        currentLine.pointA = transform;
        currentLine.pointB = null;
        currentLine.followWhilePulling = looseEnd;
        UpdateLooseEnd();
    }

    // The ray origin of the interactor dragging this connector, or null for a poke or
    // direct (hand) grab.
    Transform RayOrigin() =>
        dragger is IXRRayProvider ray && dragger is Component c && c != null ? ray.GetOrCreateRayOrigin() : null;

    void UpdateLooseEnd()
    {
        VRHandleConnector target = null;
        Transform rayOrigin = RayOrigin();

        if (rayOrigin != null)
        {
            Vector3 origin = rayOrigin.position, dir = rayOrigin.forward;
            NoteHandles hitNote = NoteUnderRay(origin, dir, out Vector3 hitPoint);
            if (hitNote != null) hitNote.RevealForDrag();

            // Magnet: the target handle the ray passes closest to, within magnetRadius.
            float best = magnetRadius;
            foreach (VRHandleConnector h in candidates)
            {
                if (h == null) continue;
                Vector3 toHandle = h.transform.position - origin;
                float along = Vector3.Dot(toHandle, dir);
                if (along <= 0f) continue;
                float off = Vector3.Cross(dir, toHandle).magnitude;
                if (off < best) { best = off; target = h; }
            }

            if (target != null) looseEnd.position = target.transform.position;
            else looseEnd.position = hitNote != null ? hitPoint : origin + dir * rayDistance;
        }
        else if (dragger is Component hand && hand != null)
        {
            Vector3 p = hand.transform.position;
            float best = magnetRadius;
            foreach (VRHandleConnector h in candidates)
            {
                if (h == null) continue;
                float d = Vector3.Distance(p, h.transform.position);
                if (d < best) { best = d; target = h; }
            }
            looseEnd.position = target != null ? target.transform.position : p;
        }

        if (target != null && target.note != null) target.note.RevealForDrag();
        SetSnapTarget(target);
    }

    void SetSnapTarget(VRHandleConnector target)
    {
        if (target == snapTarget) return;
        if (snapTarget != null) snapTarget.forming = false;
        snapTarget = target;
        if (snapTarget != null) snapTarget.forming = true;
        forming = snapTarget != null; // this end goes green too: "a connection is forming"
    }

    // The note the ray hits first, ignoring this handle's own note.
    NoteHandles NoteUnderRay(Vector3 origin, Vector3 dir, out Vector3 hitPoint)
    {
        hitPoint = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            NoteHandles n = hit.collider.GetComponentInParent<NoteHandles>();
            if (n == null || n == note) continue;
            hitPoint = hit.point;
            return n;
        }
        return null;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (currentLine == null) return;

        UpdateLooseEnd(); // use exactly where the ray is at the moment of release
        VRHandleConnector target = snapTarget;

        if (target != null)
        {
            currentLine.pointB = target.transform;
            currentLine.followWhilePulling = null;

            if (labelPrefab != null)
            {
                GameObject label = Instantiate(labelPrefab, currentLineObj.transform);
                LineLabelFollow follow = label.GetComponent<LineLabelFollow>();
                if (follow != null) follow.line = currentLineObj.GetComponent<LineRenderer>();
            }

            TourManager.RegisterConnection(transform.parent, target.transform.parent);
            currentLineObj = null;
            currentLine = null;
            EndDrag();
        }
        else
        {
            CancelDrag();
        }
    }

    void CancelDrag()
    {
        if (currentLineObj != null) Destroy(currentLineObj);
        currentLineObj = null;
        currentLine = null;
        EndDrag();
    }

    void EndDrag()
    {
        SetSnapTarget(null);
        if (looseEnd != null) Destroy(looseEnd.gameObject);
        looseEnd = null;
        dragger = null;
        candidates.Clear();
    }
}
