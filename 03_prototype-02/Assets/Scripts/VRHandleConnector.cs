using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// VR replacement for IP1's mouse-based HandleConnector.
// Grab (select) a handle to start pulling a thread; release it near another handle to connect.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRHandleConnector : MonoBehaviour
{
    public GameObject linePrefab;
    public GameObject labelPrefab;
    public float snapDistance = 0.15f;
    public LayerMask handleLayer;
    public GameObject glowOutline; // yellow hover-glow sphere, shown/hidden below

    XRSimpleInteractable interactable;
    GameObject currentLineObj;
    ConnectionLine currentLine;

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
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (glowOutline != null) glowOutline.SetActive(true);
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        if (glowOutline != null) glowOutline.SetActive(false);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (VRTourMode.IsTouring) return;

        currentLineObj = Instantiate(linePrefab);
        currentLine = currentLineObj.GetComponent<ConnectionLine>();
        currentLine.pointA = transform;
        currentLine.pointB = null;
        currentLine.followWhilePulling = args.interactorObject.transform;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (currentLine == null) return;

        // Look for a target around where the loose end of the line is (the hand that was
        // dragging it), not around this handle - otherwise only notes already touching
        // this one could ever be connected. Dropping anywhere on another note, or near its
        // handle, connects to that note's handle.
        Vector3 dropPoint = currentLine.followWhilePulling != null
            ? currentLine.followWhilePulling.position
            : transform.position;
        VRHandleConnector closest = FindDropTarget(dropPoint);

        if (closest != null)
        {
            currentLine.pointB = closest.transform;
            currentLine.followWhilePulling = null;

            if (labelPrefab != null)
            {
                GameObject label = Instantiate(labelPrefab, currentLineObj.transform);
                LineLabelFollow follow = label.GetComponent<LineLabelFollow>();
                if (follow != null) follow.line = currentLineObj.GetComponent<LineRenderer>();
            }

            TourManager.RegisterConnection(transform.parent, closest.transform.parent);
        }
        else
        {
            Destroy(currentLineObj);
        }

        currentLineObj = null;
        currentLine = null;
    }

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
