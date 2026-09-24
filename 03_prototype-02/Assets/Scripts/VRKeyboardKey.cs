using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// One pokeable key on VRKeyboard. Character keys type their own "character" string;
// the three reserved actions call the matching VRKeyboard method instead of typing.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRKeyboardKey : MonoBehaviour
{
    public enum KeyAction { Character, Space, Backspace, Confirm, Cancel }

    public KeyAction action = KeyAction.Character;
    public string character = "a";

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable() => interactable.selectEntered.AddListener(OnPoked);
    void OnDisable() => interactable.selectEntered.RemoveListener(OnPoked);

    void OnPoked(SelectEnterEventArgs args)
    {
        if (VRKeyboard.Instance == null) return;
        switch (action)
        {
            case KeyAction.Character: VRKeyboard.Instance.TypeChar(character); break;
            case KeyAction.Space: VRKeyboard.Instance.TypeChar(" "); break;
            case KeyAction.Backspace: VRKeyboard.Instance.Backspace(); break;
            case KeyAction.Confirm: VRKeyboard.Instance.Confirm(); break;
            case KeyAction.Cancel: VRKeyboard.Instance.Cancel(); break;
        }
    }
}
