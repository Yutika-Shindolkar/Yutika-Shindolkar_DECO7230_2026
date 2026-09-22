#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Adds a plain screen-space UI button that fires the same thing a real clap
// does. Desktop-editor testing convenience only (mouse click, no XR
// interactor / simulator gymnastics required) - safe to leave in the scene.
// It sits over the "Clap to create a note" text, hides itself the moment the
// panel opens (via TestClapButtonVisibility), and reappears once the panel
// closes so you can test again without rerunning this tool.
// Run via Tools > IP2a > Add Test Clap Button with the IP2a scene open.
public static class IP2aTestClapButton
{
    [MenuItem("Tools/IP2a/Add Test Clap Button")]
    public static void Add()
    {
        GameObject promptNoteGO = GameObject.Find("PromptNote");
        GameObject panelGO = GameObject.Find("NoteCreationPanel");
        if (promptNoteGO == null || panelGO == null)
        {
            Debug.LogError("IP2aTestClapButton: couldn't find 'PromptNote' and/or 'NoteCreationPanel' in the open scene.");
            return;
        }

        PromptNote promptNote = promptNoteGO.GetComponent<PromptNote>();
        NoteCreationPanel panel = panelGO.GetComponent<NoteCreationPanel>();
        if (promptNote == null || panel == null)
        {
            Debug.LogError("IP2aTestClapButton: missing PromptNote or NoteCreationPanel component.");
            return;
        }

        // --- Canvas (screen space overlay, so it's just a normal mouse-clickable button) ---
        GameObject canvasGO = GameObject.Find("TestClapButtonCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("TestClapButtonCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Make sure there's an EventSystem so the click actually gets routed
        // (usually auto-created by Unity when a Canvas needs one, but don't
        // rely on that silently - create it explicitly so this works even if
        // this is the first Canvas ever added to the scene).
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
        }

        // --- Button ---
        GameObject btnGO = GameObject.Find("TestClapButton");
        if (btnGO == null)
        {
            btnGO = new GameObject("TestClapButton");
            btnGO.transform.SetParent(canvasGO.transform, false);
        }

        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        if (btnRect == null) btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        // Roughly where "Clap to create a note" sits on screen at Play start.
        btnRect.anchoredPosition = new Vector2(0f, 90f);
        btnRect.sizeDelta = new Vector2(260, 64);

        Image btnImage = btnGO.GetComponent<Image>();
        if (btnImage == null) btnImage = btnGO.AddComponent<Image>();
        btnImage.color = new Color(1f, 1f, 1f, 0.85f);

        Button button = btnGO.GetComponent<Button>();
        if (button == null) button = btnGO.AddComponent<Button>();
        button.targetGraphic = btnImage;

        GameObject labelGO = null;
        if (btnGO.transform.childCount > 0)
            labelGO = btnGO.transform.GetChild(0).gameObject;
        if (labelGO == null)
        {
            labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
        }
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        if (label == null) label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "TEST: Click to Clap";
        label.color = Color.black;
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        // Wire the click to exactly what a real clap does (mirrors the
        // ClapGestureDetector.onClap wiring in IP2aSceneBuilder).
        RemovePersistentListeners(button.onClick);
        UnityEventTools.AddVoidPersistentListener(button.onClick, promptNote.Hide);
        UnityEventTools.AddVoidPersistentListener(button.onClick, panel.OpenPanel);

        // Hide the button while the panel is open, bring it back once closed.
        TestClapButtonVisibility visibility = canvasGO.GetComponent<TestClapButtonVisibility>();
        if (visibility == null) visibility = canvasGO.AddComponent<TestClapButtonVisibility>();
        visibility.panel = panelGO;
        visibility.button = btnGO;

        EditorUtility.SetDirty(canvasGO);
        EditorUtility.SetDirty(btnGO);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("IP2aTestClapButton: test button added/updated, wired, and set to auto-hide while the panel is open.");
    }

    static void RemovePersistentListeners(UnityEngine.Events.UnityEventBase evt)
    {
        if (evt == null) return;
        while (evt.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(evt, 0);
    }
}
#endif
