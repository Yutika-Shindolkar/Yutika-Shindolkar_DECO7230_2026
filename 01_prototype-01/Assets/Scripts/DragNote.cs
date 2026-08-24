using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DragNote : MonoBehaviour
{
    private Camera cam;
    private Plane dragPlane;
    private Vector3 offset;
    private bool dragging = false;

    public Outline glowOutline;

    void Start()
    {
        cam = Camera.main;
        if (glowOutline != null) glowOutline.enabled = false;
    }

    void OnMouseEnter()
    {
        if (glowOutline != null) glowOutline.enabled = true;
    }

    void OnMouseExit()
    {
        if (glowOutline != null) glowOutline.enabled = false;
    }

    void OnMouseDown()
    {
        dragging = true;
        dragPlane = new Plane(-cam.transform.forward, transform.position);
        offset = transform.position - GetMouseWorldPoint();
    }

    void OnMouseUp()
    {
        dragging = false;
    }

    void Update()
    {
        if (dragging)
        {
            transform.position = GetMouseWorldPoint() + offset;
        }
    }

    Vector3 GetMouseWorldPoint()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position;
    }
}