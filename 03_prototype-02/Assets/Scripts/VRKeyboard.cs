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

    // Callback mode (see the second Open overload): used by NoteCreationPanel, which
    // needs the typed string itself rather than having it written into a TMP_Text.
    System.Action<string> onConfirm;
    object owner;

    // The root GameObject stays active in the scene so Awake always runs and Instance is
    // set; it hides itself here at Play start. (An earlier version saved it inactive, so
    // Awake never ran and Instance stayed null.)
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
        onConfirm = null;
        owner = null;
        Show(startingText);
    }

    // Callback mode: Confirm passes the typed text (even an empty string, so a note's
    // text can be cleared) to onConfirm instead of writing into a TMP_Text. owner lets
    // the caller close the keyboard later only if it's still the one using it.
    public void Open(string startingText, System.Action<string> onConfirm, object owner)
    {
        targetText = null;
        cancelReactivateTarget = null;
        this.onConfirm = onConfirm;
        this.owner = owner;
        Show(startingText);
    }

    // Closes the keyboard without confirming, but only if `owner` opened it - e.g. the
    // note panel closing shouldn't shut a keyboard that a connection label is using.
    public void CancelIfOpenedBy(object owner)
    {
        if (gameObject.activeSelf && this.owner != null && this.owner == owner) Cancel();
    }

    void Show(string startingText)
    {
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
        if (onConfirm != null)
        {
            System.Action<string> callback = onConfirm;
            onConfirm = null;
            owner = null;
            gameObject.SetActive(false);
            callback(buffer);
            return;
        }

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
        onConfirm = null;
        owner = null;
        gameObject.SetActive(false);
    }

    void RefreshPreview()
    {
        if (previewText != null) previewText.text = buffer.Length > 0 ? buffer : "...";
    }
}
