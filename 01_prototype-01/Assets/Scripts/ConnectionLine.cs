using UnityEngine;
using UnityEngine.InputSystem;

public class ConnectionLine : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB; // stays null while still being dragged
    public Camera cam;

    private LineRenderer line;
    private const int segments = 20;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
    }

    void Update()
    {
        Vector3 endPos = pointB != null ? pointB.position : GetMouseWorldPoint();
        DrawCurve(pointA.position, endPos);
    }

    void DrawCurve(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) / 2f;
        mid += Vector3.up * 0.05f; // slight upward arc - tweak this number for more/less curve

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 point = Vector3.Lerp(Vector3.Lerp(start, mid, t), Vector3.Lerp(mid, end, t), t);
            line.SetPosition(i, point);
        }
    }

    Vector3 GetMouseWorldPoint()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane dragPlane = new Plane(-cam.transform.forward, pointA.position);
        float distance;
        if (dragPlane.Raycast(ray, out distance)) return ray.GetPoint(distance);
        return pointA.position;
    }
}