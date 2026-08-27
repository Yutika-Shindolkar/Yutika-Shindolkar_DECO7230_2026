using UnityEngine;

public class LineLabelFollow : MonoBehaviour
{
    public LineRenderer line;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (line == null || line.positionCount == 0) return;

        int midIndex = line.positionCount / 2;
        Vector3 midPoint = line.GetPosition(midIndex);
        transform.position = midPoint;

        if (cam == null) return;

        Vector3 startWorld = line.GetPosition(0);
        Vector3 endWorld = line.GetPosition(line.positionCount - 1);

        Vector3 startScreen = cam.WorldToScreenPoint(startWorld);
        Vector3 endScreen = cam.WorldToScreenPoint(endWorld);

        float angle = Mathf.Atan2(endScreen.y - startScreen.y, endScreen.x - startScreen.x) * Mathf.Rad2Deg;

        if (angle > 90f) angle -= 180f;
        if (angle < -90f) angle += 180f;

        transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, angle);
    }
}