using UnityEngine;
using UnityEngine.InputSystem;

public class TourMode : MonoBehaviour
{
    public static bool IsTouring = false;

    public Vector3 cameraLocalOffset = new Vector3(0f, 0f, -2f);
    public Vector3 approachDirection = new Vector3(0f, 0f, -1f);
    public float viewDistance = 1.2f;

    public GameObject hudText;
    public GameObject[] paletteButtons;

    private int currentIndex = 0;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private PlayerMove playerMove;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();
        if (hudText != null) hudText.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (!IsTouring) TryEnterTour();
            else ExitTour();
        }

        if (!IsTouring) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentIndex = Mathf.Min(currentIndex + 1, TourManager.tourPath.Count - 1);
            SnapToCurrent();
        }
        else if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            currentIndex = Mathf.Max(currentIndex - 1, 0);
            SnapToCurrent();
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitTour();
        }
    }

    void TryEnterTour()
    {
        if (TourManager.tourPath.Count == 0) return;

        savedPosition = transform.position;
        savedRotation = transform.rotation;

        if (playerMove != null) playerMove.enabled = false;

        SetPaletteActive(false);

        IsTouring = true;
        currentIndex = 0;
        if (hudText != null) hudText.SetActive(true);
        SnapToCurrent();
    }

    void ExitTour()
    {
        IsTouring = false;
        transform.position = savedPosition;
        transform.rotation = savedRotation;
        if (playerMove != null) playerMove.enabled = true;
        if (hudText != null) hudText.SetActive(false);

        SetPaletteActive(true);
    }

    void SetPaletteActive(bool active)
    {
        if (paletteButtons == null) return;
        foreach (GameObject obj in paletteButtons)
        {
            if (obj != null) obj.SetActive(active);
        }
    }

    void SnapToCurrent()
    {
        Transform target = TourManager.tourPath[currentIndex];
        if (target == null) return;

        Vector3 desiredCameraWorldPos = target.position + approachDirection.normalized * viewDistance;
        Vector3 lookDir = (target.position - desiredCameraWorldPos).normalized;
        Quaternion desiredRot = Quaternion.LookRotation(lookDir);

        transform.rotation = desiredRot;
        transform.position = desiredCameraWorldPos - (desiredRot * cameraLocalOffset);
    }
}