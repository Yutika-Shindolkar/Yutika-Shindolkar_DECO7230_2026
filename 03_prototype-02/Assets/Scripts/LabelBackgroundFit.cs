using TMPro;
using UnityEngine;

// Keeps a connection label's white background sized to its text, so the label sits on its
// connector: the line runs into the background instead of through the letters.
// Sits on the LabelText object; the background is a child quad just behind the text.
public class LabelBackgroundFit : MonoBehaviour
{
    public TMP_Text text;
    public Transform background;
    public Vector2 padding = new Vector2(1.2f, 0.6f); // around the text, in label units

    string fittedText;

    void LateUpdate()
    {
        if (text == null || background == null || text.text == fittedText) return;
        fittedText = text.text;

        bool hasText = !string.IsNullOrEmpty(fittedText);
        background.gameObject.SetActive(hasText);
        if (!hasText) return;

        text.ForceMeshUpdate();
        Bounds b = text.textBounds;
        background.localPosition = new Vector3(b.center.x, b.center.y, 0.05f); // just behind the letters
        background.localScale = new Vector3(b.size.x + padding.x, b.size.y + padding.y, 1f);
    }
}
