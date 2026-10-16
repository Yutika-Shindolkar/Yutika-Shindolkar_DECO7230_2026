using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Shows the trash bin (on the floor ahead of the player) while this note is being held,
// lights the bin up while the note is over it, and deletes the note if it's released there.
// The holding controller gets a light tick when the note moves over the bin and two strong
// thumps when it's deleted.
[RequireComponent(typeof(XRGrabInteractable))]
public class NoteTrashHandler : MonoBehaviour
{
    XRGrabInteractable grab;
    bool wasOverBin;

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
        bool over = grab.isSelected && bin != null && bin.IsPositionOverBin(transform.position);
        if (over)
        {
            bin.ShowReady();
            if (!wasOverBin) bin.PulseHover(grab.firstInteractorSelecting as Object);
        }
        wasOverBin = over;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        wasOverBin = false;
        if (FootTrashBin.Instance != null) FootTrashBin.Instance.RegisterHoldStart();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (FootTrashBin.Instance == null) return;

        bool overBin = FootTrashBin.Instance.IsPositionOverBin(transform.position);
        FootTrashBin.Instance.RegisterHoldEnd();

        if (overBin)
        {
            FootTrashBin.Instance.PulseDelete(args.interactorObject as Object);
            Destroy(gameObject);
        }
    }
}
