using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A single trash bin that appears on the floor a couple of metres in front of the player
// while a note is being held. Lower the held note over it and let go to delete the note.
// Placement, drop zone, glow and the hover tick are in NoteDropZone.
public class FootTrashBin : NoteDropZone
{
    public static FootTrashBin Instance;

    [Header("Delete haptics")]
    public float deleteAmplitude = 0.9f;  // two strong thumps when the note is deleted
    public float deleteDuration = 0.12f;
    public float deleteGap = 0.08f;

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }

    public bool IsPositionOverBin(Vector3 worldPos) => IsPositionOver(worldPos);

    // Run from here rather than the note, since the note is destroyed before the second
    // delete thump plays.
    public void PulseDelete(Object interactor)
    {
        if (interactor is XRBaseInputInteractor input) StartCoroutine(DoubleThump(input));
    }

    IEnumerator DoubleThump(XRBaseInputInteractor input)
    {
        input.SendHapticImpulse(deleteAmplitude, deleteDuration);
        yield return new WaitForSeconds(deleteDuration + deleteGap);
        if (input != null) input.SendHapticImpulse(deleteAmplitude, deleteDuration);
    }
}
