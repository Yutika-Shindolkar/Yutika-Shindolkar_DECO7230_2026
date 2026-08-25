using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DragNote : MonoBehaviour
{
    private Camera cam;
    private Plane dragPlane;
    private Vector3 offset;
    private bool dragging = false;
    private float dragDistance;

    public static bool AnyDragging = false;

    public Outline glowOutline;
    public float depthSpeed = 2f;

    void Awake()
    {
        cam = Camera.main;
    }

    void Start()
    {
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
        AnyDragging = true;
        dragDistance = Vector3.Distance(cam.transform.position, transform.position);
        dragPlane = new Plane(-cam.transform.forward, transform.position);
        offset = transform.position - GetMouseWorldPoint();
    }

    void OnMouseUp()
    {
        dragging = false;
        AnyDragging = false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.GetComponent<TrashZone>() != null)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void Update()
    {
        if (dragging)
        {
            if (Keyboard.current.pageDownKey.isPressed)
            {
                dragDistance -= depthSpeed * Time.deltaTime;
            }
            if (Keyboard.current.pageUpKey.isPressed)
            {
                dragDistance += depthSpeed * Time.deltaTime;
            }
            dragDistance = Mathf.Max(dragDistance, 0.3f);

            Vector3 planePoint = cam.transform.position + cam.transform.forward * dragDistance;
            dragPlane = new Plane(-cam.transform.forward, planePoint);

            transform.position = GetMouseWorldPoint() + offset;
        }
    }

    public void ForceStartDrag()
    {
        dragging = true;
        AnyDragging = true;
        dragDistance = Vector3.Distance(cam.transform.position, transform.position);
        dragPlane = new Plane(-cam.transform.forward, transform.position);
        offset = Vector3.zero;
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