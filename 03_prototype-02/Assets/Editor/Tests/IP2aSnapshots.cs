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
    const string ScenePath = "Assets/Scenes/IP2a.unity";

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
        VRHandleConnector from = shortNote.GetComponentInChildren<VRHandleConnector>();
        VRHandleConnector to = longNote.GetComponentInChildren<VRHandleConnector>();
        ConnectionLine line = Object.Instantiate(from.linePrefab).GetComponent<ConnectionLine>();
        line.pointA = from.transform;
        line.pointB = to.transform;
        GameObject label = Object.Instantiate(from.labelPrefab, line.transform);
        label.GetComponent<LineLabelFollow>().line = line.GetComponent<LineRenderer>();
        label.GetComponentInChildren<VRPlusButtonClick>(true).gameObject.SetActive(false);
        TMPro.TMP_Text labelText = label.GetComponentInChildren<VRLabelEdit>(true).GetComponent<TMPro.TMP_Text>();
        labelText.gameObject.SetActive(true);
        labelText.text = "leads to";
        yield return null;
        yield return null;

        Save(cam, "notes_and_label.png");
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
