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

        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}