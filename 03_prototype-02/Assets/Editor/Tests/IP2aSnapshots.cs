using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Saves pictures of the scene from the player's head camera, for checking layout and
// visuals without a headset. Images go to <project>/Logs/Snapshots (git-ignored; Temp is wiped when Unity quits).
// Run from the Test Runner (EditMode, category "Snapshot") or headless with
// -runTests -testPlatform EditMode -testCategory Snapshot.
[Category("Snapshot")]
public class IP2aSnapshots
{
    const string ScenePath = "Assets/Scenes/IP2b.unity";

    [UnityTest]
    public IEnumerator NoteCreationPanel()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Camera cam = panel.headTransform.GetComponent<Camera>();
        Object.FindFirstObjectByType<ClapGestureDetector>().onClap.Invoke(); // the real flow: hides the prompt, opens the panel

        // Same state as the headset photo: Rectangle picked, some text typed.
        panel.transform.Find("RectangleButton").GetComponent<XRSimpleInteractable>()
            .selectEntered.Invoke(new SelectEnterEventArgs());
        panel.noteTextDisplay.text = "too close";
        panel.notePlaceholder.SetActive(false);
        yield return null;

        Save(cam, "panel_front.png");

        // A second view from slightly above, the way you look down at it in the headset.
        Vector3 originalPos = cam.transform.position;
        Quaternion originalRot = cam.transform.rotation;
        cam.transform.position = originalPos + Vector3.up * 0.35f;
        cam.transform.LookAt(panel.transform.position);
        Save(cam, "panel_from_above.png");
        cam.transform.SetPositionAndRotation(originalPos, originalRot);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator NotesWithLabelledConnection()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<PromptNote>()?.Hide();
        Camera cam = panel.headTransform.GetComponent<Camera>();

        panel.CreateTestNote();
        NoteData shortNote = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None)[0];
        shortNote.SetLabel("hi");
        shortNote.transform.position += panel.headTransform.right * -0.45f;

        panel.CreateTestNote();
        NoteData longNote = null;
        foreach (NoteData n in Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None))
            if (n != shortNote) longNote = n;
        longNote.SetLabel("This is a longer note, the kind of thing someone writes to capture a whole idea at once.");
        longNote.transform.position += panel.headTransform.right * 0.45f;

        // Connect them and label the connection, the same way the handles do.
        // Rectangle handles: 0 bottom, 1 right, 2 top, 3 left. Join the facing edges.
        VRHandleConnector from = shortNote.transform.Find("Handle_1").GetComponent<VRHandleConnector>();
        VRHandleConnector to = longNote.transform.Find("Handle_3").GetComponent<VRHandleConnector>();
        ConnectionLine line = Object.Instantiate(from.linePrefab).GetComponent<ConnectionLine>();
        line.pointA = from.transform;
        line.pointB = to.transform;
        GameObject label = Object.Instantiate(from.labelPrefab, line.transform);
        label.GetComponent<LineLabelFollow>().line = line.GetComponent<LineRenderer>();
        label.GetComponentInChildren<VRPlusButtonClick>(true).gameObject.SetActive(false);
        TMPro.TMP_Text labelText = label.GetComponentInChildren<VRLabelEdit>(true).GetComponent<TMPro.TMP_Text>();
        labelText.gameObject.SetActive(true);
        labelText.text = "leads to";
        float settle = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - settle < 0.6f) yield return null; // let the anchor dots pop in

        Save(cam, "notes_and_label.png");
        yield return new ExitPlayMode();
    }

    // Image notes: the picture across the top of the card, text underneath. One with the
    // placeholder picture and a caption, one without text.
    [UnityTest]
    public IEnumerator ImageNotes()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<PromptNote>()?.Hide();
        Camera cam = panel.headTransform.GetComponent<Camera>();

        panel.CreateTestNote();
        NoteData captioned = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None)[0];
        captioned.SetLabel("Site visit: the north hills from the lookout");
        captioned.SetMedia(MediaType.Image, panel.imagePlaceholder);
        captioned.transform.position += panel.headTransform.right * -0.25f;

        panel.CreateTestNote();
        foreach (NoteData n in Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None))
        {
            if (n == captioned) continue;
            n.SetLabel("");
            n.SetMedia(MediaType.Image, panel.imagePlaceholder);
            n.transform.position += panel.headTransform.right * 0.25f;
        }
        yield return null;

        Save(cam, "image_notes.png");
        yield return new ExitPlayMode();
    }

    // One note of every shape with its handles revealed, to check handle placement.
    [UnityTest]
    public IEnumerator HandlesOnEveryShape()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<PromptNote>()?.Hide();
        Camera cam = panel.headTransform.GetComponent<Camera>();

        NoteHandles[] notes = new NoteHandles[panel.shapePrefabs.Length];
        for (int i = 0; i < panel.shapePrefabs.Length; i++)
        {
            Vector3 pos = panel.headTransform.position + panel.headTransform.forward * 1.1f
                + panel.headTransform.right * ((i - 2) * 0.32f);
            Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(panel.headTransform.forward, Vector3.up));
            notes[i] = Object.Instantiate(panel.shapePrefabs[i], pos, rot).GetComponent<NoteHandles>();
            notes[i].GetComponent<NoteData>().SetLabel(((NoteShape)i).ToString());
        }

        // Keep them revealed long enough for the pop-in to finish.
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 0.6f)
        {
            foreach (NoteHandles n in notes) n.RevealForDrag();
            yield return null;
        }
        foreach (NoteHandles n in notes) n.RevealForDrag();
        Save(cam, "handles_on_every_shape.png");

        yield return new ExitPlayMode();
    }
    // Close-ups of the mic on the creation panel (front and side), plus a log of its parts.
    [UnityTest]
    public IEnumerator MicOnPanel()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<ClapGestureDetector>().onClap.Invoke();
        yield return null;
        Camera cam = panel.headTransform.GetComponent<Camera>();
        Transform mount = panel.transform.Find("Mic");

        var log = new System.Text.StringBuilder("MIC REPORT\n");
        foreach (Renderer r in mount.GetComponentsInChildren<Renderer>(true))
            log.AppendLine($"  renderer {r.name} (parent {r.transform.parent.name}) enabled={r.enabled} center={r.bounds.center} size={r.bounds.size}");
        foreach (Collider col in mount.GetComponentsInChildren<Collider>(true))
            log.AppendLine($"  collider {col.name} {col.GetType().Name} center={col.bounds.center} size={col.bounds.size}");
        log.AppendLine($"  panel at {panel.transform.position} forward {panel.transform.forward}; mount at {mount.position}");
        Debug.Log(log.ToString());

        Vector3 originalPos = cam.transform.position;
        Quaternion originalRot = cam.transform.rotation;
        Vector3 focus = mount.position;
        cam.transform.position = focus - panel.transform.forward * 0.45f;
        cam.transform.LookAt(focus);
        Save(cam, "mic_front.png");
        MicBoomArm boomArm = mount.GetComponent<MicBoomArm>();
        boomArm.head.mouseHovered = true;
        yield return null;
        Save(cam, "mic_glow_head.png");
        boomArm.head.mouseHovered = false;
        boomArm.elbow.mouseHovered = true;
        yield return null;
        Save(cam, "mic_glow_arm.png");
        boomArm.elbow.mouseHovered = false;
        cam.transform.position = focus + panel.transform.right * 0.45f - panel.transform.forward * 0.1f;
        cam.transform.LookAt(focus);
        Save(cam, "mic_side.png");
        cam.transform.SetPositionAndRotation(originalPos, originalRot);

        yield return new ExitPlayMode();
    }
    // The creation panel upright with the keyboard docked below it, seen from the player's seat.
    [UnityTest]
    public IEnumerator KeyboardDockedBelowPanel()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<ClapGestureDetector>().onClap.Invoke();
        yield return null;
        panel.EditNoteText();
        yield return null;

        Camera cam = panel.headTransform.GetComponent<Camera>();
        Vector3 p = panel.transform.position;
        cam.transform.position = p - panel.transform.forward * 1.0f + Vector3.up * 0.0f;
        cam.transform.LookAt(p + Vector3.down * 0.2f);
        cam.fieldOfView = 70f;
        Save(cam, "keyboard_docked_front.png");
        cam.transform.position = p + panel.transform.right * 1.0f - panel.transform.forward * 0.3f;
        cam.transform.LookAt(p + Vector3.down * 0.2f);
        Save(cam, "keyboard_docked_side.png");

        yield return new ExitPlayMode();
    }
    // What the player sees looking straight ahead while holding a note: the trash bin should
    // be visible near the bottom of the view. Uses a headset-like field of view.
    [UnityTest]
    public IEnumerator TrashBinInView()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<PromptNote>()?.Hide();
        Camera cam = panel.headTransform.GetComponent<Camera>();
        // Standing height, looking level, roughly a Quest 2's vertical field of view.
        cam.transform.position = new Vector3(cam.transform.position.x, 1.6f, cam.transform.position.z);
        cam.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up));
        cam.fieldOfView = 90f;

        FootTrashBin.Instance.RegisterHoldStart();
        ArchiveTray.Instance.RegisterHoldStart();
        yield return null;
        Save(cam, "trash_bin_in_view.png");

        // Same view with a note over the bin: the red outline shows.
        ArchiveTray.Instance.ShowReady();
        FootTrashBin.Instance.ShowReady();
        yield return null;
        Save(cam, "trash_bin_ready.png");
        Save(cam, "archive_tray_ready.png");

        // Closer, from above: the outline traces the can's silhouette without filling in
        // its see-through mesh.
        Transform bin = FootTrashBin.Instance.transform;
        cam.transform.position = bin.position + Vector3.up * 0.9f - bin.forward * 0.7f;
        cam.transform.LookAt(bin.position + Vector3.up * 0.2f);
        FootTrashBin.Instance.ShowReady();
        yield return null;
        Save(cam, "trash_bin_ready_close.png");

        // The tray from the same kind of angle, to check the model sits the right way up.
        Transform tray = ArchiveTray.Instance.transform;
        cam.transform.position = tray.position + Vector3.up * 0.9f - tray.forward * 0.7f;
        cam.transform.LookAt(tray.position + Vector3.up * 0.05f);
        ArchiveTray.Instance.ShowReady();
        yield return null;
        Save(cam, "archive_tray_close.png");
        FootTrashBin.Instance.RegisterHoldEnd();
        ArchiveTray.Instance.RegisterHoldEnd();

        yield return new ExitPlayMode();
    }
    // The archive window with a mix of archived notes: text of different lengths and a picture.
    [UnityTest]
    public IEnumerator ArchiveWindow()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        Object.FindFirstObjectByType<PromptNote>()?.Hide();
        Camera cam = panel.headTransform.GetComponent<Camera>();
        cam.fieldOfView = 70f;

        string[] labels = { "hi", "Remember to email the client", "Budget review", "Ideas for the launch week",
            "Site visit: the north hills from the lookout", "Call back", "Prototype 3 feedback and next steps for the team", "Q&A" };
        for (int i = 0; i < labels.Length; i++)
        {
            panel.CreateTestNote();
            NoteData[] all = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None);
            NoteData note = null;
            foreach (NoteData n in all) if (!NoteArchive.Notes.Contains(n)) note = n;
            note.SetLabel(labels[i]);
            if (i == 4) note.SetMedia(MediaType.Image, panel.imagePlaceholder);
            NoteArchive.Add(note);
            yield return null;
        }

        ArchiveDrawerGesture.RaisePulled(null);
        yield return null;
        yield return null;
        Save(cam, "archive_window.png");
        yield return new ExitPlayMode();
    }

    static void Save(Camera cam, string fileName)
    {
        const int width = 1280, height = 960;
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture previous = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = null;
        cam.targetTexture = previous;

        string dir = Path.Combine(Application.dataPath, "../Logs/Snapshots");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, fileName), image.EncodeToPNG());
        Debug.Log("Snapshot saved: " + Path.GetFullPath(Path.Combine(dir, fileName)));

        Object.DestroyImmediate(image);
        rt.Release();
        Object.DestroyImmediate(rt);
    }
}
