using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Transform[] shapeButtons;     // each shape button's root transform (scaled up when selected)
    public GameObject[] shapeGlowRings;  // each shape button's glow child (shown when selected)
    public Color[] shapeColors;          // same colors as the buttons - used to tint the text field

    [Header("Note text input")]
    public TMP_InputField noteTextField;
    public Color defaultFieldColor = new Color(245f / 255f, 245f / 255f, 245f / 255f); // #F5F5F5, no shape picked yet
    // NOTE: the note text stays dark/near-black and the placeholder is #929292 so both
    // stay readable against this near-white default AND every shape colour, which are
    // all light pastels - see BuildTextInputField in IP2aSceneBuilder.cs.

    [Header("Media attachment buttons - visual only for now, wired up later")]
    public GameObject[] mediaButtons;    // Add Image, Add Video, Add Document
    public Color mediaActiveColor = new Color(0.15f, 0.15f, 0.15f);
    public Color mediaInactiveColor = new Color(0.65f, 0.65f, 0.65f);

    [Header("Placeholder media sprites")]
    public Sprite imagePlaceholder;
    public Sprite documentPlaceholder;
    public Sprite videoPlaceholder;

    NoteShape pendingShape = NoteShape.Rectangle;
    MediaType pendingMedia = MediaType.None;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        if (headTransform == null) return;

        pendingShape = NoteShape.Rectangle;
        pendingMedia = MediaType.None;
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

    // Wire these to the shape buttons' selectEntered, one integer per button (0=Rectangle...4=Star)
    public void SetPendingShape(int shapeIndex)
    {
        pendingShape = (NoteShape)shapeIndex;
        HighlightShape(shapeIndex);
    }

    void HighlightShape(int selectedIndex)
    {
        if (shapeButtons != null)
        {
            for (int i = 0; i < shapeButtons.Length; i++)
            {
                bool selected = i == selectedIndex;
                if (shapeButtons[i] != null)
                    shapeButtons[i].localScale = Vector3.one * (selected ? 1.18f : 1f);
                if (shapeGlowRings != null && i < shapeGlowRings.Length && shapeGlowRings[i] != null)
                    shapeGlowRings[i].SetActive(selected);
            }
        }

        // Text field tints to match the picked shape, grey until one is picked.
        if (noteTextField != null && noteTextField.targetGraphic != null)
        {
            Color fieldColor = defaultFieldColor;
            if (selectedIndex >= 0 && shapeColors != null && selectedIndex < shapeColors.Length)
                fieldColor = shapeColors[selectedIndex];
            noteTextField.targetGraphic.color = fieldColor;
        }

        // Add Image / Add Video / Add Document only look active for Rectangle notes.
        UpdateMediaButtonsState(selectedIndex == (int)NoteShape.Rectangle);
    }

    void UpdateMediaButtonsState(bool active)
    {
        if (mediaButtons == null) return;
        Color tint = active ? mediaActiveColor : mediaInactiveColor;
        foreach (GameObject btn in mediaButtons)
        {
            if (btn == null) continue;
            Renderer r = btn.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = tint; // .material (not sharedMaterial) auto-instances so buttons don't tint each other
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = tint;
        }
    }

    // Wire these to the media buttons' interaction once they're made clickable (0=None,1=Image,2=Document,3=Video)
    public void SetPendingMedia(int mediaIndex)
    {
        pendingMedia = (MediaType)mediaIndex;
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
