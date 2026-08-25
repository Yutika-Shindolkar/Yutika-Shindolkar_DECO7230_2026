using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public Transform cam;
    public float spawnDistance = 1.5f;

    void OnMouseDown()
    {
        Vector3 spawnPos = cam.position + cam.forward * spawnDistance;
        GameObject newObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        DragNote drag = newObj.GetComponent<DragNote>();
        if (drag != null)
        {
            drag.ForceStartDrag();
        }
    }
}