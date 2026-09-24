using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// Sits on a connection's already-revealed LabelText. Poking it again reopens the shared
// VRKeyboard pre-filled with the current text, so a relationship label stays editable
// after the first time it's set (replaces PresetLabelCycler's poke-to-cycle role now that
// labels are freely typed - see VRPlusButtonClick.cs for the initial reveal).
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRLabelEdit : MonoBehaviour
{
    public TMP_Text targetText;

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable() => interactable.selectEntered.AddListener(OnPoked);
    void OnDisable() => interactable.selectEntered.RemoveListener(OnPoked);

    void OnPoked(SelectEnterEventArgs args)
    {
        if (VRKeyboard.Instance != null && targetText != null)
            VRKeyboard.Instance.Open(targetText, targetText.text);
    }
}
