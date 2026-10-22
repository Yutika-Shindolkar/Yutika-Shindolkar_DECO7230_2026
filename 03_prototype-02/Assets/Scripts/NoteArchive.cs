using System;
using System.Collections.Generic;
using UnityEngine;

// The notes that have been put in the archive tray. They are kept for the rest of the
// session (not saved to disk): switched off and parked under a hidden holder, so they keep
// their text, shape and media and can be brought back later. An archived note has no
// connection lines and is not in Tour Mode.
public static class NoteArchive
{
    public static readonly List<NoteData> Notes = new List<NoteData>();
    public static event Action Changed;

    static Transform holder;

    // Editor play sessions can keep static state between runs.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Notes.Clear();
        holder = null;
        Changed = null;
    }

    public static void Add(NoteData note)
    {
        if (note == null || Notes.Contains(note)) return;

        note.RemoveConnections();
        note.transform.SetParent(Holder, true);
        note.gameObject.SetActive(false);
        Notes.Add(note);
        Changed?.Invoke();
    }

    // Brings an archived note back into the space, restoreDistance ahead of the player at eye
    // height, turned to face them. If another note is already there, it is nudged sideways
    // (right, then left, a little further each time) until it clears. Its connections are not
    // brought back.
    public const float RestoreDistance = 1.2f;
    const float NudgeStep = 0.05f;
    const float DepthClearance = 0.25f; // notes this far apart front to back don't count as overlapping

    public static void Restore(NoteData note, Transform head)
    {
        if (note == null || head == null || !Notes.Remove(note)) return;

        Vector3 forward = head.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 spot = head.position + forward * RestoreDistance;

        note.transform.SetParent(null, true);
        note.transform.rotation = Quaternion.LookRotation(forward);
        note.transform.position = spot;

        NoteData[] others = UnityEngine.Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);
        for (int i = 0; i < 80; i++)
        {
            // 0, +1, -1, +2, -2, ... steps along the player's right.
            float offset = ((i + 1) / 2) * NudgeStep * (i % 2 == 1 ? 1f : -1f);
            note.transform.position = spot + right * offset;
            if (!Overlaps(note, others, right)) break;
        }

        note.gameObject.SetActive(true);
        Changed?.Invoke();
    }

    static bool Overlaps(NoteData note, NoteData[] others, Vector3 right)
    {
        foreach (NoteData other in others)
        {
            if (other == note || !other.gameObject.activeInHierarchy) continue;
            Vector3 d = other.transform.position - note.transform.position;
            float halfW = HalfSize(note).x + HalfSize(other).x;
            float halfH = HalfSize(note).y + HalfSize(other).y;
            if (Mathf.Abs(Vector3.Dot(d, right)) < halfW && Mathf.Abs(d.y) < halfH
                && Mathf.Abs(Vector3.Dot(d, Vector3.Cross(right, Vector3.up))) < DepthClearance)
                return true;
        }
        return false;
    }

    // Half the note's width and height in metres (the grab box, at the note's scale).
    static Vector2 HalfSize(NoteData n)
    {
        if (n.noteCollider == null) return new Vector2(0.15f, 0.15f);
        Vector3 s = n.noteCollider.transform.lossyScale;
        return new Vector2(n.noteCollider.size.x * Mathf.Abs(s.x), n.noteCollider.size.y * Mathf.Abs(s.y)) * 0.5f;
    }

    static Transform Holder
    {
        get
        {
            if (holder == null)
            {
                GameObject go = new GameObject("ArchivedNotes");
                go.SetActive(false);
                holder = go.transform;
            }
            return holder;
        }
    }
}
