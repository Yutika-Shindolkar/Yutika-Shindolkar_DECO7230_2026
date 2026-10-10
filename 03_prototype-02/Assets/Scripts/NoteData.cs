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

    public void SetMedia(MediaType type, Sprite placeholderSprite)
    {
        attachedMedia = type;
        if (mediaThumbnail == null) return;

        mediaThumbnail.gameObject.SetActive(type != MediaType.None);
        if (placeholderSprite != null) mediaThumbnail.sprite = placeholderSprite;
    }

    [Header("Grow to fit text")]
    public float textFontSize = 20f; // note text size (canvas units at scale 1); kept the same as the note grows
    public float maxScale = 2.2f;    // the shape stops growing here; past it, text shrinks to fit
    public float minFontSize = 6f;

    public void SetLabel(string text)
    {
        if (noteText == null) return;
        noteText.text = text;
        FitToText();
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
