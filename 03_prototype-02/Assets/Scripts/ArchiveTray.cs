using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A flat serving tray that appears on the floor beside the trash bin while a note is being
// held. Lower the held note over it and let go to archive the note: it leaves the space and
// is kept in NoteArchive. Placement, drop zone, glow and the hover tick are in NoteDropZone.
public class ArchiveTray : NoteDropZone
{
    public static ArchiveTray Instance;

    [Header("Tray")]
    public float sideOffset = 0.8f;   // metres to the player's right of the trash bin

    [Header("Archive haptics")]
    public float successAmplitude = 0.55f; // two soft rising pulses when the note is archived
    public float successDuration = 0.07f;
    public float successGap = 0.06f;

    protected override Vector3 PlacementOffset => new Vector3(sideOffset, 0f, 0f);

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }

    public bool IsPositionOverTray(Vector3 worldPos) => IsPositionOver(worldPos);

    // Stores the note in the archive. Waits a frame so the note is not switched off in the
    // middle of the let-go that is still being processed.
    public void Archive(NoteData note)
    {
        StartCoroutine(ArchiveNextFrame(note));
    }

    IEnumerator ArchiveNextFrame(NoteData note)
    {
        yield return null;
        NoteArchive.Add(note);
    }

    public void PulseSuccess(Object interactor)
    {
        if (interactor is XRBaseInputInteractor input) StartCoroutine(RisingPair(input));
    }

    IEnumerator RisingPair(XRBaseInputInteractor input)
    {
        input.SendHapticImpulse(successAmplitude, successDuration);
        yield return new WaitForSeconds(successDuration + successGap);
        if (input != null) input.SendHapticImpulse(Mathf.Min(1f, successAmplitude * 1.5f), successDuration * 1.5f);
    }
}
