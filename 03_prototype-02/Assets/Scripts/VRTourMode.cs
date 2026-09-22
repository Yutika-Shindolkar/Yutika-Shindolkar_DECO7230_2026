using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// Wrist-mounted, non-linear Tour Mode menu.
// Rotate your wrist to look at it (watch-check gesture) to reveal a scrollable list of
// every connected item; poke a button to jump there. Locked spec from planning notes:
// trigger = rotate-wrist-to-look, content = scrollable button list, selection = poke.
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
    public Vector3 approachDirection = new Vector3(0f, 0f, -1f);
    public float viewDistance = 1.2f;

    Vector3 savedOriginPosition;
    Quaternion savedOriginRotation;
    bool menuOpen;
    readonly List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (exitTourButton != null) exitTourButton.SetActive(false);
    }

    void Update()
    {
        if (headTransform == null || wristTransform == null || menuCanvas == null) return;
        if (IsTouring) return;

        bool shouldShow = IsLookingAtWrist();
        if (shouldShow && !menuOpen) OpenMenu();
        else if (!shouldShow && menuOpen) CloseMenu();
    }

    bool IsLookingAtWrist()
    {
        Vector3 toWrist = (wristTransform.position - headTransform.position).normalized;
        float lookAngle = Vector3.Angle(headTransform.forward, toWrist);
        if (lookAngle > maxLookAngle) return false;

        float wristFacingAngle = Vector3.Angle(wristTransform.up, -toWrist);
        return wristFacingAngle <= maxWristFacingAngle;
    }

    void OpenMenu()
    {
        menuOpen = true;
        menuCanvas.SetActive(true);
        RebuildList();
    }

    void CloseMenu()
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

        CloseMenu();

        Vector3 desiredHeadPos = target.position + approachDirection.normalized * viewDistance;

        Vector3 flatLookDir = target.position - desiredHeadPos;
        flatLookDir.y = 0f;
        if (flatLookDir.sqrMagnitude < 0.0001f) flatLookDir = xrOriginRoot.forward;
        float desiredYaw = Quaternion.LookRotation(flatLookDir).eulerAngles.y;
        float currentYaw = Quaternion.LookRotation(new Vector3(headTransform.forward.x, 0f, headTransform.forward.z)).eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(currentYaw, desiredYaw);

        // Rotate the rig around the user's actual current head position, not the origin's
        // own pivot, so the turn doesn't fling them sideways.
        xrOriginRoot.RotateAround(headTransform.position, Vector3.up, yawDelta);

        Vector3 positionDelta = desiredHeadPos - headTransform.position;
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
