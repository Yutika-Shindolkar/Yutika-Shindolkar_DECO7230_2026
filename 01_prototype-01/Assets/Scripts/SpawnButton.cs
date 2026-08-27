using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public Transform cam;
    public float spawnDistance = 1.5f;
    public Color hoverColor = Color.yellow;

    private Renderer rend;
    private Color originalColor;
    private bool isHovering = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        isHovering = true;
    }

    void OnMouseExit()
    {
        isHovering = false;
    }

    void Update()
    {
        if (rend == null) return;
        rend.material.color = (isHovering && !DragNote.AnyDragging) ? hoverColor : originalColor;
    }

    void OnMouseDown()
    {
        if (TourMode.IsTouring) return;
        if (DragNote.AnyDragging) return;

        Vector3 spawnPos = cam.position + cam.forward * spawnDistance;
        GameObject newObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        DragNote drag = newObj.GetComponent<DragNote>();
        if (drag != null)
        {
            drag.ForceStartDrag();
        }
    }
}