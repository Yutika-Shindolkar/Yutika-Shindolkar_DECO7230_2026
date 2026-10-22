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
    const string ScenePath = "Assets/Scenes/IP2b.unity";

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
    public IEnumerator ImageNoteIsACardWithTextBelow()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();
        Assert.IsNotNull(panel.imagePlaceholder, "No placeholder image assigned.");

        // 4:3 picture: full card width, height from the aspect ratio, text underneath.
        NoteData note = SpawnNote(Vector3.zero);
        note.SetLabel("A caption under the picture");
        note.SetMedia(MediaType.Image, panel.imagePlaceholder);
        Canvas.ForceUpdateCanvases();
        Rect media = LocalRect(note, note.mediaThumbnail.rectTransform);
        Rect text = LocalRect(note, note.noteText.rectTransform);
        Bounds card = note.noteCollider.bounds;
        Vector3 cardSize = note.transform.InverseTransformVector(card.size);
        Assert.AreEqual(note.maxMediaWidth, media.width, 0.001f, "Image should fill the card width.");
        Assert.AreEqual(note.maxMediaWidth * 3f / 4f, media.height, 0.001f, "Image height should follow its aspect ratio.");
        Assert.LessOrEqual(text.yMax, media.yMin, "Text should sit below the image.");
        Assert.AreEqual(note.maxMediaWidth + 2f * note.cardPadding, Mathf.Abs(cardSize.x), 0.001f, "Card should wrap the image.");
        Assert.Greater(Mathf.Abs(cardSize.y), media.height + text.height, "Card should hold the image and the text.");
        Assert.AreEqual(1f, note.transform.localScale.x, 0.0001f, "An image card isn't scaled up like a text note.");
        foreach (VRHandleConnector h in note.GetComponentsInChildren<VRHandleConnector>(true))
        {
            Vector3 p = h.transform.localPosition;
            bool outside = Mathf.Abs(p.x) > Mathf.Abs(cardSize.x) * 0.5f || Mathf.Abs(p.y) > Mathf.Abs(cardSize.y) * 0.5f;
            Assert.IsTrue(outside, $"Handle {h.name} should sit outside the card's edge.");
        }

        // A very tall picture is capped in height (and keeps its shape), on a note-width card.
        NoteData tall = SpawnNote(Vector3.right);
        tall.SetMedia(MediaType.Image, MakeSprite(200, 2000));
        Canvas.ForceUpdateCanvases();
        media = LocalRect(tall, tall.mediaThumbnail.rectTransform);
        Assert.AreEqual(tall.maxMediaHeight, media.height, 0.001f, "Tall image should stop at the max height.");
        Assert.AreEqual(tall.maxMediaHeight * 0.1f, media.width, 0.001f, "Tall image should keep its aspect ratio.");
        Assert.AreEqual(tall.minCardWidth, Mathf.Abs(tall.transform.InverseTransformVector(tall.noteCollider.bounds.size).x), 0.001f);

        // A very wide, very large picture is capped at the max width.
        NoteData wide = SpawnNote(Vector3.left);
        wide.SetMedia(MediaType.Image, MakeSprite(4000, 1000));
        Canvas.ForceUpdateCanvases();
        media = LocalRect(wide, wide.mediaThumbnail.rectTransform);
        Assert.AreEqual(wide.maxMediaWidth, media.width, 0.001f);
        Assert.AreEqual(wide.maxMediaWidth / 4f, media.height, 0.001f);

        yield return new ExitPlayMode();
    }

    // A RectTransform's rectangle in the note's own space (metres).
    static Rect LocalRect(NoteData note, RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        Vector3 min = note.transform.InverseTransformPoint(c[0]);
        Vector3 max = note.transform.InverseTransformPoint(c[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static Sprite MakeSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
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

        // The bin appears on the floor about 2 m ahead of where the player faces, so it's in view.
        Vector3 head = panel.headTransform.position;
        Vector3 toBin = bin.transform.position - head;
        float floorY = GameObject.Find("XR Origin (XR Rig)").transform.position.y;
        Assert.AreEqual(floorY, bin.transform.position.y, 0.01f, "Bin should sit on the floor.");
        Assert.AreEqual(bin.distanceAhead, new Vector2(toBin.x, toBin.z).magnitude, 0.01f, "Bin should be 2 m ahead.");
        Vector3 facing = panel.headTransform.forward; facing.y = 0f;
        Assert.Greater(Vector3.Dot(new Vector3(toBin.x, 0f, toBin.z).normalized, facing.normalized), 0.99f, "Bin should be in front.");
        Assert.IsTrue(bin.visual.enabled, "Bin should show while a note is held.");

        // Red outline: hidden until a held note is over the bin.
        yield return null;
        Assert.IsFalse(bin.outline.enabled, "Outline should stay hidden while the note isn't over the bin.");
        bin.ShowReady();
        yield return null;
        Assert.IsTrue(bin.outline.enabled, "Outline should show while a note is over the bin.");

        // Drop zone: over the bin and low enough counts; at the player's feet or high up doesn't.
        Assert.IsTrue(bin.IsPositionOverBin(bin.transform.position + Vector3.up * 0.5f));
        Assert.IsFalse(bin.IsPositionOverBin(new Vector3(head.x, floorY + 0.1f, head.z)), "Feet shouldn't count any more.");
        Assert.IsFalse(bin.IsPositionOverBin(bin.transform.position + Vector3.up * 1.5f), "Too high above the bin shouldn't count.");

        // Let go at the feet: the note survives.
        a.transform.position = new Vector3(head.x, floorY + 0.1f, head.z);
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);
        yield return null;
        Assert.IsTrue(a != null, "A note dropped at the player's feet shouldn't be deleted.");
        Assert.IsFalse(bin.visual.enabled, "Bin should hide when nothing is held.");
        Assert.IsFalse(bin.outline.enabled, "Outline should hide with the bin.");

        // Pick it up again and let go just above the bin: deleted.
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);
        a.transform.position = bin.transform.position + Vector3.up * 0.5f;
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);
        // Destroy() takes effect at the end of the frame, for the note and then its lines.
        yield return null;
        yield return null;

        Assert.IsTrue(a == null, "Note dropped on the bin should be deleted.");
        Assert.AreEqual(0, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length,
            "Its connection line and label should be removed too.");
        Assert.AreEqual(1, TourManager.tourPath.Count, "Deleted note should leave the Tour list.");
        Assert.IsFalse(bin.visual.enabled, "Bin should hide again.");

        yield return new ExitPlayMode();
    }

    // The archive tray beside the bin: a held note over it lights it up, and letting go there
    // hides the note and keeps it in the archive, with its connections removed.
    [UnityTest]
    public IEnumerator TrayArchivesNoteAndItsConnections()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.6f);
        Physics.SyncTransforms();
        Connect(a, b);

        FootTrashBin bin = FootTrashBin.Instance;
        ArchiveTray tray = ArchiveTray.Instance;
        Assert.IsNotNull(tray, "No archive tray.");
        Assert.IsFalse(tray.visual.enabled, "Tray should be hidden until a note is held.");
        XRGrabInteractable grabA = a.GetComponent<XRGrabInteractable>();
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);

        // Beside the bin, to the player's right, on the floor.
        float floorY = GameObject.Find("XR Origin (XR Rig)").transform.position.y;
        Assert.IsTrue(tray.visual.enabled, "Tray should show while a note is held.");
        Assert.AreEqual(floorY, tray.transform.position.y, 0.01f, "Tray should sit on the floor.");
        Vector3 side = tray.transform.position - bin.transform.position;
        Assert.AreEqual(tray.sideOffset, side.magnitude, 0.01f, "Tray should sit beside the bin.");
        Assert.Greater(Vector3.Dot(side.normalized, bin.transform.right), 0.99f, "Tray should be to the bin's right.");
        Assert.Greater(Vector3.Distance(tray.transform.position, bin.transform.position), bin.dropRadius + tray.dropRadius,
            "The two drop zones shouldn't overlap.");

        // Blue outline: hidden until a held note is over the tray.
        yield return null;
        Assert.IsFalse(tray.outline.enabled, "Outline should stay hidden while the note isn't over the tray.");
        // The held note snaps to the hand, so carry the hand (and the note with it) over the tray.
        hand.transform.position = tray.transform.position + Vector3.up * 0.2f;
        float waited = 0f;
        while (!tray.outline.enabled && waited < 1f) { waited += Time.unscaledDeltaTime; yield return null; }
        Assert.IsTrue(tray.outline.enabled, "Outline should show while a note is over the tray.");
        Assert.IsFalse(bin.outline.enabled, "The bin shouldn't light up over the tray.");

        // Let go over the tray: the note is kept, hidden, and disconnected.
        manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grabA);
        yield return null;
        yield return null;
        Assert.IsTrue(a != null, "An archived note isn't deleted.");
        Assert.IsFalse(a.gameObject.activeInHierarchy, "An archived note should leave the space.");
        CollectionAssert.Contains(NoteArchive.Notes, a, "The note should be in the archive.");
        Assert.AreEqual(0, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length,
            "Its connection line and label should be removed.");
        Assert.IsFalse(TourManager.tourPath.Contains(a.transform), "An archived note should leave the Tour list.");
        Assert.IsFalse(tray.visual.enabled, "Tray should hide again.");

        yield return new ExitPlayMode();
    }

    // Restoring archived notes: each comes back 1.2 m ahead at eye height facing the player,
    // without its connections, and a second note restored in a row is nudged sideways.
    [UnityTest]
    public IEnumerator RestoredNotesReturnInFrontAndDoNotOverlap()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        Setup();

        NoteData a = SpawnNote(Vector3.zero);
        NoteData b = SpawnNote(panel.headTransform.right * 0.6f);
        Physics.SyncTransforms();
        Connect(a, b);
        NoteArchive.Add(a);
        NoteArchive.Add(b);
        yield return null; // the lines are destroyed at the end of the frame
        Assert.AreEqual(0, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length);

        Transform head = panel.headTransform;
        NoteArchive.Restore(a, head);
        Assert.IsTrue(a.gameObject.activeInHierarchy, "A restored note should be back in the space.");
        Assert.IsNull(a.transform.parent, "A restored note should not stay under the archive holder.");
        CollectionAssert.DoesNotContain(NoteArchive.Notes, a);
        Vector3 toNote = a.transform.position - head.position;
        Assert.AreEqual(head.position.y, a.transform.position.y, 0.001f, "Restored at eye height.");
        toNote.y = 0f;
        Assert.AreEqual(NoteArchive.RestoreDistance, toNote.magnitude, 0.01f, "Restored 1.2 m ahead.");
        Assert.Greater(Vector3.Dot(toNote.normalized, a.transform.forward), 0.99f, "Turned to face the player.");
        Assert.AreEqual(0, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length,
            "Connections are not brought back.");

        NoteArchive.Restore(b, head);
        Vector3 apart = b.transform.position - a.transform.position;
        Assert.AreEqual(0f, apart.y, 0.001f);
        Assert.Greater(apart.magnitude, 0.2f, "The second restored note should be nudged clear of the first.");
        Assert.AreEqual(0, NoteArchive.Notes.Count, "Both notes should have left the archive.");

        yield return new ExitPlayMode();
    }

    // The drawer-pull gesture: grip pressed with the hand out in front, then pulled back.
    [Test]
    public void DrawerPullNeedsAHandHeldOutThenPulledBack()
    {
        // Out, then back 30 cm: opens, once.
        var d = new DrawerPullDetector();
        Assert.IsFalse(d.Step(0.5f, true, false));
        Assert.IsFalse(d.Step(0.4f, true, false), "10 cm back is not enough.");
        Assert.IsTrue(d.Step(0.2f, true, false), "30 cm back should open it.");
        Assert.IsFalse(d.Step(0.1f, true, false), "It fires once per grip press.");
        d.Step(0.1f, false, false);
        Assert.IsFalse(d.Step(0.5f, true, false), "A new press starts again.");
        Assert.IsTrue(d.Step(0.2f, true, false));

        // The hand starts too close to the body: not a drawer pull.
        d = new DrawerPullDetector();
        Assert.IsFalse(d.Step(0.2f, true, false));
        Assert.IsFalse(d.Step(0f, true, false));

        // Grip is busy holding or pointing at something: it doesn't count, even once freed
        // mid-press, until the grip is let go and pressed again.
        d = new DrawerPullDetector();
        Assert.IsFalse(d.Step(0.5f, true, true));
        Assert.IsFalse(d.Step(0.1f, true, false));

        // Pushing out further first still needs the full pull back from the furthest point.
        d = new DrawerPullDetector();
        d.Step(0.4f, true, false);
        Assert.IsFalse(d.Step(0.6f, true, false));
        Assert.IsFalse(d.Step(0.4f, true, false), "Only 20 cm back from 0.6.");
        Assert.IsTrue(d.Step(0.3f, true, false));
    }

    [UnityTest]
    public IEnumerator ArchiveGestureIsPlacedInTheScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        ArchiveDrawerGesture gesture = Object.FindFirstObjectByType<ArchiveDrawerGesture>();
        Assert.IsNotNull(gesture, "No archive drawer gesture in the scene.");
        Assert.IsNotNull(gesture.headTransform);
        Assert.IsNotNull(gesture.leftHand);
        Assert.IsNotNull(gesture.rightHand);

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

    // Grabs a's handle with the test hand, drags it onto one of b's handles and lets go.
    ConnectionLine Connect(NoteData a, NoteData b)
    {
        XRSimpleInteractable handle = a.GetComponentInChildren<VRHandleConnector>().GetComponent<XRSimpleInteractable>();
        hand.transform.position = handle.transform.position;
        manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)handle);
        hand.transform.position = b.GetComponentInChildren<VRHandleConnector>().transform.position;
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
