using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Play-mode smoke test of the clap -> note creation panel flow in IP2a.unity.
// Run from Window > General > Test Runner > EditMode, or headless with
// Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode
//
// Buttons are pressed by firing their selectEntered event, which is what a trigger,
// grip, poke or Editor mouse click all end up doing (see XRButtonPressRouter).
public class IP2aPanelFlowTests
{
    const string ScenePath = "Assets/Scenes/IP2a.unity";

    ClapGestureDetector clap;
    NoteCreationPanel panel;

    [UnityTest]
    public IEnumerator ClapTypeAndCreateNote()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode(); // must be yielded from the test method itself
        yield return null;
        yield return null;
        FindSceneObjects();

        Assert.IsFalse(panel.gameObject.activeSelf, "Panel should start hidden.");
        Assert.IsNotNull(VRKeyboard.Instance, "VRKeyboard.Instance should be set at Play start.");
        Assert.IsFalse(VRKeyboard.Instance.gameObject.activeSelf, "Keyboard should start hidden.");
        Assert.AreEqual(1, Object.FindObjectsByType<VRKeyboard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            "There should be exactly one VRKeyboard.");
        Assert.IsNotNull(clap.leftHand, "Clap left hand not wired.");
        Assert.IsNotNull(clap.rightHand, "Clap right hand not wired.");

        clap.onClap.Invoke();
        Assert.IsTrue(panel.gameObject.activeSelf, "Clap should open the panel.");

        Press(panel.transform, "CircleButton");
        Assert.AreEqual(panel.shapeColors[(int)NoteShape.Circle], panel.noteFieldBackground.color,
            "Picking Circle should tint the text field.");

        Press(panel.transform, "NoteTextField");
        Assert.IsTrue(VRKeyboard.Instance.gameObject.activeSelf, "Pressing the text field should open the keyboard.");
        Press(VRKeyboard.Instance.transform, "Key_H");
        Press(VRKeyboard.Instance.transform, "Key_I");
        Press(VRKeyboard.Instance.transform, "Key_Confirm");
        Assert.IsFalse(VRKeyboard.Instance.gameObject.activeSelf, "Confirm should close the keyboard.");
        Assert.AreEqual("hi", panel.noteTextDisplay.text, "Typed text should show in the field.");
        Assert.IsFalse(panel.notePlaceholder.activeSelf, "Placeholder should hide once there's text.");

        NoteData[] notesBefore = AllNotes();
        Press(panel.transform, "CreateButton");
        yield return null;

        Assert.IsFalse(panel.gameObject.activeSelf, "Create should close the panel.");
        Assert.AreEqual(notesBefore.Length + 1, AllNotes().Length, "Create should spawn one note.");
        NoteData note = NewNote(notesBefore);
        Assert.AreEqual(NoteShape.Circle, note.shape, "Note should have the picked shape.");
        Assert.AreEqual("hi", note.noteText.text, "Note should carry the typed text.");

        Vector3 toNote = note.transform.position - panel.headTransform.position;
        Assert.Greater(Vector3.Dot(note.transform.forward, toNote), 0f, "Note's front should face the player.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DiscardResetsAndTestNoteSpawns()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode(); // must be yielded from the test method itself
        yield return null;
        yield return null;
        FindSceneObjects();

        clap.onClap.Invoke();
        Press(panel.transform, "NoteTextField");
        Press(VRKeyboard.Instance.transform, "Key_A");
        Press(VRKeyboard.Instance.transform, "Key_Confirm");
        Press(panel.transform, "DiscardButton");
        Assert.IsFalse(panel.gameObject.activeSelf, "Discard should close the panel.");

        clap.onClap.Invoke();
        Assert.AreEqual("", panel.noteTextDisplay.text, "Reopening should clear the old text.");
        Assert.IsTrue(panel.notePlaceholder.activeSelf, "Placeholder should be back.");

        // Closing the panel while the keyboard is open for it should close the keyboard too.
        Press(panel.transform, "NoteTextField");
        Press(panel.transform, "DiscardButton");
        Assert.IsFalse(VRKeyboard.Instance.gameObject.activeSelf, "Discard should close the panel's keyboard.");

        clap.onClap.Invoke();
        NoteData[] notesBefore = AllNotes();
        Transform testButton = panel.transform.Find("TestNoteButton");
        Assert.IsTrue(testButton.gameObject.activeInHierarchy, "Test note button should show in the Editor.");
        Press(panel.transform, "TestNoteButton");
        yield return null;
        Assert.AreEqual(notesBefore.Length + 1, AllNotes().Length, "Test note button should spawn a note.");
        Assert.AreEqual("dummy text", NewNote(notesBefore).noteText.text);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator PanelButtonsAreReachableByRay()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode(); // must be yielded from the test method itself
        yield return null;
        yield return null;
        FindSceneObjects();
        clap.onClap.Invoke();
        yield return null;

        // A ray from the camera to each button should hit that button first, so nothing
        // (another button, the mic, a stray collider) blocks the controller ray or mouse.
        string[] buttons = { "TestNoteButton", "RectangleButton", "CircleButton", "TriangleButton", "HexagonButton",
            "StarButton", "NoteTextField", "DiscardButton", "CreateButton" };
        Vector3 eye = panel.headTransform.position;
        foreach (string name in buttons)
        {
            Collider col = panel.transform.Find(name).GetComponent<Collider>();
            Assert.IsNotNull(col, name + " has no collider.");
            Vector3 target = col.bounds.center;
            RaycastHit[] hits = Physics.RaycastAll(eye, (target - eye).normalized, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            RaycastHit first = hits.Where(h => h.collider.GetComponentInParent<XRBaseInteractable>() != null)
                .OrderBy(h => h.distance).FirstOrDefault();
            Assert.IsNotNull(first.collider, name + " was not hit by a ray from the head.");
            Assert.AreEqual(col.GetComponentInParent<XRBaseInteractable>(), first.collider.GetComponentInParent<XRBaseInteractable>(),
                $"Ray to {name} hit {first.collider.name} first.");
        }

        yield return new ExitPlayMode();
    }

    void FindSceneObjects()
    {
        clap = Object.FindFirstObjectByType<ClapGestureDetector>();
        panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Assert.IsNotNull(clap, "No ClapGestureDetector in the scene.");
        Assert.IsNotNull(panel, "No NoteCreationPanel in the scene.");
    }

    static void Press(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        Assert.IsNotNull(t, $"'{childName}' not found under '{parent.name}'.");
        XRSimpleInteractable button = t.GetComponent<XRSimpleInteractable>();
        Assert.IsNotNull(button, $"'{childName}' has no XRSimpleInteractable.");
        Assert.IsTrue(button.isActiveAndEnabled, $"'{childName}' is not pressable right now.");
        button.selectEntered.Invoke(new SelectEnterEventArgs { interactableObject = button });
    }

    static NoteData[] AllNotes() => Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);

    static NoteData NewNote(NoteData[] before) => AllNotes().Single(n => !before.Contains(n));
}
