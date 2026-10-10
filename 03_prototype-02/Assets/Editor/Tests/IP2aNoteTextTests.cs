using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Note text sizing: notes grow to fit longer text at a constant letter size, and connection
// labels use the same letter size as note text.
public class IP2aNoteTextTests
{
    const string ScenePath = "Assets/Scenes/IP2a.unity";

    const string Paragraph = "This is a longer note, the kind of thing someone writes when they want to capture " +
        "a whole idea at once instead of a single word. It should make the note bigger, not the text smaller.";

    [UnityTest]
    public IEnumerator NotesGrowToFitTextAtTheSameLetterSize()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteData note = SpawnNote();
        note.SetLabel("hi");
        Assert.AreEqual(1f, note.transform.localScale.x, 0.001f, "Short text should keep the normal note size.");
        float shortFont = WorldFontSize(note);

        note.SetLabel(Paragraph);
        float scale = note.transform.localScale.x;
        Assert.Greater(scale, 1.2f, "A paragraph should make the note noticeably bigger.");
        Assert.LessOrEqual(scale, note.maxScale + 0.001f);
        note.noteText.ForceMeshUpdate();
        Assert.IsFalse(note.noteText.isTextOverflowing, "The paragraph should fit inside the grown note.");
        Assert.AreEqual(shortFont, WorldFontSize(note), shortFont * 0.01f,
            "Letters should stay the same size when the note grows.");

        note.SetLabel("ok");
        Assert.AreEqual(1f, note.transform.localScale.x, 0.001f, "Shortening the text should shrink the note back.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ConnectionLabelMatchesNoteTextSize()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteData note = SpawnNote();
        note.SetLabel("hi");

        // A label from the real connection label prefab, showing the same text.
        VRHandleConnector connector = note.GetComponentInChildren<VRHandleConnector>();
        GameObject label = Object.Instantiate(connector.labelPrefab);
        TMP_Text labelText = label.GetComponentInChildren<VRLabelEdit>(true).GetComponent<TMP_Text>();
        labelText.gameObject.SetActive(true);
        labelText.text = "hi";

        float noteLetters = WorldLetterHeight(note.noteText);
        float labelLetters = WorldLetterHeight(labelText);
        Debug.Log($"TEXT SIZE: note letters {noteLetters * 100f:0.00} cm, label letters {labelLetters * 100f:0.00} cm");
        Assert.AreEqual(noteLetters, labelLetters, noteLetters * 0.1f,
            $"Connection label letters ({labelLetters * 100f:0.00} cm) should match note text ({noteLetters * 100f:0.00} cm).");

        yield return new ExitPlayMode();
    }

    static NoteData SpawnNote()
    {
        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        NoteData[] before = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);
        panel.CreateTestNote();
        foreach (NoteData n in Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None))
            if (System.Array.IndexOf(before, n) < 0) return n;
        Assert.Fail("No note spawned.");
        return null;
    }

    // Note text font size in world units (canvas font size times the note's scale).
    static float WorldFontSize(NoteData note) => note.noteText.fontSize * note.transform.localScale.x;

    // Height of the rendered text in world metres (use with single-line text).
    static float WorldLetterHeight(TMP_Text text)
    {
        text.ForceMeshUpdate();
        return text.textBounds.size.y * text.transform.lossyScale.y;
    }
}
