using UnityEngine;
using System.Collections.Generic;

public static class TourManager
{
    public static List<Transform> tourPath = new List<Transform>();

    public static void RegisterConnection(Transform noteA, Transform noteB)
    {
        if (noteA != null && !tourPath.Contains(noteA)) tourPath.Add(noteA);
        if (noteB != null && !tourPath.Contains(noteB)) tourPath.Add(noteB);
    }
}