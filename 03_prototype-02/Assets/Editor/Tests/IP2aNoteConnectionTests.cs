using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Play-mode tests for grabbing a note's handle and connecting it to another note.
// A stand-in XRDirectInteractor plays the controller: the tests select the handle with
// it, move it, and deselect, which is the same sequence XRI runs for a real grip.
public class IP2aNoteConnectionTests
{
    const string ScenePath = "Assets/Scenes/IP2a.unity";

    NoteCreationPanel panel;
    XRInteractionManager manager;
    XRDirectInteractor hand;

    [UnityTest]
    public IEnumerator HandleColliderBelongsToHandleNotNote()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData note = SpawnNote(Vector3.zero);
        yield return null;

        XRSimpleInteractable handle = HandleOf(note);
        Assert.IsTrue(manager.TryGetInteractableForCollider(handle.GetComponent<Collider>(), out IXRInteractable owner));
        Assert.AreEqual(handle, owner, "The handle's collider should belong to the handle, not the note's grab.");

        XRGrabInteractable grab = note.GetComponent<XRGrabInteractable>();
        Assert.AreEqual(1, grab.colliders.Count, "The note's grab should only use its own box collider.");
        Assert.AreEqual(note.GetComponent<Collider>(), grab.colliders[0]);

        Assert.IsFalse(XRButtonPressRouter.IsPressable(handle), "Trigger/mouse click must not press a handle.");
        panel.OpenPanel();
        Assert.IsTrue(XRButtonPressRouter.IsPressable(panel.transform.Find("CreateButton").GetComponent<XRSimpleInteractable>()),
            "Normal buttons should still be pressable.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DraggingHandleOntoAnotherNoteConnectsThem()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        // Far enough apart that the old "search around the start handle" logic would fail.
        Assert.IsNotNull(panel.headTransform, "panel.headTransform");
        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.7f);
        Physics.SyncTransforms();

        XRSimpleInteractable handleA = HandleOf(a);
        XRSimpleInteractable handleB = HandleOf(b);

        hand.transform.position = handleA.transform.position;
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        ConnectionLine line = LineFrom(handleA);
        Assert.IsNotNull(line, "Grabbing a handle should start a line.");
        Assert.IsNull(line.pointB, "The line should be loose while dragging.");

        // Drop 5 cm off one of B's handles: inside its 8 cm magnet radius.
        hand.transform.position = handleB.transform.position + Vector3.up * 0.05f;
        Physics.SyncTransforms();
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);

        Assert.AreEqual(handleB.transform, line.pointB, "Dropping near B's handle should snap to it.");
        Assert.IsTrue(TourManager.tourPath.Contains(a.transform) && TourManager.tourPath.Contains(b.transform),
            "Both notes should be registered for Tour Mode.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DroppingInEmptySpaceOrOnOwnNoteMakesNoConnection()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        Physics.SyncTransforms();
        XRSimpleInteractable handleA = HandleOf(a);

        // Empty space.
        hand.transform.position = handleA.transform.position;
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        hand.transform.position = a.transform.position + Vector3.up * 2f;
        Physics.SyncTransforms();
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        yield return null;
        Assert.AreEqual(0, LinesFrom(handleA), "Dropping in empty space should remove the line.");

        // Its own note.
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        hand.transform.position = a.transform.position;
        Physics.SyncTransforms();
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        yield return null;
        Assert.AreEqual(0, LinesFrom(handleA), "A note shouldn't connect to itself.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator OneHandleCanHaveSeveralConnections()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.7f);
        NoteData c = SpawnNote(panel.headTransform.up * 0.5f);
        Physics.SyncTransforms();
        XRSimpleInteractable handleA = HandleOf(a);

        foreach (NoteData target in new[] { b, c })
        {
            hand.transform.position = handleA.transform.position;
            manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
            hand.transform.position = HandleOf(target).transform.position;
            manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)handleA);
        }
        yield return null;

        Assert.AreEqual(2, LinesFrom(handleA), "The same handle should hold two lines.");
        Assert.AreEqual(2, handleA.GetComponent<VRHandleConnector>().ConnectionCount);
        Assert.AreEqual(VRHandleConnector.State.Idle, handleA.GetComponent<VRHandleConnector>().VisualState,
            "A handle with lines should stay visible even when its note isn't hovered.");

        yield return new ExitPlayMode();
    }

    [Test]
    public void EachShapeHasAHandlePerEdge()
    {
        var expected = new System.Collections.Generic.Dictionary<string, int>
            { { "Rectangle", 4 }, { "Circle", 4 }, { "Triangle", 3 }, { "Hexagon", 6 }, { "Star", 5 } };
        foreach (var pair in expected)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Notes/{pair.Key}Note.prefab");
            VRHandleConnector[] handles = prefab.GetComponentsInChildren<VRHandleConnector>(true);
            Assert.AreEqual(pair.Value, handles.Length, pair.Key + " handle count");
            Assert.IsNotNull(prefab.GetComponent<NoteHandles>(), pair.Key + " needs NoteHandles");

            Bounds body = prefab.GetComponent<BoxCollider>().bounds;
            foreach (VRHandleConnector h in handles)
            {
                Assert.AreEqual(0.02f, h.visual.localScale.x, 0.0001f, "Handle dot should be 2/3 of the old 0.03.");
                // Every handle sits outside the note's text: beyond the inner text-safe area.
                Assert.Greater(((Vector2)h.transform.localPosition).magnitude, 0.05f, pair.Key + " handle too close to the centre");
            }
        }
    }

    void Setup()
    {
        panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        manager = Object.FindFirstObjectByType<XRInteractionManager>();
        Assert.IsNotNull(panel);
        Assert.IsNotNull(manager);
        TourManager.tourPath.Clear();

        // Placed well away from the notes so it doesn't hover anything by itself.
        GameObject handGO = new GameObject("TestHand");
        handGO.transform.position = new Vector3(0f, -50f, 0f);
        SphereCollider trigger = handGO.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.01f;
        hand = handGO.AddComponent<XRDirectInteractor>();
        hand.interactionManager = manager;
    }

    // Spawns a note via the panel's test-note path, then moves it by `offset` before
    // physics runs, so two test notes never start overlapping.
    NoteData SpawnNote(Vector3 offset)
    {
        NoteData[] before = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);
        panel.CreateTestNote();
        NoteData note = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None).Single(n => !before.Contains(n));
        note.transform.position += offset;
        return note;
    }

    static XRSimpleInteractable HandleOf(NoteData note)
    {
        Assert.IsTrue(note != null, "Note was destroyed.");
        VRHandleConnector connector = note.GetComponentInChildren<VRHandleConnector>(true);
        Assert.IsNotNull(connector, $"{note.name} has no VRHandleConnector (children: {string.Join(", ", note.GetComponentsInChildren<Transform>(true).Select(t => t.name))}).");
        return connector.GetComponent<XRSimpleInteractable>();
    }

    // Note for these tests: don't capture local variables in lambdas inside a [UnityTest]
    // that enters Play mode. The domain reload rebuilds the test's state and the lambda's
    // hidden capture object comes back null. Use helpers like these instead.
    static ConnectionLine LineFrom(XRSimpleInteractable handle) =>
        Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).SingleOrDefault(l => l.pointA == handle.transform);

    static int LinesFrom(XRSimpleInteractable handle) =>
        Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Count(l => l.pointA == handle.transform);
}
