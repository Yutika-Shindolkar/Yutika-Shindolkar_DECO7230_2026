using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// VR replacement for IP1's mouse-based PlusButtonClick, now a translucent green sphere.
// Poking it opens the shared VRKeyboard (see VRKeyboard.cs) aimed at this connection's
// label text - real typed text, not presets - matching the original design concept ("a
// '+' icon labels the relationship"). The label itself only appears once the user
// actually confirms something on the keyboard; poking the keyboard's Cancel key instead
// backs out and reactivates this sphere (passed in as VRKeyboard's cancelReactivateTarget)
// so a fresh connection never gets stranded with no way to add a label. Also drives the
// sphere's own yellow hover-glow, same pattern as the shape buttons' glow rings in
// NoteCreationPanel/IP2aSceneBuilder.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRPlusButtonClick : MonoBehaviour
{
    public TMP_Text labelText;
    public GameObject glowOutline;

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnPoked);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnPoked);
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
    }

    void OnPoked(SelectEnterEventArgs args)
    {
        if (VRKeyboard.Instance != null && labelText != null)
            VRKeyboard.Instance.Open(labelText, "", gameObject);
        gameObject.SetActive(false);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (glowOutline != null) glowOutline.SetActive(true);
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        if (glowOutline != null) glowOutline.SetActive(false);
    }
}
