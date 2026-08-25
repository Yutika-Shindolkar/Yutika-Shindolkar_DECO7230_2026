using UnityEngine;

public class TrashZone : MonoBehaviour
{
    public Color hoverColor = Color.red;

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
        rend.material.color = (isHovering && DragNote.AnyDragging) ? hoverColor : originalColor;
    }
}