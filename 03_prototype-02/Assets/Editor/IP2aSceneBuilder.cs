#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

// One-shot build tool for the clap -> prompt -> note-creation-panel loop.
// Run via Tools > IP2a > Build Clap-To-Panel Flow with the IP2a scene open.
//
// Sizing rule for everything this file builds: real-world size is baked into MESH
// VERTICES (see BuildPrismMesh) rather than into transform.localScale. An earlier
// version used transform.localScale to size flat button cubes, which silently
// corrupted the local position/scale of any child parented under them (that's what
// made the Confirm button's "Create" label invisible). SetupFlatShape/SetupRoundedRect
// below always leave transform.localScale at Vector3.one, so a label, a glow ring, or
// anything else parented under a button keeps meaningful local transforms.
public static class IP2aSceneBuilder
{
    const string PrefabDir = "Assets/Prefabs";
    const string NotesDir = "Assets/Prefabs/Notes";
    const string MaterialsDir = "Assets/Materials";
    const float TextCanvasScale = 0.001f;

    // Found while sweeping the scene: "XR Origin (XR Rig)" is currently parented under
    // "Directional Light", which sits at local (0, 3, 0) tilted ~50 degrees down. Since the
    // rig has no position/rotation override of its own, it silently inherits that - which is
    // exactly why the camera opens high up looking down at the floor, and why the mic (a
    // child of the panel, at normal height) ends up far out of the controllers' reach. This
    // un-parents the rig back to the scene root and zeroes it out. Run this once; it's
    // independent of the other build steps.
    [MenuItem("Tools/IP2a/Fix Camera Rig Parenting")]
    public static void FixRigParenting()
    {
        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null)
        {
            Debug.LogWarning("IP2aSceneBuilder: 'XR Origin (XR Rig)' not found in the scene - nothing to fix.");
            return;
        }

        Transform t = rig.transform;
        if (t.parent != null)
        {
            Debug.Log($"IP2aSceneBuilder: XR Origin was parented under '{t.parent.name}' at local position " +
                $"{t.localPosition}, rotation {t.localRotation.eulerAngles} - moving it back to the scene root.");
            t.SetParent(null, false);
        }
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Selection.activeGameObject = rig;
        Debug.Log("IP2aSceneBuilder: XR Origin (XR Rig) is now at the scene root, position (0,0,0), no rotation.");
    }

    // Diagnostic: run this in Play mode (panel open, mic mounted) to find out exactly
    // why the mic FBX doesn't render in Game view. Logs world positions, viewport
    // coordinates (from Camera.main), and Renderer.isVisible for the mic mesh vs the
    // MicBase placeholder, so we can tell frustum-culling / occlusion apart from a
    // material or draw-order problem instead of guessing.
    [MenuItem("Tools/IP2a/Debug Mic Visibility")]
    public static void DebugMicVisibility()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("IP2aSceneBuilder: Debug Mic Visibility only works in Play mode (open the note panel first).");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("IP2aSceneBuilder: Camera.main is null.");
            return;
        }

        GameObject cyl = GameObject.Find("Cylinder003");
        GameObject micBase = GameObject.Find("MicBase");
        GameObject micFbxGO = cyl != null ? cyl.transform.parent.gameObject : null;

        Debug.Log($"IP2aSceneBuilder: Camera.main = '{cam.name}', world pos {cam.transform.position}, forward {cam.transform.forward}, nearClip {cam.nearClipPlane}, farClip {cam.farClipPlane}");

        if (cyl != null)
        {
            Vector3 worldPos = cyl.transform.position;
            Vector3 viewport = cam.WorldToViewportPoint(worldPos);
            Renderer r = cyl.GetComponent<Renderer>();
            Debug.Log($"IP2aSceneBuilder: Cylinder003 world pos {worldPos}, viewport coords {viewport} (0..1,0..1 on-screen, z>0 in front of camera), Renderer.isVisible={r?.isVisible}, Renderer.enabled={r?.enabled}, bounds.center={r?.bounds.center}, bounds.size={r?.bounds.size}");
        }
        else
        {
            Debug.LogError("IP2aSceneBuilder: could not find 'Cylinder003' in the scene (is the panel open?).");
        }

        if (micFbxGO != null)
        {
            Vector3 viewport = cam.WorldToViewportPoint(micFbxGO.transform.position);
            Debug.Log($"IP2aSceneBuilder: mic,fbx world pos {micFbxGO.transform.position}, viewport coords {viewport}");
        }

        if (micBase != null)
        {
            Vector3 worldPos = micBase.transform.position;
            Vector3 viewport = cam.WorldToViewportPoint(worldPos);
            Renderer r = micBase.GetComponent<Renderer>();
            Debug.Log($"IP2aSceneBuilder: MicBase world pos {worldPos}, viewport coords {viewport}, Renderer.isVisible={r?.isVisible}, Renderer.enabled={r?.enabled}");
        }
        else
        {
            Debug.LogError("IP2aSceneBuilder: could not find 'MicBase' in the scene.");
        }
    }

    [MenuItem("Tools/IP2a/Build Clap-To-Panel Flow")]
    public static void Build()
    {
        meshesThisBuild.Clear();
        Directory.CreateDirectory(NotesDir);

        int handleLayer = EnsureLayer("Handle");
        EnsureTag("Note");

        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null)
        {
            Debug.LogError("IP2aSceneBuilder: couldn't find 'XR Origin (XR Rig)' in the open scene.");
            return;
        }
        // The XRI starter rig nests the camera and both controllers under "Camera Offset".
        Transform head = rig.transform.Find("Camera Offset/Main Camera");
        Transform leftHand = rig.transform.Find("Camera Offset/Left Controller");
        Transform rightHand = rig.transform.Find("Camera Offset/Right Controller");
        if (head == null || leftHand == null || rightHand == null)
            Debug.LogError("IP2aSceneBuilder: couldn't find Main Camera / Left Controller / Right Controller under 'XR Origin (XR Rig)/Camera Offset'.");

        // Earlier runs of this builder left duplicate copies of some objects behind
        // (FindOrCreate used to miss inactive ones). Clear those out first.
        RemoveDuplicateRoots();
        EnsureXRUIEventSystem();

        Material lineMat = GetOrCreateUnlitMaterial(MaterialsDir + "/ConnectionLine_Mat.mat", new Color(0.15f, 0.15f, 0.15f));
        GameObject linePrefab = BuildConnectionLinePrefab(lineMat);
        GameObject labelPrefab = BuildConnectionLabelPrefab();
        BuildKeyboard(head);
        BuildTourMode(head, leftHand, rig);

        string[] shapeNames = { "Rectangle", "Circle", "Triangle", "Hexagon", "Star" };
        Color[] shapeColors = {
            new Color(0.90f, 0.55f, 0.45f), // Rectangle - terracotta
            new Color(0.45f, 0.65f, 0.90f), // Circle - blue
            new Color(0.55f, 0.85f, 0.55f), // Triangle - green
            new Color(0.90f, 0.80f, 0.40f), // Hexagon - yellow
            new Color(0.75f, 0.55f, 0.90f), // Star - purple
        };

        GameObject[] notePrefabs = new GameObject[5];
        for (int i = 0; i < 5; i++)
            notePrefabs[i] = BuildNotePrefab(shapeNames[i], shapeColors[i], (NoteShape)i, handleLayer, linePrefab, labelPrefab);

        // --- Delete (foot-level trash bin) ---
        BuildTrashBin(rig, head);

        // --- Prompt note ---
        GameObject promptNoteGO = FindOrCreate("PromptNote");
        PromptNote promptNote = promptNoteGO.GetComponent<PromptNote>();
        if (promptNote == null) promptNote = promptNoteGO.AddComponent<PromptNote>();
        TextMeshPro promptText = promptNoteGO.GetComponent<TextMeshPro>();
        if (promptText == null) promptText = promptNoteGO.AddComponent<TextMeshPro>();
        promptText.text = "Clap to create a note";
        promptText.fontSize = 8;
        promptText.color = new Color(0.15f, 0.15f, 0.15f);
        promptText.alignment = TextAlignmentOptions.Center;
        promptNoteGO.transform.localScale = Vector3.one * 0.03f;
        promptNote.headTransform = head;
        promptNote.distance = 0.8f;
        promptNoteGO.transform.position = new Vector3(0, 1.5f, 0.8f);

        // --- Note creation panel ---
        GameObject panelGO = FindOrCreate("NoteCreationPanel");
        NoteCreationPanel panel = panelGO.GetComponent<NoteCreationPanel>();
        if (panel == null) panel = panelGO.AddComponent<NoteCreationPanel>();
        panelGO.transform.position = new Vector3(0, 1.3f, 0.6f);

        // Layout, all in metres, relative to the panel's own centre. Adjust these if the
        // panel feels too big/small once you can see it on the headset.
        float panelWidth = 0.66f;
        // Card height isn't symmetric top/bottom any more - the top edge stays exactly
        // where it was (0.31, so title/shape row spacing is untouched), only the bottom
        // is pulled in. The old symmetric 0.62 height left about 0.15m of dead white
        // space below the media buttons before hitting the card's bottom edge.
        float panelTopEdge = 0.31f;
        float panelBottomEdge = -0.20f;
        float panelHeight = panelTopEdge - panelBottomEdge;
        float panelCenterY = (panelTopEdge + panelBottomEdge) * 0.5f;
        float panelRadius = 0.035f;
        float titleY = 0.26f;
        float shapeRowY = 0.17f;
        float shapeStartX = -0.24f;
        float shapeStep = 0.12f;
        float shapeSize = 0.08f;
        float textFieldY = 0f;
        float textFieldWidth = 0.54f;
        float textFieldHeight = 0.16f;
        float mediaRowY = -0.13f; // was -0.19 - that left a noticeably empty gap under the text field
        float mediaButtonWidth = 0.19f;
        float mediaButtonHeight = 0.06f;
        float mediaButtonRadius = 0.02f;
        float actionButtonWidth = 0.22f;
        float actionButtonHeight = 0.075f;
        float actionButtonRadius = 0.025f;
        float actionRowY = panelBottomEdge - 0.08f; // fixed gap below the card's actual bottom edge

        // Outline - a slightly larger grey rounded rect sitting just behind the white card,
        // so its edges poke out as a border. Same trick as the shape buttons' glow ring.
        float cardBorderThickness = 0.006f;
        GameObject cardOutline = FindOrCreateChild(panelGO.transform, "PanelOutline");
        SetupRoundedRect(cardOutline, panelWidth + cardBorderThickness * 2f, panelHeight + cardBorderThickness * 2f,
            panelRadius + cardBorderThickness, 0.01f, new Color(169f / 255f, 169f / 255f, 169f / 255f), "PanelOutline");
        cardOutline.transform.localPosition = new Vector3(0, panelCenterY, 0.01f);
        RemoveCollider(cardOutline);

        // Background plate - solid white so it actually reads as a card against the beige floor.
        GameObject bg = FindOrCreateChild(panelGO.transform, "PanelBackground");
        SetupRoundedRect(bg, panelWidth, panelHeight, panelRadius, 0.015f, Color.white, "PanelBackground");
        bg.transform.localPosition = new Vector3(0, panelCenterY, 0f);
        RemoveCollider(bg); // background itself isn't interactive, only the buttons on top of it are

        // Title
        GameObject title = FindOrCreateChild(panelGO.transform, "Title");
        TextMeshPro titleText = title.GetComponent<TextMeshPro>();
        if (titleText == null) titleText = title.AddComponent<TextMeshPro>();
        titleText.text = "New Note - pick a shape";
        titleText.fontSize = 10;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;
        title.transform.localPosition = new Vector3(0, titleY, -0.02f);
        title.transform.localScale = Vector3.one * 0.03f;

        // Test Note button - sits above the card itself (not inside it), for quickly
        // spawning a fixed Rectangle/"dummy text" note while testing connections and
        // labels, without clapping + picking a shape + typing every time.
        GameObject testNoteBtn = FindOrCreateChild(panelGO.transform, "TestNoteButton");
        SetupRoundedRect(testNoteBtn, 0.30f, 0.06f, 0.02f, 0.012f, new Color(0.55f, 0.5f, 0.8f), "TestNoteButton");
        testNoteBtn.transform.localPosition = new Vector3(0, panelTopEdge + 0.06f, -0.02f);
        XRSimpleInteractable testNoteInteractable = testNoteBtn.GetComponent<XRSimpleInteractable>();
        if (testNoteInteractable == null) testNoteInteractable = testNoteBtn.AddComponent<XRSimpleInteractable>();
        GameObject testNoteLabel = FindOrCreateChild(testNoteBtn.transform, "Label");
        TextMeshPro testNoteText = testNoteLabel.GetComponent<TextMeshPro>();
        if (testNoteText == null) testNoteText = testNoteLabel.AddComponent<TextMeshPro>();
        testNoteText.text = "Create Test Note";
        testNoteText.fontSize = 7f;
        testNoteText.alignment = TextAlignmentOptions.Center;
        testNoteText.color = Color.white;
        testNoteLabel.transform.localPosition = new Vector3(0, 0, -0.014f);
        testNoteLabel.transform.localScale = Vector3.one * 0.03f;
        RemovePersistentListeners(testNoteInteractable.selectEntered);
        UnityEventTools.AddVoidPersistentListener(testNoteInteractable.selectEntered, panel.CreateTestNote);
        if (testNoteBtn.GetComponent<DevOnly>() == null) testNoteBtn.AddComponent<DevOnly>();

        // Shape buttons - real shape icons (not colour swatches), each with a glow ring
        // child that's only shown when that shape is selected.
        Transform[] shapeButtonTransforms = new Transform[5];
        GameObject[] glowRings = new GameObject[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject btn = FindOrCreateChild(panelGO.transform, shapeNames[i] + "Button");
            Vector2[] outline = GetShapeOutline((NoteShape)i, shapeSize);
            SetupFlatShape(btn, outline, 0.02f, shapeColors[i], shapeNames[i] + "Button");
            btn.transform.localPosition = new Vector3(shapeStartX + i * shapeStep, shapeRowY, -0.02f);
            shapeButtonTransforms[i] = btn.transform;

            XRSimpleInteractable interactable = btn.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = btn.AddComponent<XRSimpleInteractable>();

            GameObject glow = FindOrCreateChild(btn.transform, "Glow");
            Vector2[] glowOutline = GetShapeOutline((NoteShape)i, shapeSize * 1.35f);
            SetupFlatShape(glow, glowOutline, 0.006f, Color.Lerp(shapeColors[i], Color.white, 0.5f), shapeNames[i] + "Glow");
            glow.transform.localPosition = new Vector3(0, 0, 0.006f); // sits just behind the icon
            RemoveCollider(glow);
            glow.SetActive(false);
            glowRings[i] = glow;

            GameObject label = FindOrCreateChild(btn.transform, "Label");
            TextMeshPro labelText = label.GetComponent<TextMeshPro>();
            if (labelText == null) labelText = label.AddComponent<TextMeshPro>();
            labelText.text = shapeNames[i];
            labelText.fontSize = 6f; // was 3.5 - too small to read clearly regardless of colour
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.15f, 0.15f, 0.15f);
            label.transform.localPosition = new Vector3(0, -shapeSize * 0.85f, -0.02f);
            label.transform.localScale = Vector3.one * 0.03f;

            RemovePersistentListeners(interactable.selectEntered);
            UnityEventTools.AddIntPersistentListener(interactable.selectEntered, panel.SetPendingShape, i);

            // Hover-only glow, independent of the click/select state above, so the shapes
            // read as clickable before you actually pick one.
            RemovePersistentListeners(interactable.hoverEntered);
            UnityEventTools.AddIntPersistentListener(interactable.hoverEntered, panel.SetHoverShape, i);
            RemovePersistentListeners(interactable.hoverExited);
            UnityEventTools.AddIntPersistentListener(interactable.hoverExited, panel.ClearHoverShape, i);
        }

        // Note text field - tinted by NoteCreationPanel at runtime to match the picked shape.
        // Pressing it opens the VR keyboard (NoteCreationPanel.EditNoteText).
        BuildNoteTextField(panelGO.transform, "NoteTextField",
            new Vector3(0, textFieldY, -0.018f), new Vector2(textFieldWidth, textFieldHeight),
            new Color(245f / 255f, 245f / 255f, 245f / 255f), panel,
            out TMP_Text noteTextDisplay, out GameObject notePlaceholder, out Image noteFieldBackground);

        // Add Image / Add Video / Add Document - clickable, but NoteCreationPanel only
        // enables their XRSimpleInteractable once Rectangle is the picked shape (and
        // re-tints them active/inactive/selected to match). mediaTypeValues here is the
        // MediaType enum's int value for each slot (Image=1, Video=3, Document=2 - NOT the
        // row position 0/1/2), since that's what NoteCreationPanel.SetPendingMedia expects.
        string[] mediaLabels = { "Add Image", "Add Video", "Add Document" };
        int[] mediaTypeValues = { (int)MediaType.Image, (int)MediaType.Video, (int)MediaType.Document };
        float[] mediaX = { -0.205f, 0f, 0.205f };
        GameObject[] mediaButtonGOs = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            string key = mediaLabels[i].Replace(" ", "") + "Button";
            GameObject btn = FindOrCreateChild(panelGO.transform, key);
            SetupRoundedRect(btn, mediaButtonWidth, mediaButtonHeight, mediaButtonRadius,
                0.012f, new Color(0.82f, 0.82f, 0.82f), key);
            btn.transform.localPosition = new Vector3(mediaX[i], mediaRowY, -0.02f);

            XRSimpleInteractable mediaInteractable = btn.GetComponent<XRSimpleInteractable>();
            if (mediaInteractable == null) mediaInteractable = btn.AddComponent<XRSimpleInteractable>();
            RemovePersistentListeners(mediaInteractable.selectEntered);
            UnityEventTools.AddIntPersistentListener(mediaInteractable.selectEntered, panel.SetPendingMedia, mediaTypeValues[i]);

            GameObject label = FindOrCreateChild(btn.transform, "Label");
            TextMeshPro labelText = label.GetComponent<TextMeshPro>();
            if (labelText == null) labelText = label.AddComponent<TextMeshPro>();
            labelText.text = mediaLabels[i];
            labelText.fontSize = 6f; // was 3.2 - same fix as the shape labels
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.15f, 0.15f, 0.15f); // was 0.5 (too light) - matches the shape labels' contrast
            label.transform.localPosition = new Vector3(0, 0, -0.01f);
            label.transform.localScale = Vector3.one * 0.03f;

            mediaButtonGOs[i] = btn;
        }

        // Discard (red) / Create (green) - below the panel, matching the reference sketch.
        GameObject discardBtn = FindOrCreateChild(panelGO.transform, "DiscardButton");
        SetupRoundedRect(discardBtn, actionButtonWidth, actionButtonHeight, actionButtonRadius,
            0.02f, new Color(0.75f, 0.35f, 0.35f), "DiscardButton");
        discardBtn.transform.localPosition = new Vector3(-actionButtonWidth * 0.65f, actionRowY, -0.02f);
        XRSimpleInteractable discardInteractable = discardBtn.GetComponent<XRSimpleInteractable>();
        if (discardInteractable == null) discardInteractable = discardBtn.AddComponent<XRSimpleInteractable>();
        GameObject discardLabel = FindOrCreateChild(discardBtn.transform, "Label");
        TextMeshPro discardText = discardLabel.GetComponent<TextMeshPro>();
        if (discardText == null) discardText = discardLabel.AddComponent<TextMeshPro>();
        discardText.text = "Discard";
        discardText.fontSize = 8f;
        discardText.alignment = TextAlignmentOptions.Center;
        discardText.color = Color.white;
        discardLabel.transform.localPosition = new Vector3(0, 0, -0.014f);
        discardLabel.transform.localScale = Vector3.one * 0.03f;
        RemovePersistentListeners(discardInteractable.selectEntered);
        UnityEventTools.AddVoidPersistentListener(discardInteractable.selectEntered, panel.DiscardPanel);

        GameObject createBtn = FindOrCreateChild(panelGO.transform, "CreateButton");
        SetupRoundedRect(createBtn, actionButtonWidth, actionButtonHeight, actionButtonRadius,
            0.02f, new Color(0.35f, 0.75f, 0.4f), "CreateButton");
        createBtn.transform.localPosition = new Vector3(actionButtonWidth * 0.65f, actionRowY, -0.02f);
        XRSimpleInteractable createInteractable = createBtn.GetComponent<XRSimpleInteractable>();
        if (createInteractable == null) createInteractable = createBtn.AddComponent<XRSimpleInteractable>();
        GameObject createLabel = FindOrCreateChild(createBtn.transform, "Label");
        TextMeshPro createText = createLabel.GetComponent<TextMeshPro>();
        if (createText == null) createText = createLabel.AddComponent<TextMeshPro>();
        createText.text = "Create";
        createText.fontSize = 8f;
        createText.alignment = TextAlignmentOptions.Center;
        createText.color = Color.white;
        createLabel.transform.localPosition = new Vector3(0, 0, -0.014f);
        createLabel.transform.localScale = Vector3.one * 0.03f;
        RemovePersistentListeners(createInteractable.selectEntered);
        UnityEventTools.AddVoidPersistentListener(createInteractable.selectEntered, panel.ConfirmCreate);

        // Clean up leftovers from earlier iterations of this panel (old flat "Confirm"
        // button, old spawn-a-handheld-mic button).
        string[] retired = { "ConfirmButton", "ConfirmLabel", "MicButton" };
        foreach (string n in retired)
        {
            Transform old = panelGO.transform.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // Mic - a permanent fixture mounted on the panel's right edge (not a spawnable prop).
        BuildAndMountMic(panelGO, panelWidth);

        // Wire the panel script fields
        var so = new SerializedObject(panel);
        so.FindProperty("headTransform").objectReferenceValue = head;
        so.FindProperty("spawnDistance").floatValue = 1.0f; // was 0.8 - still felt too close once actually in Play mode/headset

        SerializedProperty prefabsProp = so.FindProperty("shapePrefabs");
        prefabsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = notePrefabs[i];

        SerializedProperty buttonsProp = so.FindProperty("shapeButtons");
        buttonsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = shapeButtonTransforms[i];

        SerializedProperty glowProp = so.FindProperty("shapeGlowRings");
        glowProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            glowProp.GetArrayElementAtIndex(i).objectReferenceValue = glowRings[i];

        SerializedProperty colorsProp = so.FindProperty("shapeColors");
        colorsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            colorsProp.GetArrayElementAtIndex(i).colorValue = shapeColors[i];

        so.FindProperty("noteTextDisplay").objectReferenceValue = noteTextDisplay;
        so.FindProperty("notePlaceholder").objectReferenceValue = notePlaceholder;
        so.FindProperty("noteFieldBackground").objectReferenceValue = noteFieldBackground;
        so.FindProperty("defaultFieldColor").colorValue = new Color(245f / 255f, 245f / 255f, 245f / 255f);

        SerializedProperty mediaButtonsProp = so.FindProperty("mediaButtons");
        mediaButtonsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            mediaButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = mediaButtonGOs[i];

        so.ApplyModifiedProperties();

        // --- Clap manager ---
        GameObject clapGO = FindOrCreate("ClapManager");
        ClapGestureDetector clap = clapGO.GetComponent<ClapGestureDetector>();
        if (clap == null) clap = clapGO.AddComponent<ClapGestureDetector>();
        clap.leftHand = leftHand;
        clap.rightHand = rightHand;
        if (clap.onClap == null) clap.onClap = new UnityEngine.Events.UnityEvent();
        RemovePersistentListeners(clap.onClap);
        UnityEventTools.AddVoidPersistentListener(clap.onClap, promptNote.Hide);
        UnityEventTools.AddVoidPersistentListener(clap.onClap, panel.OpenPanel);

        // The screen-space "TEST: Click to Clap" button (Tools > IP2a > Add Test Clap
        // Button) is a desktop-only aid; keep it out of non-development device builds.
        GameObject testClapCanvas = FindRoot("TestClapButtonCanvas");
        if (testClapCanvas != null && testClapCanvas.GetComponent<DevOnly>() == null)
            testClapCanvas.AddComponent<DevOnly>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Selection.objects = new Object[] { panelGO, promptNoteGO, clapGO };
        Debug.Log("IP2aSceneBuilder: build complete. PromptNote + NoteCreationPanel + ClapManager wired, 5 note prefabs + connection line/label prefabs created.");
    }

    static GameObject BuildNotePrefab(string shapeName, Color color, NoteShape shape, int handleLayer, GameObject linePrefab, GameObject labelPrefab)
    {
        GameObject note = new GameObject(shapeName + "Note");
        note.tag = "Note";

        float noteSize = 0.22f; // was 0.16 - bigger shape gives the shape-aware text box below more room
        Vector2[] outline = GetShapeOutline(shape, noteSize);
        GetTextSafeRect(shape, noteSize, out float safeWidth, out float safeHeight, out float safeCenterY);

        // Kinematic: a released note stays exactly where it was let go and never pushes
        // other notes around (a new note spawning on top of an old one used to shove it).
        // XRGrabInteractable drives it while held and restores this on release.
        Rigidbody rb = note.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearDamping = 4f;
        rb.angularDamping = 4f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(note.transform, false);
        SetupFlatShape(visual, outline, 0.02f, color, shapeName + "NoteVisual");

        // The root carries the interaction collider (matching the visual's own bounds)
        // and the grab/rigidbody components, so there's only one collider on the prefab.
        BoxCollider visualCol = visual.GetComponent<BoxCollider>();
        BoxCollider col = note.AddComponent<BoxCollider>();
        col.size = visualCol.size;
        col.center = visualCol.center;
        Object.DestroyImmediate(visualCol);

        // World-space canvas for text + media thumbnail (NoteData needs uGUI components for these).
        // The canvas is now sized to the shape-aware "safe rect" itself (GetTextSafeRect,
        // computed above from the same geometry as GetShapeOutline/RegularPolygon) instead
        // of a fixed 150x100 box with a uniform inset - a uniform inset worked fine for
        // Rectangle/Circle but let text spill past Triangle's/Star's pointed edges.
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(note.transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(safeWidth / TextCanvasScale, safeHeight / TextCanvasScale);
        canvasGO.transform.localScale = Vector3.one * TextCanvasScale;
        canvasGO.transform.localPosition = new Vector3(0, safeCenterY, -0.021f);
        canvasGO.AddComponent<CanvasRenderer>();

        GameObject textGO = new GameObject("NoteText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = shapeName;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        // Shrinks/grows to fit whatever the user types into the note's shape.
        text.enableAutoSizing = true;
        text.fontSizeMin = 6;
        text.fontSizeMax = 20; // was 24 - biased smaller so more text fits comfortably
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        // The canvas itself IS the safe rect now, so the text just fills it edge to edge.
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        GameObject thumbGO = new GameObject("MediaThumbnail");
        thumbGO.transform.SetParent(canvasGO.transform, false);
        Image thumb = thumbGO.AddComponent<Image>();
        RectTransform thumbRect = thumbGO.GetComponent<RectTransform>();
        thumbRect.anchorMin = new Vector2(0.7f, 0.7f);
        thumbRect.anchorMax = new Vector2(0.95f, 0.95f);
        thumbRect.sizeDelta = Vector2.zero;
        thumbGO.SetActive(false);

        NoteData data = note.AddComponent<NoteData>();
        data.shape = shape;
        data.noteText = text;
        data.mediaThumbnail = thumb;

        XRGrabInteractable grab = note.AddComponent<XRGrabInteractable>();
        // A note should stay exactly where you let go of it, not keep sailing across the
        // room on your hand's release velocity - that's a distraction for a whiteboard
        // note and makes the trash-bin drop (below) unreliable to aim.
        grab.throwOnDetach = false;
        // Grabbed with the ray from a distance, a note stays out at the end of the ray and
        // moves with it, like dragging on a whiteboard, instead of flying into your hand.
        grab.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Far;
        // An empty colliders list makes XRI claim every child collider for the grab,
        // including the Handle's - so aiming at the handle grabbed the whole note instead
        // of starting a connection. List the note's own box only.
        grab.colliders.Clear();
        grab.colliders.Add(col);
        note.AddComponent<NoteTrashHandler>();

        // Handle child for connecting notes together
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(note.transform, false);
        handle.layer = handleLayer;
        handle.transform.localPosition = new Vector3(noteSize * 0.55f, -noteSize * 0.4f, 0f);
        BoxCollider handleCol = handle.AddComponent<BoxCollider>();
        handleCol.size = Vector3.one * 0.03f;
        VRHandleConnector connector = handle.AddComponent<VRHandleConnector>();
        connector.linePrefab = linePrefab;
        connector.labelPrefab = labelPrefab;
        connector.snapDistance = 0.15f;
        connector.handleLayer = 1 << handleLayer;

        GameObject handleVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handleVisual.name = "HandleVisual";
        handleVisual.transform.SetParent(handle.transform, false);
        handleVisual.transform.localScale = Vector3.one * 0.03f;
        Object.DestroyImmediate(handleVisual.GetComponent<Collider>());
        handleVisual.GetComponent<Renderer>().sharedMaterial = GetOrCreateUnlitMaterial(MaterialsDir + "/Handle_Mat.mat", new Color(0.2f, 0.2f, 0.2f));

        // Yellow hover-glow, hidden until a controller/hand ray or poke is hovering this
        // handle - same show/hide-on-hover pattern as the shape buttons' glow rings and the
        // new plus-button sphere.
        GameObject handleGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handleGlow.name = "GlowOutline";
        handleGlow.transform.SetParent(handle.transform, false);
        handleGlow.transform.localScale = Vector3.one * 0.042f; // a bit larger than HandleVisual so it reads as an outline behind it
        Object.DestroyImmediate(handleGlow.GetComponent<Collider>());
        handleGlow.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateTransparentMaterial(MaterialsDir + "/HandleGlow_Mat.mat", new Color(1f, 0.85f, 0.2f, 0.55f));
        handleGlow.SetActive(false);
        connector.glowOutline = handleGlow;

        string path = $"{NotesDir}/{shapeName}Note.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(note, path);
        Object.DestroyImmediate(note);
        return prefab;
    }

    static GameObject BuildConnectionLinePrefab(Material lineMat)
    {
        string path = $"{PrefabDir}/ConnectionLinePrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject go = new GameObject("ConnectionLine");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = lineMat;
        lr.widthMultiplier = 0.006f;
        lr.positionCount = 20;
        lr.useWorldSpace = true;
        go.AddComponent<ConnectionLine>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildConnectionLabelPrefab()
    {
        string path = $"{PrefabDir}/ConnectionLabelPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        // Root just carries LineLabelFollow (keeps the whole thing positioned/rotated to
        // the connection's midpoint, "text parallel to the line" - see LineLabelFollow.cs)
        // - the "+" sphere and the actual label are separate children so only one is ever
        // visible at a time.
        GameObject go = new GameObject("ConnectionLabel");
        go.transform.localScale = Vector3.one * 0.03f;
        go.AddComponent<LineLabelFollow>();

        // "+" icon: a translucent green sphere shown on every new connection. Poking it
        // reveals the relationship label below - the two-step "connect, then label" flow
        // from the original design concept ("a '+' icon labels the relationship"),
        // translated from IP1's desktop PlusButtonClick (OnMouseDown) to a VR poke (see
        // VRPlusButtonClick.cs). GlowOutline is a slightly larger yellow sphere, hidden
        // until hovered - same show/hide-on-hover pattern as the shape buttons' glow rings.
        GameObject plusGO = new GameObject("PlusButton");
        plusGO.transform.SetParent(go.transform, false);

        GameObject plusSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        plusSphere.name = "SphereVisual";
        plusSphere.transform.SetParent(plusGO.transform, false);
        plusSphere.transform.localScale = Vector3.one * 2.2f;
        Object.DestroyImmediate(plusSphere.GetComponent<Collider>());
        plusSphere.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateTransparentMaterial(MaterialsDir + "/PlusButtonSphere_Mat.mat", new Color(0.35f, 0.85f, 0.45f, 0.45f));

        GameObject plusGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        plusGlow.name = "GlowOutline";
        plusGlow.transform.SetParent(plusGO.transform, false);
        plusGlow.transform.localScale = Vector3.one * 2.6f; // a bit larger than SphereVisual so it reads as an outline behind it
        Object.DestroyImmediate(plusGlow.GetComponent<Collider>());
        plusGlow.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateTransparentMaterial(MaterialsDir + "/PlusButtonGlow_Mat.mat", new Color(1f, 0.85f, 0.2f, 0.55f));
        plusGlow.SetActive(false);

        GameObject plusGlyphGO = new GameObject("PlusGlyph");
        plusGlyphGO.transform.SetParent(plusGO.transform, false);
        TextMeshPro plusText = plusGlyphGO.AddComponent<TextMeshPro>();
        plusText.transform.localPosition = new Vector3(0, 0, -1.15f); // just in front of the sphere's surface
        plusText.text = "+";
        plusText.fontSize = 5f;
        plusText.alignment = TextAlignmentOptions.Center;
        plusText.color = Color.white;

        SphereCollider plusCol = plusGO.AddComponent<SphereCollider>();
        plusCol.radius = 1.3f;
        plusGO.AddComponent<XRSimpleInteractable>();

        // Label text: hidden until the "+" is poked, which opens the shared VRKeyboard
        // (see BuildKeyboard/VRKeyboard.cs) aimed at this TMP_Text - free typed text, not
        // presets. Poking the label again once it's showing reopens the keyboard pre-filled
        // with its current text, so it stays editable afterwards (VRLabelEdit.cs).
        GameObject labelGO = new GameObject("LabelText");
        labelGO.transform.SetParent(go.transform, false);
        TextMeshPro text = labelGO.AddComponent<TextMeshPro>();
        text.text = "Related to";
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        BoxCollider col = labelGO.AddComponent<BoxCollider>();
        col.size = new Vector3(3f, 1.5f, 0.5f);
        labelGO.AddComponent<XRSimpleInteractable>();
        labelGO.AddComponent<VRLabelEdit>().targetText = text;
        labelGO.SetActive(false);

        VRPlusButtonClick plusClick = plusGO.AddComponent<VRPlusButtonClick>();
        plusClick.labelText = text;
        plusClick.glowOutline = plusGlow;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Single reusable world-space poke keyboard (see VRKeyboard.cs) - IP2a's one piece of
    // real free-text entry, used for typing a connection's relationship label. Built once
    // here rather than per-connection; VRPlusButtonClick/VRLabelEdit just point it at
    // whichever TMP_Text they want typed into. QWERTY layout (not alphabetical) since
    // that's the layout everyone already has muscle memory for.
    // NOT yet tested on-device or in Play mode - check key spacing/collider sizes are
    // actually comfortable to poke once you can see it, this was built from measurements
    // matching the rest of the panel, not from a live test.
    static void BuildKeyboard(Transform head)
    {
        GameObject kbGO = FindOrCreate("VRKeyboard");
        VRKeyboard kb = kbGO.GetComponent<VRKeyboard>();
        if (kb == null) kb = kbGO.AddComponent<VRKeyboard>();

        float panelWidth = 0.70f; // wide enough for four bottom-row buttons (Del/Space/Cancel/Confirm)
        float panelHeight = 0.36f;
        GameObject bg = FindOrCreateChild(kbGO.transform, "KeyboardBackground");
        SetupRoundedRect(bg, panelWidth, panelHeight, 0.02f, 0.015f, Color.white, "KeyboardBackground");
        bg.transform.localPosition = Vector3.zero;
        RemoveCollider(bg);

        GameObject outline = FindOrCreateChild(kbGO.transform, "KeyboardOutline");
        SetupRoundedRect(outline, panelWidth + 0.012f, panelHeight + 0.012f, 0.026f, 0.01f,
            new Color(169f / 255f, 169f / 255f, 169f / 255f), "KeyboardOutline");
        outline.transform.localPosition = new Vector3(0, 0, 0.005f);
        RemoveCollider(outline);

        GameObject previewGO = FindOrCreateChild(kbGO.transform, "PreviewText");
        TextMeshPro previewText = previewGO.GetComponent<TextMeshPro>();
        if (previewText == null) previewText = previewGO.AddComponent<TextMeshPro>();
        previewText.text = "...";
        previewText.fontSize = 9f;
        previewText.alignment = TextAlignmentOptions.Center;
        previewText.color = Color.black;
        previewGO.transform.localPosition = new Vector3(0, 0.14f, -0.02f);
        previewGO.transform.localScale = Vector3.one * 0.03f;

        string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        float[] rowY = { 0.06f, 0f, -0.06f };
        float keySize = 0.048f;
        float keyStep = 0.054f;
        float keyRadius = 0.008f;
        Color keyColor = new Color(0.9f, 0.9f, 0.9f);

        for (int r = 0; r < rows.Length; r++)
        {
            string row = rows[r];
            float startX = -(row.Length - 1) * keyStep * 0.5f;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                GameObject key = FindOrCreateChild(kbGO.transform, "Key_" + c);
                SetupRoundedRect(key, keySize, keySize, keyRadius, 0.01f, keyColor, "Key_" + c);
                key.transform.localPosition = new Vector3(startX + i * keyStep, rowY[r], -0.02f);

                XRSimpleInteractable interactable = key.GetComponent<XRSimpleInteractable>();
                if (interactable == null) interactable = key.AddComponent<XRSimpleInteractable>();

                GameObject label = FindOrCreateChild(key.transform, "Label");
                TextMeshPro labelText = label.GetComponent<TextMeshPro>();
                if (labelText == null) labelText = label.AddComponent<TextMeshPro>();
                labelText.text = c.ToString();
                labelText.fontSize = 7f;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = Color.black;
                label.transform.localPosition = new Vector3(0, 0, -0.012f);
                label.transform.localScale = Vector3.one * 0.03f;

                VRKeyboardKey keyScript = key.GetComponent<VRKeyboardKey>();
                if (keyScript == null) keyScript = key.AddComponent<VRKeyboardKey>();
                keyScript.action = VRKeyboardKey.KeyAction.Character;
                keyScript.character = c.ToString().ToLowerInvariant();
            }
        }

        // Bottom row: Backspace / Space (wide) / Cancel / Confirm.
        float bottomY = -0.13f;
        BuildKeyboardActionKey(kbGO.transform, "Key_Backspace", "Del", 0.10f, 0.05f,
            new Color(0.75f, 0.35f, 0.35f), new Vector3(-0.27f, bottomY, -0.02f), VRKeyboardKey.KeyAction.Backspace);
        BuildKeyboardActionKey(kbGO.transform, "Key_Space", "Space", 0.20f, 0.05f,
            keyColor, new Vector3(-0.06f, bottomY, -0.02f), VRKeyboardKey.KeyAction.Space);
        BuildKeyboardActionKey(kbGO.transform, "Key_Cancel", "Cancel", 0.12f, 0.05f,
            new Color(0.5f, 0.5f, 0.5f), new Vector3(0.13f, bottomY, -0.02f), VRKeyboardKey.KeyAction.Cancel);
        BuildKeyboardActionKey(kbGO.transform, "Key_Confirm", "Confirm", 0.13f, 0.05f,
            new Color(0.35f, 0.75f, 0.4f), new Vector3(0.28f, bottomY, -0.02f), VRKeyboardKey.KeyAction.Confirm);

        var so = new SerializedObject(kb);
        so.FindProperty("headTransform").objectReferenceValue = head;
        so.FindProperty("spawnDistance").floatValue = 0.6f;
        so.FindProperty("previewText").objectReferenceValue = previewText;
        so.ApplyModifiedProperties();

        // Left active on purpose: VRKeyboard.Awake sets Instance and then hides the
        // keyboard itself at Play start. Saving it inactive meant Awake never ran.
        kbGO.SetActive(true);
    }

    static void BuildKeyboardActionKey(Transform parent, string name, string labelStr, float width, float height,
        Color color, Vector3 localPos, VRKeyboardKey.KeyAction action)
    {
        GameObject key = FindOrCreateChild(parent, name);
        SetupRoundedRect(key, width, height, 0.01f, 0.01f, color, name);
        key.transform.localPosition = localPos;

        XRSimpleInteractable interactable = key.GetComponent<XRSimpleInteractable>();
        if (interactable == null) interactable = key.AddComponent<XRSimpleInteractable>();

        GameObject label = FindOrCreateChild(key.transform, "Label");
        TextMeshPro labelText = label.GetComponent<TextMeshPro>();
        if (labelText == null) labelText = label.AddComponent<TextMeshPro>();
        labelText.text = labelStr;
        labelText.fontSize = 6f;
        labelText.alignment = TextAlignmentOptions.Center;
        // Space shares the light letter-key colour (passed in as `color`), so it needs
        // dark text too - only the coloured/dark action keys (Backspace/Cancel/Confirm)
        // get white text.
        bool darkBackground = action == VRKeyboardKey.KeyAction.Backspace
            || action == VRKeyboardKey.KeyAction.Cancel
            || action == VRKeyboardKey.KeyAction.Confirm;
        labelText.color = darkBackground ? Color.white : Color.black;
        label.transform.localPosition = new Vector3(0, 0, -0.012f);
        label.transform.localScale = Vector3.one * 0.03f;

        VRKeyboardKey keyScript = key.GetComponent<VRKeyboardKey>();
        if (keyScript == null) keyScript = key.AddComponent<VRKeyboardKey>();
        keyScript.action = action;
    }

    // Wires VRTourMode.cs into the scene: a compact watch-style menu mounted on the back
    // of the left hand/controller that shows once you rotate your wrist to look at it
    // (see VRTourMode.IsLookingAtWrist), listing every connected note (TourManager.tourPath,
    // populated by VRHandleConnector.OnReleased whenever two notes get linked) and
    // teleporting the rig's XR Origin there on poke. This method only builds/wires the
    // visuals - the look-detection, list-rebuilding and jump logic all already existed in
    // VRTourMode.cs, fully written but never hooked up to anything in the scene.
    static void BuildTourMode(Transform head, Transform leftHand, GameObject rig)
    {
        if (leftHand == null)
        {
            Debug.LogWarning("IP2aSceneBuilder: 'Left Controller' not found under the XR rig - Tour Mode needs a wrist to mount its menu on, skipping.");
            return;
        }

        GameObject listItemPrefab = BuildTourListItemPrefab();

        // --- Wrist menu canvas ---
        GameObject menuGO = FindOrCreateChild(leftHand, "TourMenu");
        menuGO.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        // Rotates the canvas's front face (-Z of an unrotated RectTransform) to point along
        // the wrist's local +Y ("up") - VRTourMode's own look check compares
        // wristTransform.up against the direction back to the headset, so the menu should
        // face the same way (i.e. up toward your face when you turn your wrist to look at
        // it, watch-check style). Exact offset/angle will likely need live tuning once it's
        // actually on a controller - same as the mic mount and panel layout were.
        menuGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        menuGO.transform.localScale = Vector3.one * TextCanvasScale;

        Canvas menuCanvas = menuGO.GetComponent<Canvas>();
        if (menuCanvas == null) menuCanvas = menuGO.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform menuRect = menuGO.GetComponent<RectTransform>();
        menuRect.sizeDelta = new Vector2(0.20f / TextCanvasScale, 0.26f / TextCanvasScale);
        if (menuGO.GetComponent<CanvasRenderer>() == null) menuGO.AddComponent<CanvasRenderer>();
        // GraphicRaycaster handles the Editor mouse; TrackedDeviceGraphicRaycaster lets
        // controller rays and pokes hit the list buttons (with XRUIInputModule).
        if (menuGO.GetComponent<GraphicRaycaster>() == null) menuGO.AddComponent<GraphicRaycaster>();
        if (menuGO.GetComponent<TrackedDeviceGraphicRaycaster>() == null) menuGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        GameObject menuBg = FindOrCreateChild(menuGO.transform, "Background");
        RectTransform menuBgRect = menuBg.GetComponent<RectTransform>();
        if (menuBgRect == null) menuBgRect = menuBg.AddComponent<RectTransform>();
        menuBgRect.anchorMin = Vector2.zero;
        menuBgRect.anchorMax = Vector2.one;
        menuBgRect.sizeDelta = Vector2.zero;
        Image menuBgImage = menuBg.GetComponent<Image>();
        if (menuBgImage == null) menuBgImage = menuBg.AddComponent<Image>();
        menuBgImage.color = Color.white;

        GameObject menuTitleGO = FindOrCreateChild(menuGO.transform, "Title");
        RectTransform menuTitleRect = menuTitleGO.GetComponent<RectTransform>();
        if (menuTitleRect == null) menuTitleRect = menuTitleGO.AddComponent<RectTransform>();
        menuTitleRect.anchorMin = new Vector2(0f, 0.88f);
        menuTitleRect.anchorMax = new Vector2(1f, 1f);
        menuTitleRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI menuTitleText = menuTitleGO.GetComponent<TextMeshProUGUI>();
        if (menuTitleText == null) menuTitleText = menuTitleGO.AddComponent<TextMeshProUGUI>();
        menuTitleText.text = "Tour";
        menuTitleText.fontSize = 34;
        menuTitleText.alignment = TextAlignmentOptions.Center;
        menuTitleText.color = Color.black;

        // Scroll view: Viewport (masked) + Content (vertical stack of note buttons, one
        // per TourManager.tourPath entry, rebuilt by VRTourMode.RebuildList() every time
        // the menu opens).
        GameObject viewportGO = FindOrCreateChild(menuGO.transform, "Viewport");
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        if (viewportRect == null) viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.05f, 0.04f);
        viewportRect.anchorMax = new Vector2(0.95f, 0.85f);
        viewportRect.sizeDelta = Vector2.zero;
        if (viewportGO.GetComponent<RectMask2D>() == null) viewportGO.AddComponent<RectMask2D>();
        Image viewportImage = viewportGO.GetComponent<Image>();
        if (viewportImage == null) viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f); // RectMask2D needs a Graphic to clip against, kept near-invisible

        GameObject contentGO = FindOrCreateChild(viewportGO.transform, "Content");
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        if (contentRect == null) contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentGO.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 20f;
        ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = menuGO.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = menuGO.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Exit Tour button - deliberately NOT part of the scroll view/Content, since
        // RebuildList() destroys and recreates every child of Content on every menu open.
        // Same poke-button pattern as the rest of the panel (SetupRoundedRect +
        // XRSimpleInteractable), parented to the wrist so it travels with the player once
        // JumpTo() moves the XR Origin. Hidden until VRTourMode.JumpTo()/ExitTour() toggle it.
        GameObject exitBtn = FindOrCreateChild(leftHand, "ExitTourButton");
        SetupRoundedRect(exitBtn, 0.10f, 0.04f, 0.012f, 0.01f, new Color(0.75f, 0.35f, 0.35f), "ExitTourButton");
        exitBtn.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        exitBtn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        XRSimpleInteractable exitInteractable = exitBtn.GetComponent<XRSimpleInteractable>();
        if (exitInteractable == null) exitInteractable = exitBtn.AddComponent<XRSimpleInteractable>();
        GameObject exitLabel = FindOrCreateChild(exitBtn.transform, "Label");
        TextMeshPro exitLabelText = exitLabel.GetComponent<TextMeshPro>();
        if (exitLabelText == null) exitLabelText = exitLabel.AddComponent<TextMeshPro>();
        exitLabelText.text = "Exit Tour";
        exitLabelText.fontSize = 6f;
        exitLabelText.alignment = TextAlignmentOptions.Center;
        exitLabelText.color = Color.white;
        exitLabel.transform.localPosition = new Vector3(0, 0, -0.012f);
        exitLabel.transform.localScale = Vector3.one * 0.03f;
        exitBtn.SetActive(false);

        // --- VRTourMode component + wiring ---
        GameObject tourGO = FindOrCreate("TourMode");
        VRTourMode tour = tourGO.GetComponent<VRTourMode>();
        if (tour == null) tour = tourGO.AddComponent<VRTourMode>();

        RemovePersistentListeners(exitInteractable.selectEntered);
        UnityEventTools.AddVoidPersistentListener(exitInteractable.selectEntered, tour.ExitTour);

        var tourSo = new SerializedObject(tour);
        tourSo.FindProperty("headTransform").objectReferenceValue = head;
        tourSo.FindProperty("wristTransform").objectReferenceValue = leftHand;
        tourSo.FindProperty("xrOriginRoot").objectReferenceValue = rig.transform;
        tourSo.FindProperty("menuCanvas").objectReferenceValue = menuGO;
        tourSo.FindProperty("listContent").objectReferenceValue = contentRect;
        tourSo.FindProperty("listItemButtonPrefab").objectReferenceValue = listItemPrefab;
        tourSo.FindProperty("exitTourButton").objectReferenceValue = exitBtn;
        tourSo.ApplyModifiedProperties();

        // Safe to deactivate here (unlike VRKeyboard's root object) - this is a *child*
        // found via FindOrCreateChild/Transform.Find, which (unlike the GameObject.Find
        // used for top-level objects) still finds inactive children, so idempotency on
        // rerun isn't affected. VRTourMode.Start() also hides it at Play-mode start.
        menuGO.SetActive(false);
    }

    // Runtime list-item prefab for the wrist menu's scroll view: a plain uGUI Button (not
    // an XRSimpleInteractable poke button like the rest of this file) because
    // VRTourMode.RebuildList() drives it through Button.onClick, matching how the note
    // text field elsewhere in this file already goes through Canvas + GraphicRaycaster
    // rather than a 3D collider.
    static GameObject BuildTourListItemPrefab()
    {
        string path = $"{PrefabDir}/TourListItemPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject go = new GameObject("TourListItem", typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 60f);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.85f, 0.9f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Item";
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 28;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Builds the desk-mic-style fixture mounted on the panel's right edge: a fixed base,
    // an arm that visually rotates to track the head, and a grabbable head that the user
    // can pull toward/push away from the panel but never fully detach. See
    // MicBoomConstraint.cs for the actual movement clamp (added to the mount below).
    static void BuildAndMountMic(GameObject panelGO, float panelWidth)
    {
        // The mic FBX's mesh isn't centred on its own pivot (I confirmed this by trying
        // to auto-correct it from Renderer.bounds, which overshot to ~0.7m away - reverted
        // that, see below). Once you can see the mic in the Editor, nudge the "mic,fbx"
        // object's Position in the Inspector until it sits right at the end of the arm,
        // then tell me those X/Y/Z numbers and I'll put them here permanently instead of
        // (0,0,0) - same as we did for the -180 Y rotation.
        Vector3 micVisualOffset = Vector3.zero;

        GameObject mount = FindOrCreateChild(panelGO.transform, "MicMount");
        mount.transform.localPosition = new Vector3(panelWidth * 0.5f + 0.02f, 0.02f, -0.01f);
        mount.transform.localRotation = Quaternion.identity;

        GameObject baseGO = FindOrCreateChild(mount.transform, "MicBase");
        SetupFlatShape(baseGO, RegularPolygon(0.025f, 16, 0f), 0.015f, new Color(0.2f, 0.2f, 0.2f), "MicBase");
        baseGO.transform.localPosition = Vector3.zero;
        RemoveCollider(baseGO); // purely visual - not meant to be interactable itself

        float armLength = 0.07f; // was 0.14 - too long, made the mic look like it was floating well off the panel

        GameObject arm = FindOrCreateChild(mount.transform, "MicArm");
        arm.transform.localPosition = Vector3.zero;
        arm.transform.localRotation = Quaternion.identity;

        GameObject armVisual = FindOrCreateChild(arm.transform, "MicArmVisual");
        MeshFilter armMf = armVisual.GetComponent<MeshFilter>();
        if (armMf == null) armMf = armVisual.AddComponent<MeshFilter>();
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armMf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tempCube);
        MeshRenderer armMr = armVisual.GetComponent<MeshRenderer>();
        if (armMr == null) armMr = armVisual.AddComponent<MeshRenderer>();
        armMr.sharedMaterial = GetOrCreateUnlitMaterial($"{MaterialsDir}/MicArm_Mat.mat", new Color(0.25f, 0.25f, 0.25f));
        // Spans 0..armLength along local Z (not centred on the pivot) so it visually
        // connects the mount to wherever MicBoomConstraint puts the head each frame.
        armVisual.transform.localScale = new Vector3(0.006f, 0.006f, armLength);
        armVisual.transform.localPosition = new Vector3(0, 0, armLength * 0.5f);
        RemoveCollider(armVisual);

        // Sibling of MicArm (not its child) so the constraint script can move the head
        // independently of the arm's own rotation.
        GameObject headGO = FindOrCreateChild(mount.transform, "MicHead");
        headGO.transform.localPosition = new Vector3(0, 0, armLength);
        headGO.transform.localRotation = Quaternion.identity;

        // The mic model is only placed once. Re-instantiating it on every run gave it new
        // object IDs (and a new shortened arm mesh) each time, churning the scene file.
        // To redo it, delete MicHead's child in the scene and run the builder again.
        GameObject micVisualSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/mic,fbx.fbx");
        bool micAlreadyPlaced = headGO.transform.childCount > 0;
        if (micVisualSource != null && !micAlreadyPlaced)
        {
            GameObject micVisual = (GameObject)PrefabUtility.InstantiatePrefab(micVisualSource, headGO.transform);
            // Reverted the Renderer.bounds auto-centring I tried here - it overshot badly
            // (pushed the model ~0.7m away, nowhere near MicHead) rather than fixing the
            // offset, so it's worse than doing nothing. Back to zero, which at least
            // parents the model at the right spot even if the FBX's own mesh pivot isn't
            // centred on it.
            micVisual.transform.localPosition = micVisualOffset;
            // Confirmed by eye in the Inspector - the model's own front faced away from
            // the user at identity rotation, this turns it around to face correctly.
            micVisual.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
            micVisual.transform.localScale = Vector3.one * 0.35f;
            // The FBX's mesh pivot isn't centred on this transform (see the bounds-based
            // recentring further down, after Object009 is shortened) - micVisualOffset above
            // stays a manual nudge you can still use on top of that if the auto-centring
            // isn't perfect once you see it.

            // The mic model has its own built-in boom-arm piece, "Object009" in the FBX -
            // it was reaching way further than intended. You asked for its actual length
            // shortened, not its Transform scaled down, so this edits the mesh's own
            // vertex data instead: it rewrites Object009's mesh, compressing vertices
            // along whichever local axis is longest (almost certainly the rod's length),
            // keeping the end nearer the mount fixed and pulling only the far end in.
            // Object009's own Transform.localScale stays exactly (1,1,1) throughout - only
            // the geometry itself gets smaller. armShortenFactor is the one number to
            // adjust: 0.5 halves its real length, 0.3 leaves 30% of it, etc.
            float armShortenFactor = 0.5f;
            Transform micArmPiece = micVisual.transform.Find("Object009");
            if (micArmPiece != null)
            {
                MeshFilter micArmMf = micArmPiece.GetComponent<MeshFilter>();
                if (micArmMf != null && micArmMf.sharedMesh != null)
                {
                    Mesh originalArmMesh = micArmMf.sharedMesh;
                    Bounds localBounds = originalArmMesh.bounds;
                    float extentX = localBounds.size.x, extentY = localBounds.size.y, extentZ = localBounds.size.z;
                    int lengthAxis = (extentX >= extentY && extentX >= extentZ) ? 0 : (extentY >= extentZ ? 1 : 2);
                    float anchorCoord = lengthAxis == 0 ? localBounds.min.x : lengthAxis == 1 ? localBounds.min.y : localBounds.min.z;

                    Vector3[] verts = originalArmMesh.vertices;
                    Vector3[] shortenedVerts = new Vector3[verts.Length];
                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 v = verts[i];
                        if (lengthAxis == 0) v.x = anchorCoord + (v.x - anchorCoord) * armShortenFactor;
                        else if (lengthAxis == 1) v.y = anchorCoord + (v.y - anchorCoord) * armShortenFactor;
                        else v.z = anchorCoord + (v.z - anchorCoord) * armShortenFactor;
                        shortenedVerts[i] = v;
                    }

                    Mesh shortenedArmMesh = new Mesh();
                    shortenedArmMesh.name = originalArmMesh.name + "_Shortened";
                    shortenedArmMesh.vertices = shortenedVerts;
                    shortenedArmMesh.triangles = originalArmMesh.triangles;
                    shortenedArmMesh.normals = originalArmMesh.normals;
                    shortenedArmMesh.uv = originalArmMesh.uv;
                    shortenedArmMesh.RecalculateBounds();
                    micArmMf.sharedMesh = SaveMeshAsset("MicArmShortened", shortenedArmMesh); // the original FBX asset is untouched

                    string axisName = lengthAxis == 0 ? "X" : lengthAxis == 1 ? "Y" : "Z";
                    Debug.Log($"IP2aSceneBuilder: shortened mic model's Object009 to {armShortenFactor * 100f}% of its " +
                        $"length along local {axisName} (guessed as the rod's long axis from its mesh bounds). If it now " +
                        "detaches from the wrong end instead of the mount end, that's the anchorCoord guess being wrong " +
                        "- tell me and I'll flip it to localBounds.max instead.");
                }
                else
                {
                    Debug.LogWarning("IP2aSceneBuilder: found 'Object009' under the mic model but it has no mesh to shorten.");
                }
            }
            else
            {
                Debug.LogWarning("IP2aSceneBuilder: couldn't find a child named 'Object009' under the mic model - can't shorten its arm piece. Check the exact name in the Hierarchy.");
            }

            // Root-caused via Tools > IP2a > Debug Mic Visibility in Play mode: the FBX's
            // sub-meshes (Cylinder003 etc.) don't just have an off-pivot origin, they render
            // ~0.7m away from micVisual's own transform (confirmed: Cylinder003 was at
            // viewport x=1.28, off the right edge of the screen, Renderer.isVisible=false,
            // while micVisual's own pivot sat at a perfectly reasonable viewport x=0.65).
            // That's why the model was invisible in Game view but looked fine in the Scene
            // view when framed directly on it - Frame Selected jumps the camera to wherever
            // the geometry actually is, wherever that is.
            // The earlier Renderer.bounds auto-centring attempt wasn't wrong in kind, it was
            // just done before the Object009 mesh edit above and left uncomputed correctly -
            // this repeats it now, after the mesh is in its final shape, and applies the
            // correction as a world-space Transform.position delta (never touching
            // localScale, so it can't corrupt any child's local transform the way scaling a
            // parent can).
            Renderer[] micRenderers = micVisual.GetComponentsInChildren<Renderer>();
            if (micRenderers.Length > 0)
            {
                Bounds combined = micRenderers[0].bounds;
                for (int i = 1; i < micRenderers.Length; i++) combined.Encapsulate(micRenderers[i].bounds);
                Vector3 worldDelta = headGO.transform.position - combined.center;
                micVisual.transform.position += worldDelta;
                Debug.Log($"IP2aSceneBuilder: recentred mic model by world offset {worldDelta} (combined mesh bounds " +
                    $"were centred at {combined.center}, size {combined.size}, MicHead is at {headGO.transform.position}). " +
                    "Run Tools > IP2a > Debug Mic Visibility again in Play mode to confirm Renderer.isVisible=True and " +
                    "viewport coords are inside 0..1 now.");
            }
        }
        else if (micVisualSource == null)
        {
            Debug.LogWarning("IP2aSceneBuilder: mic model not found at Assets/Models/mic,fbx.fbx, MicHead will have no visual.");
        }

        BoxCollider headCol = headGO.GetComponent<BoxCollider>();
        if (headCol == null) headCol = headGO.AddComponent<BoxCollider>();
        headCol.size = new Vector3(0.05f, 0.05f, 0.08f);
        headCol.center = Vector3.zero;

        Rigidbody headRb = headGO.GetComponent<Rigidbody>();
        if (headRb == null) headRb = headGO.AddComponent<Rigidbody>();
        headRb.useGravity = false;
        headRb.isKinematic = true;

        if (headGO.GetComponent<XRGrabInteractable>() == null)
            headGO.AddComponent<XRGrabInteractable>();

        MicBoomConstraint boom = mount.GetComponent<MicBoomConstraint>();
        if (boom == null) boom = mount.AddComponent<MicBoomConstraint>();
        boom.mount = mount.transform;
        boom.arm = arm.transform;
        boom.head = headGO.transform;
        boom.minDistance = 0.05f;
        boom.maxDistance = armLength;
    }

    // Foot-level trash bin for the delete gesture (FootTrashBin.cs / NoteTrashHandler.cs,
    // both already in Assets/Scripts - this is just the missing piece, the GameObject
    // itself was never built into the scene). A flat rectangle standing in for the real
    // dustbin model Yutika will bring in later: FootTrashBin only reads this object's own
    // Renderer/Collider/Transform, so swapping the visual later (new mesh, same
    // GameObject and components) won't need any script changes.
    static void BuildTrashBin(GameObject rig, Transform head)
    {
        GameObject bin = FindOrCreate("FootTrashBin");
        SetupFlatShape(bin, RectOutline(0.35f, 0.35f), 0.02f, new Color(0.75f, 0.35f, 0.35f), "FootTrashBin");
        // Starting position only - FootTrashBin.Update() re-tracks the rig's position every
        // frame itself once a note is picked up, this is just where it sits at edit time.
        bin.transform.position = rig.transform.position;
        bin.transform.rotation = Quaternion.identity;

        FootTrashBin trashBin = bin.GetComponent<FootTrashBin>();
        if (trashBin == null) trashBin = bin.AddComponent<FootTrashBin>();
        trashBin.playerRoot = rig.transform; // XR Origin's own root = floor-level position (Tracking Origin Mode: Floor)
        trashBin.headTransform = head;       // follows where the player stands, not the rig's origin
        trashBin.footOffsetY = 0.01f; // just proud of the floor plane, avoids z-fighting
    }

    // ---------- Shape outlines ----------

    static Vector2[] GetShapeOutline(NoteShape shape, float size)
    {
        switch (shape)
        {
            case NoteShape.Rectangle: return RectOutline(size, size * 0.7f);
            case NoteShape.Circle: return RegularPolygon(size * 0.5f, 32, 0f);
            case NoteShape.Triangle: return RegularPolygon(size * 0.55f, 3, 90f);
            case NoteShape.Hexagon: return RegularPolygon(size * 0.5f, 6, 0f);
            case NoteShape.Star: return StarOutline(size * 0.55f, size * 0.22f, 5);
            default: return RectOutline(size, size);
        }
    }

    // The largest safe rectangle (in the same local units as `size`) that a note's text can
    // occupy without spilling past that shape's own edges. A single uniform inset (the old
    // approach) works fine for Rectangle/Circle, since those are convex and roughly
    // square-ish, but badly under- or over-shoots for Triangle (tapers to a point),
    // Hexagon (tapers less, but still not square) and Star (concave - most of its outer
    // radius is unusable). Each case below is derived from the exact same geometry
    // GetShapeOutline uses (RegularPolygon's circumradius etc.), not guessed.
    static void GetTextSafeRect(NoteShape shape, float size, out float width, out float height, out float centerY)
    {
        const float margin = 0.85f; // shrink the geometrically-exact inscribed rect a bit for breathing room
        switch (shape)
        {
            case NoteShape.Circle:
            {
                // Largest square inscribed in a circle of this radius has side = radius*sqrt(2).
                float side = size * 0.5f * 1.41421356f * margin;
                width = side; height = side; centerY = 0f;
                break;
            }
            case NoteShape.Triangle:
            {
                // Apex-up equilateral triangle (GetShapeOutline uses RegularPolygon(size*0.55, 3, 90)).
                // The largest axis-aligned rect that fits sits on the base, centred, spanning
                // the bottom half of the triangle's height at half the base's width.
                float r = size * 0.55f; // circumradius, matches GetShapeOutline
                float baseWidth = r * 1.73205081f; // 2 * r * cos(30deg)
                float fullHeight = r * 1.5f;       // apex to base
                float safeH = fullHeight * 0.5f;
                width = baseWidth * 0.5f * margin;
                height = safeH * margin;
                float baseY = -r * 0.5f;
                centerY = baseY + safeH * 0.5f; // sits on the base, not centred on the triangle's centroid
                break;
            }
            case NoteShape.Hexagon:
            {
                // Flat-top/flat-bottom hexagon (GetShapeOutline uses RegularPolygon(size*0.5, 6, 0)).
                // A rect as wide as the flat top/bottom edge fits the full point-to-point height.
                float r = size * 0.5f;
                width = r * margin;
                height = r * 1.73205081f * margin; // 2 * r * sin(60deg)
                centerY = 0f;
                break;
            }
            case NoteShape.Star:
            {
                // Concave - stay safely inside the inner radius rather than try to use the points.
                float side = size * 0.22f * 1.41421356f * margin;
                width = side; height = side; centerY = 0f;
                break;
            }
            case NoteShape.Rectangle:
            default:
                width = size * margin;
                height = size * 0.7f * margin;
                centerY = 0f;
                break;
        }
    }

    static Vector2[] RectOutline(float width, float height)
    {
        float hw = width * 0.5f, hh = height * 0.5f;
        return new Vector2[]
        {
            new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh), new Vector2(-hw, hh)
        };
    }

    static Vector2[] RegularPolygon(float radius, int sides, float rotationOffsetDeg)
    {
        Vector2[] pts = new Vector2[sides];
        float rot = rotationOffsetDeg * Mathf.Deg2Rad;
        for (int i = 0; i < sides; i++)
        {
            float angle = rot + i * Mathf.PI * 2f / sides;
            pts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return pts;
    }

    static Vector2[] StarOutline(float outerRadius, float innerRadius, int points)
    {
        int n = points * 2;
        Vector2[] pts = new Vector2[n];
        float rot = Mathf.PI / 2f; // top point straight up
        for (int i = 0; i < n; i++)
        {
            float angle = rot + i * Mathf.PI / points;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            pts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        return pts;
    }

    // 4 corner arcs (bottom-right, top-right, top-left, bottom-left), each a quarter
    // circle of cornerRadius, joined into one closed outline.
    static Vector2[] RoundedRectOutline(float width, float height, float cornerRadius, int cornerSegments)
    {
        cornerRadius = Mathf.Min(cornerRadius, Mathf.Min(width, height) * 0.5f);
        float hw = width * 0.5f - cornerRadius;
        float hh = height * 0.5f - cornerRadius;

        Vector2[] centers =
        {
            new Vector2(hw, -hh),
            new Vector2(hw, hh),
            new Vector2(-hw, hh),
            new Vector2(-hw, -hh),
        };
        float[] startAngles = { -90f, 0f, 90f, 180f };

        var pts = new List<Vector2>();
        for (int c = 0; c < 4; c++)
        {
            for (int s = 0; s <= cornerSegments; s++)
            {
                float t = startAngles[c] + (90f * s / cornerSegments);
                float rad = t * Mathf.Deg2Rad;
                pts.Add(centers[c] + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * cornerRadius);
            }
        }
        return pts.ToArray();
    }

    // ---------- Mesh building ----------

    // Extrudes any 2D outline into a flat prism, front cap at local z = -depth/2 (the
    // side facing the viewer) and back cap at z = +depth/2. Assumes the outline is
    // star-shaped with respect to its own centroid (true for every shape used in this
    // file - regular polygons, the star, and rounded rects all qualify), so a simple
    // centroid fan triangulates both caps correctly.
    static Mesh BuildPrismMesh(Vector2[] outline, float depth)
    {
        int n = outline.Length;
        float halfDepth = depth * 0.5f;

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < n; i++) centroid += outline[i];
        centroid /= n;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        int frontCenter = vertices.Count;
        vertices.Add(new Vector3(centroid.x, centroid.y, -halfDepth));
        int frontStart = vertices.Count;
        for (int i = 0; i < n; i++)
            vertices.Add(new Vector3(outline[i].x, outline[i].y, -halfDepth));
        for (int i = 0; i < n; i++)
        {
            int a = frontStart + i;
            int b = frontStart + (i + 1) % n;
            triangles.Add(frontCenter); triangles.Add(b); triangles.Add(a);
        }

        int backCenter = vertices.Count;
        vertices.Add(new Vector3(centroid.x, centroid.y, halfDepth));
        int backStart = vertices.Count;
        for (int i = 0; i < n; i++)
            vertices.Add(new Vector3(outline[i].x, outline[i].y, halfDepth));
        for (int i = 0; i < n; i++)
        {
            int a = backStart + i;
            int b = backStart + (i + 1) % n;
            triangles.Add(backCenter); triangles.Add(a); triangles.Add(b);
        }

        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = outline[i];
            Vector2 p1 = outline[(i + 1) % n];
            int vf0 = vertices.Count; vertices.Add(new Vector3(p0.x, p0.y, -halfDepth));
            int vf1 = vertices.Count; vertices.Add(new Vector3(p1.x, p1.y, -halfDepth));
            int vb0 = vertices.Count; vertices.Add(new Vector3(p0.x, p0.y, halfDepth));
            int vb1 = vertices.Count; vertices.Add(new Vector3(p1.x, p1.y, halfDepth));

            triangles.Add(vf0); triangles.Add(vf1); triangles.Add(vb1);
            triangles.Add(vf0); triangles.Add(vb1); triangles.Add(vb0);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Builds a mesh from `outline` and sizes the object's BoxCollider to match its
    // bounds - transform.localScale is always left at Vector3.one, which is the whole
    // point (see the class comment at the top of this file).
    static void SetupFlatShape(GameObject go, Vector2[] outline, float depth, Color color, string materialKeyOverride = null)
    {
        string safeName = (materialKeyOverride ?? go.name).Replace(" ", "_");

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = SaveMeshAsset(safeName, BuildPrismMesh(outline, depth));

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetOrCreateUnlitMaterial($"{MaterialsDir}/{safeName}_Mat.mat", color);

        go.transform.localScale = Vector3.one;

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector2 p in outline)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(maxX - minX, maxY - minY, depth);
        col.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
    }

    static void SetupRoundedRect(GameObject go, float width, float height, float cornerRadius, float depth, Color color, string materialKeyOverride = null)
    {
        SetupFlatShape(go, RoundedRectOutline(width, height, cornerRadius, 8), depth, color, materialKeyOverride);
    }

    // World-space TMP_InputField, sized in real metres via the canvas's own scale rather
    // than by scaling a UI RectTransform. Text is kept dark/near-black so it stays
    // readable against both bgColor and every shape colour it gets re-tinted to at
    // runtime (see NoteCreationPanel.HighlightShape) - all the shape colours are light
    // pastels, so dark text always has enough contrast. Typing is via a physical
    // keyboard for now (no on-screen keyboard wired up).
    // A tap-to-type text field: a world-space canvas showing the text (or a placeholder),
    // plus a collider and XRSimpleInteractable so a trigger press, poke or Editor mouse
    // click opens the shared VRKeyboard. It used to be a uGUI TMP_InputField, which can't
    // receive XR input in this setup and would only have offered a desktop keyboard.
    static void BuildNoteTextField(Transform parent, string name, Vector3 localPos, Vector2 worldSize, Color bgColor,
        NoteCreationPanel panel, out TMP_Text textDisplay, out GameObject placeholderObject, out Image background)
    {
        GameObject fieldGO = FindOrCreateChild(parent, name);

        // Strip the old input-field setup from scenes built by earlier versions. The
        // GraphicRaycaster goes too: nothing on this canvas needs uGUI pointer events.
        TMP_InputField oldField = fieldGO.GetComponent<TMP_InputField>();
        if (oldField != null) Object.DestroyImmediate(oldField);
        GraphicRaycaster oldRaycaster = fieldGO.GetComponent<GraphicRaycaster>();
        if (oldRaycaster != null) Object.DestroyImmediate(oldRaycaster);
        fieldGO.transform.localPosition = localPos;
        fieldGO.transform.localRotation = Quaternion.identity;
        fieldGO.transform.localScale = Vector3.one * TextCanvasScale;

        Canvas canvas = fieldGO.GetComponent<Canvas>();
        if (canvas == null) canvas = fieldGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = fieldGO.GetComponent<RectTransform>();
        rect.sizeDelta = worldSize / TextCanvasScale;
        if (fieldGO.GetComponent<CanvasRenderer>() == null) fieldGO.AddComponent<CanvasRenderer>();

        GameObject bgGO = FindOrCreateChild(fieldGO.transform, "Background");
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        if (bgRect == null) bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgGO.GetComponent<Image>();
        if (bgImage == null) bgImage = bgGO.AddComponent<Image>();
        bgImage.color = bgColor;

        GameObject textAreaGO = FindOrCreateChild(fieldGO.transform, "TextArea");
        RectTransform textAreaRect = textAreaGO.GetComponent<RectTransform>();
        if (textAreaRect == null) textAreaRect = textAreaGO.AddComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0.05f, 0.05f);
        textAreaRect.anchorMax = new Vector2(0.95f, 0.95f);
        textAreaRect.sizeDelta = Vector2.zero;
        if (textAreaGO.GetComponent<RectMask2D>() == null) textAreaGO.AddComponent<RectMask2D>();

        GameObject placeholderGO = FindOrCreateChild(textAreaGO.transform, "Placeholder");
        TextMeshProUGUI placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
        if (placeholder == null) placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Type your note...";
        placeholder.fontSize = 42;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(146f / 255f, 146f / 255f, 146f / 255f);
        placeholder.alignment = TextAlignmentOptions.TopLeft;
        placeholder.enableWordWrapping = true;
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;

        GameObject textGO = FindOrCreateChild(textAreaGO.transform, "Text");
        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 42;
        text.color = new Color(0.1f, 0.1f, 0.1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        text.text = "";

        // 3D collider so the field can be targeted with an XR ray/poke (and the Editor
        // mouse, see XRButtonPressRouter).
        BoxCollider col = fieldGO.GetComponent<BoxCollider>();
        if (col == null) col = fieldGO.AddComponent<BoxCollider>();
        col.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 10f);
        col.center = Vector3.zero;

        XRSimpleInteractable interactable = fieldGO.GetComponent<XRSimpleInteractable>();
        if (interactable == null) interactable = fieldGO.AddComponent<XRSimpleInteractable>();
        RemovePersistentListeners(interactable.selectEntered);
        UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, panel.EditNoteText);

        textDisplay = text;
        placeholderObject = placeholder.gameObject;
        background = bgImage;
    }

    // ---------- Small helpers ----------

    const string MeshesDir = "Assets/Meshes/Generated";

    // Mesh asset paths handed out during the current Build(), so two objects that share a
    // name key but have different geometry get separate files instead of overwriting.
    static readonly Dictionary<string, Mesh> meshesThisBuild = new Dictionary<string, Mesh>();

    // Stores a generated mesh as an asset file and returns the asset. Rebuilding updates
    // the existing file in place, so the scene and prefabs keep pointing at the same
    // asset and an unchanged shape produces no diff. (Meshes used to be created fresh
    // inside the scene on every run, which rewrote thousands of scene lines each time.)
    static Mesh SaveMeshAsset(string key, Mesh mesh)
    {
        Directory.CreateDirectory(MeshesDir);

        string path = $"{MeshesDir}/{key}.asset";
        for (int n = 2; meshesThisBuild.TryGetValue(path, out Mesh taken); n++)
        {
            if (SameGeometry(taken, mesh)) { Object.DestroyImmediate(mesh); return taken; }
            path = $"{MeshesDir}/{key}_{n}.asset";
        }

        mesh.name = Path.GetFileNameWithoutExtension(path);
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            existing = mesh;
        }
        else
        {
            if (!SameGeometry(existing, mesh))
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
            }
            Object.DestroyImmediate(mesh);
        }

        meshesThisBuild[path] = existing;
        return existing;
    }

    static bool SameGeometry(Mesh a, Mesh b)
    {
        if (a.vertexCount != b.vertexCount) return false;
        Vector3[] va = a.vertices, vb = b.vertices;
        for (int i = 0; i < va.Length; i++)
            if ((va[i] - vb[i]).sqrMagnitude > 1e-12f) return false;
        int[] ta = a.triangles, tb = b.triangles;
        if (ta.Length != tb.Length) return false;
        for (int i = 0; i < ta.Length; i++)
            if (ta[i] != tb[i]) return false;
        return true;
    }

    static void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static Material GetOrCreateUnlitMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // Same as GetOrCreateUnlitMaterial but configured as a URP "Transparent" surface, so
    // the colour's alpha actually renders see-through instead of being ignored (URP Lit's
    // default Opaque surface type ignores alpha entirely). Used for the plus-button sphere
    // and the yellow hover-glow spheres.
    static Material GetOrCreateTransparentMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Blend", 0f);   // Alpha blend
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.4f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // Looks up a top-level object by name, including inactive ones (GameObject.Find skips
    // inactive objects, which is how duplicate VRKeyboards got created on reruns).
    static GameObject FindRoot(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    static GameObject FindOrCreate(string name)
    {
        GameObject go = FindRoot(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    // World-space uGUI (the Tour Mode list) only receives XR controller rays and pokes
    // through XRI's XRUIInputModule; the Input System's own UI module ignores them.
    // XRUIInputModule still handles the mouse, so the screen-space test button keeps working.
    static void EnsureXRUIEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null) eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        GameObject esGO = eventSystem.gameObject;

        foreach (BaseInputModule module in esGO.GetComponents<BaseInputModule>())
            if (!(module is XRUIInputModule)) Object.DestroyImmediate(module);
        if (esGO.GetComponent<XRUIInputModule>() == null) esGO.AddComponent<XRUIInputModule>();
    }

    // Deletes extra copies of the top-level objects this builder owns, keeping the first.
    static void RemoveDuplicateRoots()
    {
        string[] owned = { "VRKeyboard", "PromptNote", "NoteCreationPanel", "ClapManager", "TourMode", "FootTrashBin" };
        var seen = new HashSet<string>();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (System.Array.IndexOf(owned, root.name) < 0) continue;
            if (seen.Add(root.name)) continue;
            Debug.Log($"IP2aSceneBuilder: removing duplicate '{root.name}'.");
            Object.DestroyImmediate(root);
        }
    }

    // Entry point for running the builder without opening the Editor UI:
    // Unity.exe -batchmode -quit -projectPath <project> -executeMethod IP2aSceneBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/IP2a.unity", OpenSceneMode.Single);
        Build();
        LogDuplicateSiblings();
    }

    // Logs any two objects with the same name under the same parent - a sign of leftover
    // copies from earlier builder runs.
    static void LogDuplicateSiblings()
    {
        int found = 0;
        var stack = new Stack<Transform>();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects()) stack.Push(root.transform);
        var rootNames = new HashSet<string>();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (!rootNames.Add(root.name)) { Debug.LogWarning($"IP2aSceneBuilder: duplicate root '{root.name}'"); found++; }

        while (stack.Count > 0)
        {
            Transform t = stack.Pop();
            var names = new HashSet<string>();
            foreach (Transform child in t)
            {
                if (!names.Add(child.name))
                {
                    Debug.LogWarning($"IP2aSceneBuilder: duplicate child '{child.name}' under '{GetPath(t)}'");
                    found++;
                }
                stack.Push(child);
            }
        }
        Debug.Log($"IP2aSceneBuilder: duplicate check finished, {found} found.");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.gameObject;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void RemovePersistentListeners(UnityEngine.Events.UnityEventBase evt)
    {
        if (evt == null) return;
        while (evt.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(evt, 0);
    }

    static int EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < layersProp.arraySize; i++)
        {
            if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                return i;
        }
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        throw new System.Exception("IP2aSceneBuilder: no free layer slots for '" + layerName + "'.");
    }

    static void EnsureTag(string tagName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
