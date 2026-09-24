using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// World-space panel that appears after a clap. Lets the user pick a shape, type a
// short note, and an optional placeholder media attachment, then Discard or Create.
// The mic is a permanent fixture mounted on the panel (see MicBoomConstraint) rather
// than something spawned from a button, so there's no SpawnMic()/micPrefab here anymore.
public class NoteCreationPanel : MonoBehaviour
{
    [Header("Placement")]
    public Transform headTransform;      // the VR camera
    public float spawnDistance = 1.0f; // was 0.8 - still felt too close once actually in Play mode/headset

    [Header("Note prefabs, indexed to match the NoteShape enum order")]
    public GameObject[] shapePrefabs;    // Rectangle, Circle, Triangle, Hexagon, Star

    [Header("Shape picker visuals, indexed to match shapePrefabs")]
    public Transform[] shapeButtons;     // each shape button's root transform (scaled up when selected/hovered)
    public GameObject[] shapeGlowRings;  // each shape button's glow child (shown on hover AND selection)
    public Color[] shapeColors;          // same colors as the buttons - used to tint the text field

    [Header("Note text input")]
    public TMP_InputField noteTextField;
    public Color defaultFieldColor = new Color(245f / 255f, 245f / 255f, 245f / 255f); // #F5F5F5, no shape picked yet
    // NOTE: the note text stays dark/near-black and the placeholder is #929292 so both
    // stay readable against this near-white default AND every shape colour, which are
    // all light pastels - see BuildTextInputField in IP2aSceneBuilder.cs.

    [Header("Media attachment buttons - only clickable once Rectangle is the picked shape")]
    public GameObject[] mediaButtons;    // Add Image, Add Video, Add Document
    public Color mediaActiveColor = new Color(0.15f, 0.15f, 0.15f);   // label, active + not the chosen one
    public Color mediaInactiveColor = new Color(0.65f, 0.65f, 0.65f); // label, not clickable yet
    public Color mediaSelectedColor = Color.white;                     // label, this is the chosen media type
    public Color mediaBgInactive = new Color(0.82f, 0.82f, 0.82f);
    public Color mediaBgActive = new Color(0.92f, 0.92f, 0.92f);
    public Color mediaBgSelected = new Color(0.90f, 0.55f, 0.45f); // matches the Rectangle shape's own accent

    [Header("Placeholder media sprites")]
    public Sprite imagePlaceholder;
    public Sprite documentPlaceholder;
    public Sprite videoPlaceholder;

    // mediaButtons[] order is Add Image / Add Video / Add Document (see IP2aSceneBuilder) -
    // this is the matching MediaType for each slot, used to test/set "is this one chosen".
    static readonly MediaType[] MediaOrder = { MediaType.Image, MediaType.Video, MediaType.Document };

    NoteShape pendingShape = NoteShape.Rectangle;
    MediaType pendingMedia = MediaType.None;
    int selectedShapeIndex = -1;
    int hoveredShapeIndex = -1;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        if (headTransform == null) return;

        pendingShape = NoteShape.Rectangle;
        pendingMedia = MediaType.None;
        hoveredShapeIndex = -1;
        HighlightShape(-1); // nothing glows and the field is the default grey until a shape is picked
        if (noteTextField != null) noteTextField.text = "";

        Vector3 pos = headTransform.position + headTransform.forward * spawnDistance;
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(transform.position - headTransform.position);

        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // Wired to the red "Discard" button. Same as closing for now - if you later want
    // a confirmation step before losing typed text, add it here rather than in ClosePanel.
    public void DiscardPanel()
    {
        ClosePanel();
    }

    // Wired to each shape button's selectEntered, one integer per button (0=Rectangle...4=Star).
    public void SetPendingShape(int shapeIndex)
    {
        pendingShape = (NoteShape)shapeIndex;
        HighlightShape(shapeIndex);
    }

    // Wired to each shape button's hoverEntered/hoverExited, so the shapes glow under the
    // pointer even before you click one - the click (select) state below still wins if a
    // shape is already picked, hover on top of it doesn't un-pick anything.
    public void SetHoverShape(int shapeIndex)
    {
        hoveredShapeIndex = shapeIndex;
        RefreshShapeVisuals();
    }

    public void ClearHoverShape(int shapeIndex)
    {
        if (hoveredShapeIndex == shapeIndex) hoveredShapeIndex = -1;
        RefreshShapeVisuals();
    }

    void HighlightShape(int selectedIndex)
    {
        selectedShapeIndex = selectedIndex;
        // Media only makes sense on a Rectangle note in this design - switching to any
        // other shape (or back to nothing picked) drops whatever media was chosen so the
        // buttons' inactive look always matches what's actually going to be created.
        if (selectedShapeIndex != (int)NoteShape.Rectangle) pendingMedia = MediaType.None;
        RefreshShapeVisuals();
    }

    void RefreshShapeVisuals()
    {
        if (shapeButtons != null)
        {
            for (int i = 0; i < shapeButtons.Length; i++)
            {
                bool selected = i == selectedShapeIndex;
                bool hovered = i == hoveredShapeIndex;
                if (shapeButtons[i] != null)
                    shapeButtons[i].localScale = Vector3.one * (selected ? 1.18f : hovered ? 1.08f : 1f);
                if (shapeGlowRings != null && i < shapeGlowRings.Length && shapeGlowRings[i] != null)
                    shapeGlowRings[i].SetActive(selected || hovered);
            }
        }

        // Text field tints to match the picked shape, grey until one is picked.
        if (noteTextField != null && noteTextField.targetGraphic != null)
        {
            Color fieldColor = defaultFieldColor;
            if (selectedShapeIndex >= 0 && shapeColors != null && selectedShapeIndex < shapeColors.Length)
                fieldColor = shapeColors[selectedShapeIndex];
            noteTextField.targetGraphic.color = fieldColor;
        }

        // Add Image / Add Video / Add Document only become clickable for Rectangle notes.
        RefreshMediaVisuals(selectedShapeIndex == (int)NoteShape.Rectangle);
    }

    void RefreshMediaVisuals(bool active)
    {
        if (mediaButtons == null) return;
        for (int i = 0; i < mediaButtons.Length; i++)
        {
            GameObject btn = mediaButtons[i];
            if (btn == null) continue;

            bool selected = active && i < MediaOrder.Length && pendingMedia == MediaOrder[i];

            // The button root itself carries the background mesh's Renderer (see
            // SetupRoundedRect/SetupFlatShape) - GetComponent, not GetComponentInChildren,
            // so this never picks up the Label's own text-mesh renderer by accident.
            Renderer r = btn.GetComponent<Renderer>();
            if (r != null) r.material.color = selected ? mediaBgSelected : (active ? mediaBgActive : mediaBgInactive);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = selected ? mediaSelectedColor : (active ? mediaActiveColor : mediaInactiveColor);

            // Only actually interactable once a Rectangle is picked - not just tinted to
            // look that way, so it can't be poked/rayed while "inactive".
            XRSimpleInteractable interactable = btn.GetComponent<XRSimpleInteractable>();
            if (interactable != null) interactable.enabled = active;
        }
    }

    // Wired to each media button's selectEntered (1=Image, 3=Video, 2=Document - the
    // MediaType int values, not the button's row position). Click the already-chosen one
    // again to clear it, like a radio group that allows "none".
    public void SetPendingMedia(int mediaIndex)
    {
        if (selectedShapeIndex != (int)NoteShape.Rectangle) return; // belt and braces - the collider should already be disabled
        MediaType newMedia = (MediaType)mediaIndex;
        pendingMedia = (pendingMedia == newMedia) ? MediaType.None : newMedia;
        RefreshMediaVisuals(true);
    }

    // Wired to the "Create Test Note" button - spawns a fixed Rectangle note with
    // "dummy text" straight away, skipping the shape pick and typing, purely so testing
    // connections/labels doesn't require clapping + filling the form out every time.
    public void CreateTestNote()
    {
        int index = (int)NoteShape.Rectangle;
        if (shapePrefabs == null || index >= shapePrefabs.Length || shapePrefabs[index] == null)
        {
            Debug.LogWarning("NoteCreationPanel: no Rectangle prefab assigned, can't create a test note.");
            return;
        }

        Vector3 spawnPos = headTransform.position + headTransform.forward * spawnDistance;
        GameObject note = Instantiate(shapePrefabs[index], spawnPos, Quaternion.identity);

        NoteData data = note.GetComponent<NoteData>();
        if (data != null) data.SetLabel("dummy text");
    }

    // Wired to the green "Create" button.
    public void ConfirmCreate()
    {
        int index = (int)pendingShape;
        if (shapePrefabs == null || index < 0 || index >= shapePrefabs.Length || shapePrefabs[index] == null)
        {
            Debug.LogWarning("NoteCreationPanel: no prefab assigned for shape " + pendingShape);
            ClosePanel();
            return;
        }

        Vector3 spawnPos = headTransform.position + headTransform.forward * spawnDistance;
        GameObject note = Instantiate(shapePrefabs[index], spawnPos, Quaternion.identity);

        NoteData data = note.GetComponent<NoteData>();
        if (data != null)
        {
            data.SetLabel(noteTextField != null ? noteTextField.text : "");

            Sprite placeholder = pendingMedia switch
            {
                MediaType.Image => imagePlaceholder,
                MediaType.Document => documentPlaceholder,
                MediaType.Video => videoPlaceholder,
                _ => null
            };
            data.SetMedia(pendingMedia, placeholder);
        }

        ClosePanel();
    }
}
