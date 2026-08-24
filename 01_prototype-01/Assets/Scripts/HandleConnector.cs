using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HandleConnector : MonoBehaviour
{
    public static List<HandleConnector> allHandles = new List<HandleConnector>();

    public GameObject linePrefab;
    public float snapDistance = 0.1f;

    private Camera cam;
    private GameObject currentLineObj;
    private ConnectionLine currentLine;

    void OnEnable() { allHandles.Add(this); }
    void OnDisable() { allHandles.Remove(this); }
    void Start() { cam = Camera.main; }

    void OnMouseDown()
    {
        currentLineObj = Instantiate(linePrefab);
        currentLine = currentLineObj.GetComponent<ConnectionLine>();
        currentLine.pointA = transform;
        currentLine.pointB = null;
        currentLine.cam = cam;
    }

    void OnMouseUp()
    {
        if (currentLine == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane plane = new Plane(-cam.transform.forward, transform.position);
        Vector3 releasePoint = transform.position;
        float dist;
        if (plane.Raycast(ray, out dist)) releasePoint = ray.GetPoint(dist);

        HandleConnector closest = null;
        float closestDist = snapDistance;
        foreach (HandleConnector h in allHandles)
        {
            if (h == this) continue;
            float d = Vector3.Distance(h.transform.position, releasePoint);
            if (d < closestDist) { closestDist = d; closest = h; }
        }

        if (closest != null) currentLine.pointB = closest.transform;
        else Destroy(currentLineObj);

        currentLineObj = null;
        currentLine = null;
    }
}