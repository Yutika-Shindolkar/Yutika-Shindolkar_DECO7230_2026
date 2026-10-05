using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Wrist-mounted, non-linear Tour Mode menu.
// Rotate your wrist to look at it (watch-check gesture) to reveal a scrollable list of
// every connected item; poke a button to jump there. Locked spec from planning notes:
// trigger = rotate-wrist-to-look, content = scrollable button list, selection = poke.
// The list buttons are uGUI, so they need the EventSystem's XRUIInputModule and the
// canvas's TrackedDeviceGraphicRaycaster (both set up by IP2aSceneBuilder) to receive
// controller rays and pokes.
//
// Editor only: B opens/closes the menu in front of the camera (the wrist gesture is
// awkward in the simulator), and exits the tour while touring.
public class VRTourMode : MonoBehaviour
{
    public static bool IsTouring = false;

    [Header("References")]
    public Transform headTransform;         // VR camera
    public Transform wristTransform;        // left hand/controller, menu anchors here
    public Transform xrOriginRoot;          // XR Origin GameObject, moved to "teleport" the player
    public GameObject menuCanvas;           // wrist menu canvas root, child of wristTransform
    public RectTransform listContent;       // Scroll View's Content
    public GameObject listItemButtonPrefab; // prefab with a Button + TMP_Text child
    public GameObject exitTourButton;

    [Header("Look-at-wrist detection (tune these live in-headset)")]
    public float maxLookAngle = 35f;        // how directly you must be looking at the wrist
    public float maxWristFacingAngle = 50f; // how much the wrist face must turn toward your head

    [Header("Approach settings")]
    public float viewDistance = 1.2f;       // how far in front of a note's face a jump lands you

    Vector3 savedOriginPosition;
    Quaternion savedOriginRotation;
    bool menuOpen;
    readonly List<GameObject> spawnedButtons = new List<GameObject>();

#if UNITY_EDITOR
    bool editorMenuForced;
    Transform menuHomeParent;
    Vector3 menuHomeLocalPos;
    Quaternion menuHomeLocalRot;
#endif

    void Start()
    {
        IsTouring = false; // static, so reset it in case a previous Play session left it set
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (exitTourButton != null) exitTourButton.SetActive(false);
    }

    void Update()
    {
        if (headTransform == null || wristTransform == null || menuCanvas == null) return;

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (IsTouring) ExitTour();
            else SetEditorMenuForced(!editorMenuForced);
        }
        if (editorMenuForced) return;
#endif

        if (IsTouring) return;

        bool shouldShow = IsLookingAtWrist();
        if (shouldShow && !menuOpen) OpenMenu();
        else if (!shouldShow && menuOpen) CloseMenu();
    }

#if UNITY_EDITOR
    // Shows the wrist menu floating in front of the camera so it can be clicked with the
    // mouse or a simulated controller, then puts it back on the wrist.
    void SetEditorMenuForced(bool forced)
    {
        editorMenuForced = forced;
        Transform menu = menuCanvas.transform;
        if (forced)
        {
            menuHomeParent = menu.parent;
            menuHomeLocalPos = menu.localPosition;
            menuHomeLocalRot = menu.localRotation;
            menu.SetParent(null, true);
            menu.position = headTransform.position + headTransform.forward * 0.5f;
            menu.rotation = Quaternion.LookRotation(menu.position - headTransform.position);
            OpenMenu();
        }
        else
        {
            CloseMenu();
            menu.SetParent(menuHomeParent, true);
            menu.localPosition = menuHomeLocalPos;
            menu.localRotation = menuHomeLocalRot;
        }
    }
#endif

    bool IsLookingAtWrist()
    {
        Vector3 toWrist = (wristTransform.position - headTransform.position).normalized;
        float lookAngle = Vector3.Angle(headTransform.forward, toWrist);
        if (lookAngle > maxLookAngle) return false;

        float wristFacingAngle = Vector3.Angle(wristTransform.up, -toWrist);
        return wristFacingAngle <= maxWristFacingAngle;
    }

    public bool IsMenuOpen => menuOpen;

    public void OpenMenu()
    {
        menuOpen = true;
        menuCanvas.SetActive(true);
        RebuildList();
    }

    public void CloseMenu()
    {
        menuOpen = false;
        if (menuCanvas != null) menuCanvas.SetActive(false);
    }

    void RebuildList()
    {
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();

        if (listContent == null || listItemButtonPrefab == null) return;

        for (int i = 0; i < TourManager.tourPath.Count; i++)
        {
            Transform target = TourManager.tourPath[i];
            if (target == null) continue;

            GameObject item = Instantiate(listItemButtonPrefab, listContent);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                NoteData data = target.GetComponent<NoteData>();
                label.text = (data != null && data.noteText != null && !string.IsNullOrEmpty(data.noteText.text))
                    ? data.noteText.text
                    : "Item " + (i + 1);
            }

            Transform capturedTarget = target;
            Button btn = item.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => JumpTo(capturedTarget));

            spawnedButtons.Add(item);
        }
    }

    public void JumpTo(Transform target)
    {
        if (target == null || xrOriginRoot == null || headTransform == null) return;

        if (!IsTouring)
        {
            savedOriginPosition = xrOriginRoot.position;
            savedOriginRotation = xrOriginRoot.rotation;
            IsTouring = true;
            if (exitTourButton != null) exitTourButton.SetActive(true);
        }

#if UNITY_EDITOR
        if (editorMenuForced) SetEditorMenuForced(false);
#endif
        CloseMenu();

        // Land in front of the note's face. A note's readable side faces -Z (its forward
        // points away from whoever created it), so stand back along -forward.
        Vector3 noteFacing = -target.forward;
        noteFacing.y = 0f;
        if (noteFacing.sqrMagnitude < 0.0001f) noteFacing = -xrOriginRoot.forward;
        noteFacing.Normalize();
        Vector3 desiredHeadPos = target.position + noteFacing * viewDistance;

        float desiredYaw = Quaternion.LookRotation(-noteFacing).eulerAngles.y;
        float currentYaw = Quaternion.LookRotation(new Vector3(headTransform.forward.x, 0f, headTransform.forward.z)).eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(currentYaw, desiredYaw);

        // Rotate the rig around the user's actual current head position, not the origin's
        // own pivot, so the turn doesn't fling them sideways.
        xrOriginRoot.RotateAround(headTransform.position, Vector3.up, yawDelta);

        // Horizontal move only: the floor stays the floor, so the player's own height
        // (and the trash bin at their feet) is unchanged.
        Vector3 positionDelta = desiredHeadPos - headTransform.position;
        positionDelta.y = 0f;
        xrOriginRoot.position += positionDelta;
    }

    public void ExitTour()
    {
        if (!IsTouring || xrOriginRoot == null) return;

        xrOriginRoot.position = savedOriginPosition;
        xrOriginRoot.rotation = savedOriginRotation;
        IsTouring = false;
        if (exitTourButton != null) exitTourButton.SetActive(false);
    }
}
