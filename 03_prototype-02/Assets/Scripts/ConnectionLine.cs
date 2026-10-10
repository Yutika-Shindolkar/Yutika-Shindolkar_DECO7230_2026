using System.Collections.Generic;
using UnityEngine;

// Draws the curved thread between two notes. While being pulled (pointB is null),
// the loose end follows whichever controller/hand transform is currently dragging it.
// Once connected it can be grabbed and yanked to disconnect (see LineYank): while held it
// bends toward the hand and reddens as it nears breaking.
// Deleting a note removes its lines (see NoteData.OnDestroy).
public class ConnectionLine : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;       // stays null while still being pulled
    public Transform followWhilePulling; // the interactor's transform, set while dragging

    // Every line in the scene, so handles can count their connections cheaply.
    public static readonly List<ConnectionLine> All = new List<ConnectionLine>();

    // Set by LineYank while the line is grabbed: the curve passes through this point.
    [System.NonSerialized] public Vector3? bendThrough;
    // 0..1, how close a yank is to breaking the line; tints the line toward red.
    [System.NonSerialized] public float stretch;

    static readonly Color RestColor = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color BreakColor = new Color(0.85f, 0.2f, 0.2f);

    LineRenderer line;
    MaterialPropertyBlock block;
    LineYank yank;
    const int segments = 20;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        block = new MaterialPropertyBlock();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (pointA == null) return;
        Vector3 endPos = pointB != null ? pointB.position
            : (followWhilePulling != null ? followWhilePulling.position : pointA.position);
        DrawCurve(pointA.position, endPos);

        // A finished connection becomes grabbable (to yank it apart).
        if (pointB != null && yank == null)
            yank = LineYank.AddTo(this);

        line.GetPropertyBlock(block);
        block.SetColor("_BaseColor", Color.Lerp(RestColor, BreakColor, Mathf.InverseLerp(0.4f, 1f, stretch)));
        line.SetPropertyBlock(block);
    }

    void DrawCurve(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) / 2f;
        mid += Vector3.up * 0.05f;

        // While grabbed, use a control point that makes the curve pass through the hand.
        if (bendThrough.HasValue)
            mid = 2f * bendThrough.Value - 0.5f * (start + end);

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 point = Vector3.Lerp(Vector3.Lerp(start, mid, t), Vector3.Lerp(mid, end, t), t);
            line.SetPosition(i, point);
        }
    }
}
