using UnityEngine;

public class PlusButtonHover : MonoBehaviour
{
    public Color hoverColor = Color.white;

    private Renderer[] renderers;
    private Color[] originalColors;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void OnMouseEnter()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = hoverColor;
            c.a = originalColors[i].a;
            renderers[i].material.color = c;
        }
    }

    void OnMouseExit()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}