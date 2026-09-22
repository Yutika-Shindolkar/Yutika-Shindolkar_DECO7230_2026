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

    public void SetLabel(string text)
    {
        if (noteText != null) noteText.text = text;
    }
}
