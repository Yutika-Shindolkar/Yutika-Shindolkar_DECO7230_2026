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
