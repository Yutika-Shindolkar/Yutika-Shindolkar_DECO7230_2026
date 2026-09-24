using UnityEngine;
using TMPro;

// Single reusable world-space poke keyboard - IP2a's one piece of real free-text entry,
// built once by IP2aSceneBuilder.BuildKeyboard() and shared by every connection label
// rather than spawned per-connection. Callers (VRPlusButtonClick, VRLabelEdit) just call
// Open() with the TMP_Text they want typed into; VRKeyboardKey pokes call back into
// TypeChar/Backspace/Confirm.
public class VRKeyboard : MonoBehaviour
{
    public static VRKeyboard Instance { get; private set; }

    public Transform headTransform;
    public float spawnDistance = 0.6f;
    public TMP_Text previewText; // live preview of the in-progress text, shown on the keyboard itself

    TMP_Text targetText;
    GameObject cancelReactivateTarget;
    string buffer = "";

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    // target: where the confirmed text ends up (its GameObject is SetActive(true) on
    // Confirm). startingText: pre-fills the buffer, e.g. re-opening an already-labelled
    // connection to edit it (see VRLabelEdit.cs) - pass "" for a brand new label.
    // cancelReactivateTarget: optional GameObject to SetActive(true) again if the user
    // backs out via Cancel instead of Confirm - the "+" sphere for a fresh connection
    // (so it isn't stranded retired with no label ever set), null when re-editing an
    // already-showing label (nothing needs reactivating either way).
    public void Open(TMP_Text target, string startingText, GameObject cancelReactivateTarget = null)
    {
        targetText = target;
        this.cancelReactivateTarget = cancelReactivateTarget;
        buffer = startingText ?? "";
        RefreshPreview();

        if (headTransform != null)
        {
            transform.position = headTransform.position + headTransform.forward * spawnDistance + Vector3.down * 0.15f;
            transform.rotation = Quaternion.LookRotation(transform.position - headTransform.position);
        }
        gameObject.SetActive(true);
    }

    public void TypeChar(string c)
    {
        buffer += c;
        RefreshPreview();
    }

    public void Backspace()
    {
        if (buffer.Length > 0) buffer = buffer.Substring(0, buffer.Length - 1);
        RefreshPreview();
    }

    // Wired to the "Confirm" key. Only commits if something was actually typed - poking
    // Confirm with an empty buffer just closes the keyboard without changing the label
    // (matters most for re-editing an existing label: backspacing everything then hitting
    // Confirm by mistake shouldn't blank it out).
    public void Confirm()
    {
        if (targetText != null && buffer.Length > 0)
        {
            targetText.text = buffer;
            targetText.gameObject.SetActive(true);
        }
        gameObject.SetActive(false);
    }

    // Wired to the "Cancel" key - backs out without changing the label. Reactivates
    // whatever was passed as cancelReactivateTarget in Open() (the "+" sphere, for a
    // fresh connection), if anything was given.
    public void Cancel()
    {
        if (cancelReactivateTarget != null) cancelReactivateTarget.SetActive(true);
        gameObject.SetActive(false);
    }

    void RefreshPreview()
    {
        if (previewText != null) previewText.text = buffer.Length > 0 ? buffer : "...";
    }
}
