using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HandleConnector : MonoBehaviour
{
    public static List<HandleConnector> allHandles = new List<HandleConnector>();

    public GameObject linePrefab;
    public GameObject labelPrefab;
    public float snapDistance = 0.1f;

    private Camera cam;
    private GameObject currentLineObj;
    private ConnectionLine currentLine;

    void OnEnable() { allHandles.Add(this); }
    void OnDisable() { allHandles.Remove(this); }
    void Start() { cam = Camera.main; }

    void OnMouseDown()
    {
        if (TourMode.IsTouring) return;

        currentLineObj = Instantiate(linePrefab);
        currentLine = currentLineObj.GetComponent<ConnectionLine>();
        currentLine.pointA = transform;
        currentLine.pointB = null;
        currentLine.cam = cam;
    }

    void OnMouseUp()
    {
        if (currentLine == null) return;

        HandleConnector closest = null;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            HandleConnector hc = hit.collider.GetComponent<HandleConnector>();
            if (hc != null && hc != this)
            {
                closest = hc;
            }
        }

        if (closest != null)
        {
            currentLine.pointB = closest.transform;

            if (labelPrefab != null)
            {
                GameObject label = Instantiate(labelPrefab, currentLineObj.transform);
                LineLabelFollow follow = label.GetComponent<LineLabelFollow>();
                if (follow != null)
                {
                    follow.line = currentLineObj.GetComponent<LineRenderer>();
                }
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