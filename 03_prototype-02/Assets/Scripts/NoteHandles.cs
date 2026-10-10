using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Decides when a note's connection handles are revealed: while a controller ray (or hand)
// is on the note or any of its handles, while one of its handles is being dragged, or
// while someone is dragging a connector over this note. Each handle reads Revealed and
// animates itself (see VRHandleConnector). Handles with a line attached always show.
public class NoteHandles : MonoBehaviour
{
    public VRHandleConnector[] Handles { get; private set; }

    XRGrabInteractable grab;
    int dragRevealFrame = -10;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        Handles = GetComponentsInChildren<VRHandleConnector>(true);
    }

    // Called every frame by a connector being dragged over this note, so the target
    // note's handles appear (and breathe) as the line approaches.
    public void RevealForDrag() => dragRevealFrame = Time.frameCount;

    public bool Revealed
    {
        get
        {
            if (grab != null && grab.isHovered) return true;
            if (Time.frameCount - dragRevealFrame <= 1) return true;
            foreach (VRHandleConnector h in Handles)
                if (h != null && h.IsHoveredOrDragging) return true;
            return false;
        }
    }
}
