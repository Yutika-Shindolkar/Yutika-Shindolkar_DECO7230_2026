using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Shows the trash bin (on the floor ahead of the player) while this note is being held,
// lights the bin up while the note is over it, and deletes the note if it's released there.
[RequireComponent(typeof(XRGrabInteractable))]
public class NoteTrashHandler : MonoBehaviour
{
    XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void Update()
    {
        FootTrashBin bin = FootTrashBin.Instance;
        if (grab.isSelected && bin != null && bin.IsPositionOverBin(transform.position))
            bin.ShowReady();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (FootTrashBin.Instance != null) FootTrashBin.Instance.RegisterHoldStart();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (FootTrashBin.Instance == null) return;

        bool overBin = FootTrashBin.Instance.IsPositionOverBin(transform.position);
        FootTrashBin.Instance.RegisterHoldEnd();

        if (overBin)
        {
            Destroy(gameObject);
        }
    }
}
