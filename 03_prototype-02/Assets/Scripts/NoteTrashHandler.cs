using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Shows the foot-level trash bin while this note is being held, and deletes the note
// if it's released while overlapping the bin. Replaces the earlier static-trash-zone design.
[RequireComponent(typeof(XRGrabInteractable))]
public class NoteTrashHandler : MonoBehaviour
{
    public float trashCheckRadius = 0.2f;

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

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (FootTrashBin.Instance != null) FootTrashBin.Instance.RegisterHoldStart();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (FootTrashBin.Instance == null) return;

        bool overBin = FootTrashBin.Instance.IsPositionOverBin(transform.position, trashCheckRadius);
        FootTrashBin.Instance.RegisterHoldEnd();

        if (overBin)
        {
            Destroy(gameObject);
        }
    }
}
