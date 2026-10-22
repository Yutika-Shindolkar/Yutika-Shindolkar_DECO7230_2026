using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A plain window that shows the archived notes in a grid. The drawer-pull gesture opens it in
// front of the player and the same gesture closes it. It stays where it opened. Grab empty
// space on the window and drag up or down to scroll; the grid settles on whole rows when you
// let go. Each archived note is shown as a display copy sized to fit its cell, while the real
// note stays hidden in the archive.
public class ArchiveWindow : MonoBehaviour
{
    public Transform headTransform;
    public GameObject card;                    // the window itself, shown while open
    public Transform content;                  // display copies live here, in the card's space
    public TMP_Text header;                    // "Archive (n)"
    public GameObject emptyLabel;              // shown when nothing is archived
    public XRSimpleInteractable scrollSurface; // the card's grab area

    [Header("Placement")]
    public float distance = 1.0f;              // metres ahead of the player
    public float heightOffset = -0.05f;        // window centre relative to the head

    [Header("Grid, in metres in the card's own space")]
    public int columns = 4;
    public int visibleRows = 3;
    public float cellPitch = 0.2f;
    public float cellSize = 0.17f;
    public float viewportTop = 0.25f;          // top edge of the area the grid shows in
    public float itemZ = -0.03f;               // in front of the card

    readonly List<GameObject> items = new List<GameObject>();
    float scroll;                              // metres the grid has moved up
    IXRSelectInteractor dragger;
    float dragStartHandY;
    float dragStartScroll;

    public bool IsOpen => card != null && card.activeSelf;
    public int ItemCount => items.Count;
    public float Scroll => scroll;
    public int VisibleItemCount
    {
        get
        {
            int n = 0;
            foreach (GameObject item in items) if (item.activeSelf) n++;
            return n;
        }
    }

    void Awake()
    {
        if (card != null) card.SetActive(false);
    }

    void OnEnable()
    {
        ArchiveDrawerGesture.Pulled += OnPulled;
        NoteArchive.Changed += OnArchiveChanged;
        if (scrollSurface != null)
        {
            scrollSurface.selectEntered.AddListener(OnGrabbed);
            scrollSurface.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        ArchiveDrawerGesture.Pulled -= OnPulled;
        NoteArchive.Changed -= OnArchiveChanged;
        if (scrollSurface != null)
        {
            scrollSurface.selectEntered.RemoveListener(OnGrabbed);
            scrollSurface.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnPulled(Transform hand) => Toggle();

    void OnArchiveChanged()
    {
        if (IsOpen) Rebuild();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (headTransform == null || card == null) return;

        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        transform.position = headTransform.position + forward * distance + Vector3.up * heightOffset;
        transform.rotation = Quaternion.LookRotation(forward); // faces the player, like the creation panel

        scroll = 0f;
        card.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        dragger = null;
        ClearItems();
        if (card != null) card.SetActive(false);
    }

    public void SetScroll(float metres)
    {
        scroll = Mathf.Clamp(metres, 0f, MaxScroll);
        Layout();
    }

    float MaxScroll => Mathf.Max(0, Mathf.CeilToInt(items.Count / (float)columns) - visibleRows) * cellPitch;

    // ---- Display copies ----

    void Rebuild()
    {
        ClearItems();
        foreach (NoteData note in NoteArchive.Notes)
            if (note != null) items.Add(MakeDisplayCopy(note));
        scroll = Mathf.Clamp(scroll, 0f, MaxScroll);
        if (header != null) header.text = $"Archive ({items.Count})";
        if (emptyLabel != null) emptyLabel.SetActive(items.Count == 0);
        Layout();
    }

    void ClearItems()
    {
        foreach (GameObject item in items) if (item != null) Destroy(item);
        items.Clear();
    }

    // A copy of the note with everything interactive taken off, scaled to fit its cell.
    GameObject MakeDisplayCopy(NoteData note)
    {
        Vector2 size = NoteArchive.HalfSize(note) * 2f;
        GameObject copy = Instantiate(note.gameObject, content, false); // archived notes are switched off, so the copy starts off too
        copy.name = "ArchivedNote";
        copy.tag = "Untagged";

        // Connection handles go entirely; other scripts and interactables go before the
        // colliders and the Rigidbody they depend on.
        foreach (VRHandleConnector handle in copy.GetComponentsInChildren<VRHandleConnector>(true))
            DestroyImmediate(handle.gameObject);
        var behaviours = new List<MonoBehaviour>(copy.GetComponentsInChildren<MonoBehaviour>(true));
        foreach (MonoBehaviour mb in behaviours)
            if (mb != null && (!(mb is UIBehaviour) || mb is BaseRaycaster) && !(mb is XRBaseInteractable)) DestroyImmediate(mb);
        foreach (MonoBehaviour mb in behaviours)
            if (mb != null && mb is XRBaseInteractable) DestroyImmediate(mb);
        foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        foreach (Rigidbody rb in copy.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);

        float fit = Mathf.Min(1f, cellSize / Mathf.Max(0.01f, size.x), cellSize / Mathf.Max(0.01f, size.y));
        copy.transform.localRotation = Quaternion.identity;
        copy.transform.localScale = note.transform.lossyScale * fit;
        copy.SetActive(true);
        return copy;
    }

    // Puts each copy in its cell, moved up by the scroll, and hides the ones that would
    // hang over the top or bottom edge of the window.
    void Layout()
    {
        float bottom = viewportTop - visibleRows * cellPitch;
        float tolerance = cellPitch * 0.15f;
        for (int i = 0; i < items.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float x = (col - (columns - 1) * 0.5f) * cellPitch;
            float y = viewportTop - cellPitch * 0.5f - row * cellPitch + scroll;
            items[i].transform.localPosition = new Vector3(x, y, itemZ);
            items[i].SetActive(y >= bottom + cellPitch * 0.5f - tolerance && y <= viewportTop - cellPitch * 0.5f + tolerance);
        }
    }

    // ---- Dragging empty space to scroll ----

    void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        dragger = args.interactorObject;
        dragStartHandY = HandY();
        dragStartScroll = scroll;
    }

    void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        dragger = null;
    }

    // Height of the dragging hand in the card's space.
    float HandY()
    {
        Transform attach = dragger.GetAttachTransform(scrollSurface);
        return card.transform.InverseTransformPoint(attach.position).y;
    }

    void Update()
    {
        if (!IsOpen || items.Count == 0) return;

        if (dragger != null)
        {
            scroll = Mathf.Clamp(dragStartScroll + HandY() - dragStartHandY, 0f, MaxScroll);
        }
        else
        {
            // Settle on a whole row.
            float target = Mathf.Clamp(Mathf.Round(scroll / cellPitch) * cellPitch, 0f, MaxScroll);
            scroll = Mathf.MoveTowards(scroll, target, cellPitch * 4f * Time.unscaledDeltaTime);
        }
        Layout();
    }
}
