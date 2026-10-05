using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Play-mode tests for what happens after notes exist: moving them, labelling a
// connection, deleting in the foot trash bin, and Tour Mode. A stand-in
// XRDirectInteractor plays the controller (see IP2aNoteConnectionTests).
//
// Don't capture local variables in lambdas inside these tests: entering Play mode
// reloads the domain and the lambda's capture object comes back null.
public class IP2aWhiteboardTests
{
    const string ScenePath = "Assets/Scenes/IP2a.unity";

    NoteCreationPanel panel;
    XRInteractionManager manager;
    XRDirectInteractor hand;

    [Test]
    public void NotePrefabsStayPutAndMoveAtRayDistance()
    {
        foreach (string shape in new[] { "Rectangle", "Circle", "Triangle", "Hexagon", "Star" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Notes/{shape}Note.prefab");
            Assert.IsTrue(prefab.GetComponent<Rigidbody>().isKinematic, shape + " note should be kinematic.");
            Assert.AreEqual(InteractableFarAttachMode.Far, prefab.GetComponent<XRGrabInteractable>().farAttachMode,
                shape + " note should stay at ray distance when grabbed from afar.");
        }
    }

    [UnityTest]
    public IEnumerator SceneUsesXRUIInput()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        Assert.IsNotNull(es.GetComponent<XRUIInputModule>(), "EventSystem needs XRUIInputModule for controller UI input.");
        Assert.IsNull(es.GetComponent<InputSystemUIInputModule>(), "The Input System UI module would compete with XRUIInputModule.");

        VRTourMode tour = Object.FindFirstObjectByType<VRTourMode>();
        Assert.IsNotNull(tour.menuCanvas.GetComponent<TrackedDeviceGraphicRaycaster>(), "Tour menu needs a TrackedDeviceGraphicRaycaster.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LabelAConnectionAndEditIt()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.6f);
        Physics.SyncTransforms();
        ConnectionLine line = Connect(a, b);
        Assert.IsNotNull(line.pointB, "Notes should be connected.");

        Transform plus = line.GetComponentInChildren<VRPlusButtonClick>(true).transform;
        Transform labelText = line.GetComponentInChildren<VRLabelEdit>(true).transform;
        Assert.IsNotNull(plus, "Connection should have a + button.");
        Assert.IsFalse(labelText.gameObject.activeSelf, "Label text should be hidden until set.");

        Press(plus);
        Assert.IsTrue(VRKeyboard.Instance.gameObject.activeSelf, "+ should open the keyboard.");
        Press(VRKeyboard.Instance.transform.Find("Key_C"));
        Press(VRKeyboard.Instance.transform.Find("Key_A"));
        Press(VRKeyboard.Instance.transform.Find("Key_U"));
        Press(VRKeyboard.Instance.transform.Find("Key_S"));
        Press(VRKeyboard.Instance.transform.Find("Key_E"));
        Press(VRKeyboard.Instance.transform.Find("Key_Confirm"));

        Assert.IsTrue(labelText.gameObject.activeSelf, "Label should show after Confirm.");
        Assert.AreEqual("cause", labelText.GetComponent<TMP_Text>().text);
        Assert.IsFalse(plus.gameObject.activeSelf, "+ should hide once labelled.");

        Press(labelText);
        Assert.IsTrue(VRKeyboard.Instance.gameObject.activeSelf, "Pressing the label should reopen the keyboard.");
        Assert.AreEqual("cause", VRKeyboard.Instance.previewText.text, "Keyboard should start with the current label.");
        Press(VRKeyboard.Instance.transform.Find("Key_Cancel"));
        Assert.AreEqual("cause", labelText.GetComponent<TMP_Text>().text, "Cancel shouldn't change the label.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TrashDeletesNoteAndItsConnections()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.6f);
        Physics.SyncTransforms();
        Connect(a, b);
        Assert.AreEqual(1, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length);

        FootTrashBin bin = FootTrashBin.Instance;
        Assert.IsNotNull(bin, "No foot trash bin.");
        XRGrabInteractable grabA = a.GetComponent<XRGrabInteractable>();
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);

        Vector3 head = panel.headTransform.position;
        Assert.AreEqual(head.x, bin.transform.position.x, 0.001f, "Bin should be under the player's head.");
        Assert.AreEqual(head.z, bin.transform.position.z, 0.001f, "Bin should be under the player's head.");
        Assert.IsTrue(bin.GetComponent<Renderer>().enabled, "Bin should show while a note is held.");

        a.transform.position = bin.transform.position + Vector3.up * 0.05f;
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);
        // Destroy() takes effect at the end of the frame, for the note and then its lines.
        yield return null;
        yield return null;

        Assert.IsTrue(a == null, "Note dropped on the bin should be deleted.");
        Assert.AreEqual(0, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length,
            "Its connection line and label should be removed too.");
        Assert.AreEqual(1, TourManager.tourPath.Count, "Deleted note should leave the Tour list.");
        Assert.IsFalse(bin.GetComponent<Renderer>().enabled, "Bin should hide again.");

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TourJumpsInFrontOfNoteAndExitReturns()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 2f);
        b.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // turned sideways, so a fixed approach side would be wrong
        Physics.SyncTransforms();
        Connect(a, b);

        VRTourMode tour = Object.FindFirstObjectByType<VRTourMode>();
        Vector3 rigStart = tour.xrOriginRoot.position;

        tour.OpenMenu();
        yield return null;
        Button[] items = tour.listContent.GetComponentsInChildren<Button>();
        Assert.AreEqual(2, items.Length, "Menu should list both connected notes.");
        Assert.AreEqual("dummy text", items[1].GetComponentInChildren<TMP_Text>().text);

        items[1].onClick.Invoke(); // jump to note b
        Assert.IsTrue(VRTourMode.IsTouring);
        Assert.IsFalse(tour.IsMenuOpen, "Menu should close on jump.");
        Assert.IsTrue(tour.exitTourButton.activeSelf, "Exit Tour should show while touring.");

        Vector3 toHead = panel.headTransform.position - b.transform.position;
        toHead.y = 0f;
        Assert.AreEqual(tour.viewDistance, toHead.magnitude, 0.01f, "Should land viewDistance from the note.");
        Assert.Greater(Vector3.Dot(toHead.normalized, -b.transform.forward), 0.99f, "Should land in front of the note's face.");
        Assert.AreEqual(rigStart.y, tour.xrOriginRoot.position.y, 0.0001f, "Jump shouldn't change floor height.");

        tour.ExitTour();
        Assert.IsFalse(VRTourMode.IsTouring);
        Assert.AreEqual(rigStart, tour.xrOriginRoot.position, "Exit Tour should return the player.");

        yield return new ExitPlayMode();
    }

    void Setup()
    {
        panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        manager = Object.FindFirstObjectByType<XRInteractionManager>();
        Assert.IsNotNull(panel);
        Assert.IsNotNull(manager);
        TourManager.tourPath.Clear();

        GameObject handGO = new GameObject("TestHand");
        handGO.transform.position = new Vector3(0f, -50f, 0f);
        SphereCollider trigger = handGO.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.01f;
        hand = handGO.AddComponent<XRDirectInteractor>();
        hand.interactionManager = manager;
    }

    NoteData SpawnNote(Vector3 offset)
    {
        NoteData[] before = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);
        panel.CreateTestNote();
        NoteData note = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None).Single(n => !before.Contains(n));
        note.transform.position += offset;
        return note;
    }

    // Grabs a's handle with the test hand, drags it onto b and lets go.
    ConnectionLine Connect(NoteData a, NoteData b)
    {
        XRSimpleInteractable handle = a.GetComponentInChildren<VRHandleConnector>().GetComponent<XRSimpleInteractable>();
        hand.transform.position = handle.transform.position;
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handle);
        hand.transform.position = b.transform.position;
        Physics.SyncTransforms();
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)handle);
        hand.transform.position = new Vector3(0f, -50f, 0f);
        return Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Single(l => l.pointA == handle.transform);
    }

    static void Press(Transform t)
    {
        Assert.IsNotNull(t, "Button not found.");
        XRSimpleInteractable button = t.GetComponent<XRSimpleInteractable>();
        Assert.IsTrue(XRButtonPressRouter.IsPressable(button), $"'{t.name}' is not pressable right now.");
        button.selectEntered.Invoke(new SelectEnterEventArgs { interactableObject = button });
    }
}
