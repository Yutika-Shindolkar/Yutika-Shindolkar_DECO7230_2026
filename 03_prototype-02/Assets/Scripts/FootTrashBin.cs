using UnityEngine;

// A single trash bin that appears at the player's feet only while a note is being held,
// and follows their standing position. Drop a held note into it to delete it.
public class FootTrashBin : MonoBehaviour
{
    public static FootTrashBin Instance;

    public Transform playerRoot;      // XR Origin: its height is the floor (Tracking Origin Mode: Floor)
    public Transform headTransform;   // the VR camera: where the player is actually standing
    public float footOffsetY = 0f;    // tweak if the bin should sit slightly above/below the floor

    Renderer[] renderers;
    Collider trashCollider;
    int activeHolds = 0;

    void Awake()
    {
        Instance = this;
        trashCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    void Update()
    {
        if (activeHolds <= 0 || playerRoot == null) return;
        transform.position = FeetPosition();
    }

    // Under the player's head at floor height. With room-scale the player walks around
    // inside the rig, so the rig's own origin is often not where they're standing.
    Vector3 FeetPosition()
    {
        Vector3 pos = headTransform != null ? headTransform.position : playerRoot.position;
        pos.y = playerRoot.position.y + footOffsetY;
        return pos;
    }

    public void RegisterHoldStart()
    {
        activeHolds++;
        if (playerRoot != null) transform.position = FeetPosition();
        SetVisible(true);
    }

    public void RegisterHoldEnd()
    {
        activeHolds = Mathf.Max(0, activeHolds - 1);
        if (activeHolds == 0) SetVisible(false);
    }

    public bool IsPositionOverBin(Vector3 worldPos, float radius)
    {
        return activeHolds > 0 && Vector3.Distance(worldPos, transform.position) <= radius;
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
        if (trashCollider != null) trashCollider.enabled = visible;
    }
}
