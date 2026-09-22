using UnityEngine;

// World-space panel that appears after a clap. Lets the user pick a shape and an
// optional placeholder media attachment, then spawns the note. Also spawns the
// standalone grabbable mic prop via its own button.
public class NoteCreationPanel : MonoBehaviour
{
    [Header("Placement")]
    public Transform headTransform;      // the VR camera
    public float spawnDistance = 0.6f;

    [Header("Note prefabs, indexed to match the NoteShape enum order")]
    public GameObject[] shapePrefabs;    // Rectangle, Circle, Triangle, Hexagon, Star

    [Header("Placeholder media sprites")]
    public Sprite imagePlaceholder;
    public Sprite documentPlaceholder;
    public Sprite videoPlaceholder;

    [Header("Standalone grabbable mic prop")]
    public GameObject micPrefab;

    NoteShape pendingShape = NoteShape.Rectangle;
    MediaType pendingMedia = MediaType.None;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        if (headTransform == null) return;

        pendingShape = NoteShape.Rectangle;
        pendingMedia = MediaType.None;

        Vector3 pos = headTransform.position + headTransform.forward * spawnDistance;
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(transform.position - headTransform.position);

        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // Wire these to the shape buttons' OnClick, one integer per button (0=Rectangle...4=Star)
    public void SetPendingShape(int shapeIndex)
    {
        pendingShape = (NoteShape)shapeIndex;
    }

    // Wire these to the media buttons' OnClick (0=None,1=Image,2=Document,3=Video)
    public void SetPendingMedia(int mediaIndex)
    {
        pendingMedia = (MediaType)mediaIndex;
    }

    public void ConfirmCreate()
    {
        int index = (int)pendingShape;
        if (shapePrefabs == null || index < 0 || index >= shapePrefabs.Length || shapePrefabs[index] == null)
        {
            Debug.LogWarning("NoteCreationPanel: no prefab assigned for shape " + pendingShape);
            ClosePanel();
            return;
        }

        Vector3 spawnPos = headTransform.position + headTransform.forward * spawnDistance;
        GameObject note = Instantiate(shapePrefabs[index], spawnPos, Quaternion.identity);

        NoteData data = note.GetComponent<NoteData>();
        if (data != null)
        {
            Sprite placeholder = pendingMedia switch
            {
                MediaType.Image => imagePlaceholder,
                MediaType.Document => documentPlaceholder,
                MediaType.Video => videoPlaceholder,
                _ => null
            };
            data.SetMedia(pendingMedia, placeholder);
        }

        ClosePanel();
    }

    public void SpawnMic()
    {
        if (micPrefab == null || headTransform == null) return;
        Vector3 spawnPos = headTransform.position + headTransform.forward * spawnDistance;
        Instantiate(micPrefab, spawnPos, Quaternion.identity);
        ClosePanel();
    }
}
