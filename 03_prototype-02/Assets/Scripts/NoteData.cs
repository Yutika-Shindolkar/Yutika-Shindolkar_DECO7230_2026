using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum NoteShape { Rectangle, Circle, Triangle, Hexagon, Star }
public enum MediaType { None, Image, Document, Video }

// Holds the state of a single note: its shape, text, and any attached placeholder media.
// Attach this to the root of every note prefab.
public class NoteData : MonoBehaviour
{
    public NoteShape shape = NoteShape.Rectangle;
    public TMP_Text noteText;

    public MediaType attachedMedia = MediaType.None;
    public Image mediaThumbnail; // shown/hidden depending on attachedMedia

    [Header("Media card: the note's parts, resized to fit the media and text")]
    public Transform visual;            // the flat shape mesh, stretched to the card size
    public BoxCollider noteCollider;    // the note's grab box
    public RectTransform canvasRect;    // holds the media and the text
    public float maxMediaWidth = 0.3f;  // metres: media fills this width...
    public float maxMediaHeight = 0.3f; // ...unless that makes it taller than this
    public float minCardWidth = 0.22f;  // a tall, narrow image still gets a note-sized card
    public float cardPadding = 0.015f;
    public float textGap = 0.012f;      // between the media and the text below it
    public float maxTextHeight = 0.12f; // past this the text shrinks to fit

    public void SetMedia(MediaType type, Sprite placeholderSprite)
    {
        attachedMedia = type;
        if (mediaThumbnail == null) return;

        mediaThumbnail.gameObject.SetActive(type != MediaType.None);
        if (placeholderSprite != null) mediaThumbnail.sprite = placeholderSprite;
        if (type != MediaType.None) LayoutMediaCard();
    }

    [Header("Grow to fit text")]
    public float textFontSize = 20f; // note text size (canvas units at scale 1); kept the same as the note grows
    public float maxScale = 2.2f;    // the shape stops growing here; past it, text shrinks to fit
    public float minFontSize = 6f;

    public void SetLabel(string text)
    {
        if (noteText == null) return;
        noteText.text = text;
        if (attachedMedia != MediaType.None) LayoutMediaCard();
        else FitToText();
    }

    // A note with media becomes a card: the media across the top at full card width (its
    // height from its aspect ratio, both capped by maxMediaWidth/maxMediaHeight), the text
    // underneath, and the shape, grab box and handles resized around them. Unlike
    // FitToText, the note keeps scale 1 and only the card's height grows with the text.
    void LayoutMediaCard()
    {
        if (visual == null || canvasRect == null || mediaThumbnail == null) return;
        transform.localScale = Vector3.one;
        float canvasScale = canvasRect.localScale.x;

        // Media size: fill the width, then fit the height cap if it's a tall picture.
        Sprite sprite = mediaThumbnail.sprite;
        float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 4f / 3f;
        float mediaWidth = maxMediaWidth;
        float mediaHeight = mediaWidth / aspect;
        if (mediaHeight > maxMediaHeight)
        {
            mediaHeight = maxMediaHeight;
            mediaWidth = mediaHeight * aspect;
        }
        float cardWidth = Mathf.Max(mediaWidth, minCardWidth - 2f * cardPadding) + 2f * cardPadding;

        // Text height at its normal size, wrapped to the card width.
        float textWidth = cardWidth - 2f * cardPadding;
        float textHeight = 0f;
        bool hasText = noteText != null && !string.IsNullOrEmpty(noteText.text);
        if (noteText != null)
        {
            noteText.gameObject.SetActive(hasText);
            noteText.enableAutoSizing = false;
            noteText.fontSize = textFontSize;
        }
        if (hasText)
        {
            float preferred = noteText.GetPreferredValues(noteText.text, textWidth / canvasScale, 0f).y * canvasScale;
            textHeight = Mathf.Min(preferred, maxTextHeight);
            if (preferred > maxTextHeight)
            {
                noteText.enableAutoSizing = true;
                noteText.fontSizeMax = textFontSize;
                noteText.fontSizeMin = minFontSize;
            }
        }
        float cardHeight = cardPadding + mediaHeight + (hasText ? textGap + textHeight : 0f) + cardPadding;

        // Stretch the shape and grab box to the card, and move the handles out with the edges.
        Vector3 baseSize = visual.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Vector2 oldHalf = new Vector2(baseSize.x * visual.localScale.x, baseSize.y * visual.localScale.y) * 0.5f;
        visual.localScale = new Vector3(cardWidth / baseSize.x, cardHeight / baseSize.y, 1f);
        if (noteCollider != null)
        {
            noteCollider.size = new Vector3(cardWidth, cardHeight, noteCollider.size.z);
            noteCollider.center = new Vector3(0f, 0f, noteCollider.center.z);
        }
        Vector2 newHalf = new Vector2(cardWidth, cardHeight) * 0.5f;
        foreach (VRHandleConnector handle in GetComponentsInChildren<VRHandleConnector>(true))
        {
            Vector3 p = handle.transform.localPosition;
            if (Mathf.Abs(p.x) > Mathf.Abs(p.y)) p.x = Mathf.Sign(p.x) * (newHalf.x + Mathf.Abs(p.x) - oldHalf.x);
            else p.y = Mathf.Sign(p.y) * (newHalf.y + Mathf.Abs(p.y) - oldHalf.y);
            handle.transform.localPosition = p;
        }

        // Canvas covers the card; media pinned to the top, text to the bottom.
        canvasRect.localPosition = new Vector3(0f, 0f, canvasRect.localPosition.z);
        canvasRect.sizeDelta = new Vector2(cardWidth, cardHeight) / canvasScale;

        RectTransform media = mediaThumbnail.rectTransform;
        media.anchorMin = media.anchorMax = new Vector2(0.5f, 1f);
        media.pivot = new Vector2(0.5f, 1f);
        media.anchoredPosition = new Vector2(0f, -cardPadding / canvasScale);
        media.sizeDelta = new Vector2(mediaWidth, mediaHeight) / canvasScale;

        if (hasText)
        {
            RectTransform textRect = noteText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, cardPadding / canvasScale);
            textRect.sizeDelta = new Vector2(-2f * cardPadding / canvasScale, textHeight / canvasScale);
        }
    }

    // Grows the whole note (shape, collider, handle, text area) just enough for the text to
    // fit at its normal size, so a paragraph makes a bigger note rather than tiny print.
    // The font is divided by the same scale so the letters stay the same size in the world.
    void FitToText()
    {
        noteText.enableAutoSizing = false;
        Vector2 area = noteText.rectTransform.rect.size; // the shape's text-safe area, at scale 1

        float scale = 1f;
        while (scale < maxScale && !Fits(area, scale)) scale += 0.05f;
        scale = Mathf.Min(scale, maxScale);

        transform.localScale = Vector3.one * scale;
        noteText.fontSize = textFontSize / scale;

        // Still too long at the biggest size: let the text shrink, as before.
        if (!Fits(area, scale))
        {
            noteText.enableAutoSizing = true;
            noteText.fontSizeMax = textFontSize / scale;
            noteText.fontSizeMin = minFontSize / scale;
        }
    }

    // Would the text fit the area if the note were `scale` times bigger? Growing the note
    // by `scale` with the font shrunk by `scale` is the same as fitting the text at full
    // size into an area `scale` times bigger.
    bool Fits(Vector2 area, float scale)
    {
        noteText.fontSize = textFontSize / scale;
        Vector2 preferred = noteText.GetPreferredValues(noteText.text, area.x, 0f);
        return preferred.y <= area.y && preferred.x <= area.x + 0.01f;
    }

    // When a note is deleted (e.g. dropped in the trash bin), take its connection lines
    // (and their labels, which are children of the line) with it, and drop it from Tour
    // Mode's list. Skipped while the scene itself is unloading.
    void OnDestroy()
    {
        TourManager.tourPath.Remove(transform);
        if (!gameObject.scene.isLoaded) return;

        foreach (ConnectionLine line in FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None))
        {
            bool attached = (line.pointA != null && line.pointA.IsChildOf(transform))
                || (line.pointB != null && line.pointB.IsChildOf(transform));
            if (attached) Destroy(line.gameObject);
        }
    }
}
