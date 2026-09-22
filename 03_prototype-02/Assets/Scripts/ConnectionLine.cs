using UnityEngine;

// Draws the curved thread between two notes. While being pulled (pointB is null),
// the loose end follows whichever controller/hand transform is currently dragging it.
public class ConnectionLine : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;       // stays null while still being pulled
    public Transform followWhilePulling; // the interactor's transform, set while dragging

    LineRenderer line;
    const int segments = 20;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
    }

    void Update()
    {
        if (pointA == null) return;
        Vector3 endPos = pointB != null ? pointB.position
            : (followWhilePulling != null ? followWhilePulling.position : pointA.position);
        DrawCurve(pointA.position, endPos);
    }

    void DrawCurve(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) / 2f;
        mid += Vector3.up * 0.05f;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 point = Vector3.Lerp(Vector3.Lerp(start, mid, t), Vector3.Lerp(mid, end, t), t);
            line.SetPosition(i, point);
        }
    }
}
