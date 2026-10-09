using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Testing aids for the headset, active only in the Editor and in Development Builds
// (never in a normal build that participants use). Created automatically on scene load.
//
//  - Right controller A: spawn a "dummy text" test note in front of you, without
//    needing the creation panel's buttons.
//  - Right controller B: show/hide a debug readout floating below your view, listing for
//    each controller what its ray is hovering and whether trigger (activate) and grip
//    (select) are registering. Use it to see why a button doesn't respond.
public class DevControls : MonoBehaviour
{
    InputAction spawnNote;
    InputAction toggleReadout;

    TextMeshPro readout;
    XRInteractionManager manager;
    readonly List<IXRInteractor> interactors = new List<IXRInteractor>();
    readonly StringBuilder text = new StringBuilder();
    int notesSpawned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInstance()
    {
        if (!Debug.isDebugBuild || FindFirstObjectByType<DevControls>() != null) return;
        GameObject go = new GameObject(nameof(DevControls));
        DontDestroyOnLoad(go);
        go.AddComponent<DevControls>();
    }

    void OnEnable()
    {
        spawnNote = new InputAction("DevSpawnNote", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
        toggleReadout = new InputAction("DevToggleReadout", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
        spawnNote.Enable();
        toggleReadout.Enable();
    }

    void OnDisable()
    {
        spawnNote?.Disable();
        toggleReadout?.Disable();
    }

    void Update()
    {
        if (spawnNote.WasPressedThisFrame()) SpawnTestNote();
        if (toggleReadout.WasPressedThisFrame()) ToggleReadout();
        if (readout != null && readout.gameObject.activeSelf) RefreshReadout();
    }

    void SpawnTestNote()
    {
        NoteCreationPanel panel = FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        if (panel == null || panel.headTransform == null) return;

        NoteData[] before = FindObjectsByType<NoteData>(FindObjectsSortMode.None);
        panel.CreateTestNote();

        // Fan successive test notes out sideways so they don't stack on one spot.
        foreach (NoteData note in FindObjectsByType<NoteData>(FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(before, note) >= 0) continue;
            float side = ((notesSpawned % 5) - 2) * 0.3f;
            note.transform.position += panel.headTransform.right * side;
        }
        notesSpawned++;
    }

    void ToggleReadout()
    {
        if (readout == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            GameObject go = new GameObject("DevReadout");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.3f, 0.8f);
            go.transform.localScale = Vector3.one * 0.03f;
            readout = go.AddComponent<TextMeshPro>();
            readout.fontSize = 3f;
            readout.alignment = TextAlignmentOptions.Center;
            readout.color = new Color(0.1f, 0.1f, 0.6f);
            readout.rectTransform.sizeDelta = new Vector2(40f, 12f);
            return;
        }
        readout.gameObject.SetActive(!readout.gameObject.activeSelf);
    }

    void RefreshReadout()
    {
        if (manager == null) manager = FindFirstObjectByType<XRInteractionManager>();
        text.Clear();
        if (manager == null)
        {
            readout.text = "No XRInteractionManager in scene";
            return;
        }

        manager.GetRegisteredInteractors(interactors);
        foreach (IXRInteractor interactor in interactors)
        {
            if (!(interactor is XRBaseInputInteractor input) || !input.isActiveAndEnabled) continue;
            string hovered = input.interactablesHovered.Count > 0
                ? ((Component)input.interactablesHovered[0]).name
                : "-";
            bool trigger = input.activateInput != null && input.activateInput.ReadIsPerformed();
            bool grip = input.selectInput != null && input.selectInput.ReadIsPerformed();
            text.Append(input.name)
                .Append(": hover ").Append(hovered)
                .Append("  trig ").Append(trigger ? "ON" : "off")
                .Append("  grip ").Append(grip ? "ON" : "off");

            // What the ray physically points at, independent of XRI's hover logic, so we
            // can tell "ray doesn't reach the panel" apart from "XRI ignores what it hits".
            if (input is IXRRayProvider ray)
            {
                Transform origin = ray.GetOrCreateRayOrigin();
                string phys = Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Collide)
                    ? $"{hit.collider.name} {hit.distance:0.00}m (layer {hit.collider.gameObject.layer})"
                    : "nothing";
                text.Append("\n   physics hit: ").Append(phys)
                    .Append("  ray end: ").Append(Vector3.Distance(origin.position, ray.rayEndPoint).ToString("0.00")).Append("m");
            }
            text.Append('\n');
        }

        NoteCreationPanel panel = FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        if (panel != null && panel.gameObject.activeInHierarchy && panel.headTransform != null)
            text.Append("panel ").Append(Vector3.Distance(panel.headTransform.position, panel.transform.position).ToString("0.00")).Append("m away  ");
        VRTourMode tour = FindFirstObjectByType<VRTourMode>();
        if (tour != null) text.Append("tour menu ").Append(tour.IsMenuOpen ? "OPEN" : "closed");

        readout.text = text.Length > 0 ? text.ToString() : "No input interactors found";

        // Also goes to the device log (adb logcat -s Unity), once a second.
        if (Time.unscaledTime >= nextLogTime)
        {
            nextLogTime = Time.unscaledTime + 1f;
            Debug.Log("[DevReadout] " + readout.text.Replace("\n", " | "));
        }
    }

    float nextLogTime;
}
