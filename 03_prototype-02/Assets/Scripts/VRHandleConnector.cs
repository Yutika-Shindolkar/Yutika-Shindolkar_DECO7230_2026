using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Connecting notes, Miro-style but in 3D: hold grip on a note's handle to pull out a
// connector. The controller ray acts as the cursor: the connector's loose end follows the
// ray's tip, and when the ray points at another note the end snaps to that note's handle,
// which glows. Let go of grip on a glowing note to connect; anywhere else cancels.
// With a poke or close-up grab (no ray), the loose end follows the hand instead.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRHandleConnector : MonoBehaviour
{
    public GameObject linePrefab;
    public GameObject labelPrefab;
    public float snapDistance = 0.15f;  // fallback for non-ray drops: how close the hand must be to a note
    public LayerMask handleLayer;
    public GameObject glowOutline; // yellow hover-glow sphere, shown/hidden below

    XRSimpleInteractable interactable;
    GameObject currentLineObj;
    ConnectionLine currentLine;

    // While dragging:
    IXRSelectInteractor dragger;
    Transform looseEnd;          // the point the line's free end is drawn to
    float rayDistance;           // how far along the ray the loose end floats when not over a note
    VRHandleConnector snapTarget; // the note handle the ray is currently over, if any
    bool hovered;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrabbed);
        interactable.selectExited.RemoveListener(OnReleased);
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        CancelDrag();
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        hovered = true;
        RefreshGlow();
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        hovered = false;
        RefreshGlow();
    }

    // Glow while hovered, or while another handle's connector is snapped onto this one.
    bool isSnapTarget;

    void SetSnapTarget(bool value)
    {
        isSnapTarget = value;
        RefreshGlow();
    }

    void RefreshGlow()
    {
        if (glowOutline != null) glowOutline.SetActive(hovered || isSnapTarget);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (VRTourMode.IsTouring || currentLine != null) return;

        dragger = args.interactorObject;
        looseEnd = new GameObject("ConnectorLooseEnd").transform;
        looseEnd.position = transform.position;

        Transform rayOrigin = RayOrigin();
        rayDistance = rayOrigin != null ? Vector3.Distance(rayOrigin.position, transform.position) : 0f;

        currentLineObj = Instantiate(linePrefab);
        currentLine = currentLineObj.GetComponent<ConnectionLine>();
        currentLine.pointA = transform;
        currentLine.pointB = null;
        currentLine.followWhilePulling = looseEnd;
        UpdateLooseEnd();
    }

    void Update()
    {
        if (currentLine != null) UpdateLooseEnd();
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
            target = NoteUnderRay(rayOrigin, out Vector3 hitPoint);
            looseEnd.position = target != null
                ? target.transform.position
                : rayOrigin.position + rayOrigin.forward * rayDistance;
        }
        else if (dragger is Component hand && hand != null)
        {
            looseEnd.position = hand.transform.position;
            target = FindDropTarget(looseEnd.position);
            if (target != null) looseEnd.position = target.transform.position;
        }

        if (target != snapTarget)
        {
            if (snapTarget != null) snapTarget.SetSnapTarget(false);
            snapTarget = target;
            if (snapTarget != null) snapTarget.SetSnapTarget(true);
        }
    }

    // The first note the ray hits (skipping this connector's own note), as its handle.
    VRHandleConnector NoteUnderRay(Transform rayOrigin, out Vector3 hitPoint)
    {
        hitPoint = default;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin.position, rayOrigin.forward, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            NoteData note = hit.collider.GetComponentInParent<NoteData>();
            if (note == null || note.transform == transform.parent) continue;
            hitPoint = hit.point;
            return note.GetComponentInChildren<VRHandleConnector>();
        }
        return null;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (currentLine == null) return;

        UpdateLooseEnd(); // use exactly where the cursor is at the moment of release
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
        if (snapTarget != null) snapTarget.SetSnapTarget(false);
        snapTarget = null;
        if (looseEnd != null) Destroy(looseEnd.gameObject);
        looseEnd = null;
        dragger = null;
    }

    // For hand/poke drags: a note within snapDistance of the hand (not this one's own).
    VRHandleConnector FindDropTarget(Vector3 dropPoint)
    {
        Transform ownNote = transform.parent;
        VRHandleConnector closest = null;
        float closestDist = snapDistance;

        Collider[] hits = Physics.OverlapSphere(dropPoint, snapDistance, Physics.DefaultRaycastLayers | handleLayer,
            QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            NoteData note = hit.GetComponentInParent<NoteData>();
            if (note == null || note.transform == ownNote) continue;

            VRHandleConnector target = note.GetComponentInChildren<VRHandleConnector>();
            if (target == null) continue;

            float d = Vector3.Distance(dropPoint, hit.ClosestPoint(dropPoint));
            if (d <= closestDist)
            {
                closest = target;
                closestDist = d;
            }
        }
        return closest;
    }
}
