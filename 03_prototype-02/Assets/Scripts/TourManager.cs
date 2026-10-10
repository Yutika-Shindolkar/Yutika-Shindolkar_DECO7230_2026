using UnityEngine;
using System.Collections.Generic;

// The notes Tour Mode lists: every note that has at least one connection.
public static class TourManager
{
    public static List<Transform> tourPath = new List<Transform>();

    public static void RegisterConnection(Transform noteA, Transform noteB)
    {
        if (noteA != null && !tourPath.Contains(noteA)) tourPath.Add(noteA);
        if (noteB != null && !tourPath.Contains(noteB)) tourPath.Add(noteB);
    }

    // After a connection is broken: drop notes that no longer have any connection.
    // `ignore` is the line being removed (it still exists until the end of the frame).
    public static void PruneUnconnected(ConnectionLine ignore)
    {
        tourPath.RemoveAll(note => note == null || !HasConnection(note, ignore));
    }

    static bool HasConnection(Transform note, ConnectionLine ignore)
    {
        foreach (ConnectionLine line in ConnectionLine.All)
        {
            if (line == ignore || line.pointA == null || line.pointB == null) continue;
            if (line.pointA.IsChildOf(note) || line.pointB.IsChildOf(note)) return true;
        }
        return false;
    }
}
