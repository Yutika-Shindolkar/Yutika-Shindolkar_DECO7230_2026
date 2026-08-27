using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class NoteTextEditor : MonoBehaviour
{
    public static NoteTextEditor Instance;

    public GameObject inputPanel;
    public TMP_InputField inputField;

    private TMP_Text targetText;

    void Awake()
    {
        Instance = this;
        if (inputPanel != null) inputPanel.SetActive(false);
    }

    public void OpenFor(TMP_Text noteTextComponent, Vector3 worldPosition)
    {
        if (noteTextComponent == null || inputPanel == null || inputField == null) return;

        targetText = noteTextComponent;

        Camera cam = Camera.main;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        inputPanel.transform.position = screenPos;

        inputField.text = targetText.text;
        inputPanel.SetActive(true);
        inputField.Select();
        inputField.ActivateInputField();
    }

    void Update()
    {
        if (inputPanel == null || !inputPanel.activeSelf) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            Confirm();
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cancel();
        }
    }

    void Confirm()
    {
        if (targetText != null)
        {
            targetText.text = inputField.text;
        }
        Close();
    }

    void Cancel()
    {
        Close();
    }

    void Close()
    {
        inputPanel.SetActive(false);
        targetText = null;
    }
}