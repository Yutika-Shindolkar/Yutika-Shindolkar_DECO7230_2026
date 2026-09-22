using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// Poke this object to cycle through a preset list of labels.
// This is IP2a's simplified stand-in for freeform text entry (used for both note labels
// and connection labels), full VR keyboard text entry is deferred to IP2b, it needs its own
// on-screen keyboard sample and is a much bigger build than the time available here allows.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PresetLabelCycler : MonoBehaviour
{
    public TMP_Text targetText;
    public string[] presetLabels = { "Idea", "Task", "Question", "Related to", "Leads to", "Blocks" };

    int currentIndex = 0;
    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable() => interactable.selectEntered.AddListener(OnPoked);
    void OnDisable() => interactable.selectEntered.RemoveListener(OnPoked);

    void OnPoked(SelectEnterEventArgs args)
    {
        if (presetLabels == null || presetLabels.Length == 0 || targetText == null) return;
        currentIndex = (currentIndex + 1) % presetLabels.Length;
        targetText.text = presetLabels[currentIndex];
    }
}
