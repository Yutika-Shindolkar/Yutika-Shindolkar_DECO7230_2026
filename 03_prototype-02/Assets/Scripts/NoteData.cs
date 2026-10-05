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
