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

        VRHandleConnector closest = null;
        float closestDist = snapDistance;
        Collider[] hits = Physics.OverlapSphere(transform.position, snapDistance, handleLayer);
        foreach (var hit in hits)
        {
            VRHandleConnector hc = hit.GetComponentInParent<VRHandleConnector>();
            if (hc == null || hc == this) continue;
            float d = Vector3.Distance(transform.position, hc.transform.position);
            if (d <= closestDist)
            {
                closest = hc;
                closestDist = d;
            }
        }

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
}
