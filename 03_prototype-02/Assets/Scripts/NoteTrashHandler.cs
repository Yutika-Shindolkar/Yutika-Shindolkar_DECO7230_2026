using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Shows the trash bin and the archive tray (on the floor ahead of the player) while this note
// is being held, lights up whichever one the note is over, and acts on release there: over the
// bin the note is deleted, over the tray it is archived. The holding controller gets a light
// tick when the note moves over either one, two strong thumps when it is deleted, and two
// soft rising pulses when it is archived.
[RequireComponent(typeof(XRGrabInteractable))]
public class NoteTrashHandler : MonoBehaviour
{
    XRGrabInteractable grab;
    NoteDropZone overZone;

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

    // The bin or tray the note is currently over, if any.
    NoteDropZone ZoneUnderNote()
    {
        if (FootTrashBin.Instance != null && FootTrashBin.Instance.IsPositionOver(transform.position))
            return FootTrashBin.Instance;
        if (ArchiveTray.Instance != null && ArchiveTray.Instance.IsPositionOver(transform.position))
            return ArchiveTray.Instance;
        return null;
    }

    void Update()
    {
        NoteDropZone zone = grab.isSelected ? ZoneUnderNote() : null;
        if (zone != null)
        {
            zone.ShowReady();
            if (zone != overZone) zone.PulseHover(grab.firstInteractorSelecting as Object);
        }
        overZone = zone;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        overZone = null;
        if (FootTrashBin.Instance != null) FootTrashBin.Instance.RegisterHoldStart();
        if (ArchiveTray.Instance != null) ArchiveTray.Instance.RegisterHoldStart();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        NoteDropZone zone = ZoneUnderNote();
        if (FootTrashBin.Instance != null) FootTrashBin.Instance.RegisterHoldEnd();
        if (ArchiveTray.Instance != null) ArchiveTray.Instance.RegisterHoldEnd();

        if (zone is FootTrashBin bin)
        {
            bin.PulseDelete(args.interactorObject as Object);
            Destroy(gameObject);
        }
        else if (zone is ArchiveTray tray)
        {
            tray.PulseSuccess(args.interactorObject as Object);
            tray.Archive(GetComponent<NoteData>());
        }
    }
}
