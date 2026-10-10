using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Grab a connection line (grip) and yank it to disconnect: while held the line bends
// toward your ray tip / hand and turns red as you pull; past breakDistance it snaps and
// the connection (line + label) is removed. Let go before that and it springs back.
// Added at runtime by ConnectionLine once a connection is made. Grab colliders cover the
// middle of the line only, so grabbing right next to a note still gets its handle.
public class LineYank : MonoBehaviour, IPressAndHold
{
    public float breakDistance = 0.25f;

    ConnectionLine line;
    LineRenderer lineRenderer;
    XRSimpleInteractable interactable;
    CapsuleCollider[] capsules;

    IXRSelectInteractor holder;
    Vector3 grabPoint;       // where on the line it was grabbed
    float grabRayDistance;   // for ray grabs: how far along the ray that point was

    // Line point index ranges covered by grab capsules (of the line's 20 points).
    static readonly int[,] Spans = { { 4, 8 }, { 8, 12 }, { 12, 16 } };
    const float GrabRadius = 0.02f;

    public static LineYank AddTo(ConnectionLine line)
    {
        GameObject area = new GameObject("GrabArea");
        area.transform.SetParent(line.transform, false);
        var capsules = new CapsuleCollider[Spans.GetLength(0)];
        for (int i = 0; i < capsules.Length; i++)
        {
            GameObject c = new GameObject("GrabCapsule_" + i);
            c.transform.SetParent(area.transform, false);
            capsules[i] = c.AddComponent<CapsuleCollider>();
            capsules[i].direction = 2; // along local Z
            capsules[i].radius = GrabRadius;
        }

        LineYank yank = area.AddComponent<LineYank>();
        yank.line = line;
        yank.lineRenderer = line.GetComponent<LineRenderer>();
        yank.capsules = capsules;
        yank.PlaceCapsules();
        // Added after the colliders exist, so the interactable picks them up.
        yank.interactable = area.AddComponent<XRSimpleInteractable>();
        yank.interactable.selectEntered.AddListener(yank.OnGrabbed);
        yank.interactable.selectExited.AddListener(yank.OnReleased);
        return yank;
    }

    void LateUpdate()
    {
        if (line == null) return;

        if (holder != null)
        {
            Vector3 pull = PullPoint();
            float distance = Vector3.Distance(pull, grabPoint);
            line.bendThrough = pull;
            line.stretch = Mathf.Clamp01(distance / breakDistance);
            if (distance > breakDistance)
            {
                Break();
                return;
            }
        }
        PlaceCapsules();
    }

    void PlaceCapsules()
    {
        for (int i = 0; i < capsules.Length; i++)
        {
            Vector3 a = lineRenderer.GetPosition(Spans[i, 0]);
            Vector3 b = lineRenderer.GetPosition(Spans[i, 1]);
            Transform t = capsules[i].transform;
            t.position = (a + b) * 0.5f;
            Vector3 dir = b - a;
            if (dir.sqrMagnitude > 1e-8f) t.rotation = Quaternion.LookRotation(dir);
            capsules[i].height = dir.magnitude + GrabRadius * 2f;
        }
    }

    Transform RayOrigin() =>
        holder is IXRRayProvider ray && holder is Component c && c != null ? ray.GetOrCreateRayOrigin() : null;

    Vector3 PullPoint()
    {
        Transform origin = RayOrigin();
        if (origin != null) return origin.position + origin.forward * grabRayDistance;
        return holder is Component hand && hand != null ? hand.transform.position : grabPoint;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        holder = args.interactorObject;
        Transform origin = RayOrigin();

        // The line point nearest the ray (or hand) is the bit that was grabbed.
        float best = float.MaxValue;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 p = lineRenderer.GetPosition(i);
            float d = origin != null
                ? Vector3.Cross(origin.forward, p - origin.position).magnitude
                : Vector3.Distance(p, ((Component)holder).transform.position);
            if (d < best) { best = d; grabPoint = p; }
        }
        grabRayDistance = origin != null ? Vector3.Dot(grabPoint - origin.position, origin.forward) : 0f;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        holder = null;
        if (line == null) return;
        line.bendThrough = null; // springs back to its normal curve
        line.stretch = 0f;
    }

    void Break()
    {
        ConnectionLine broken = line;
        line = null;
        holder = null;
        TourManager.PruneUnconnected(broken);
        Destroy(broken.gameObject); // takes its label and this grab area with it
    }
}
