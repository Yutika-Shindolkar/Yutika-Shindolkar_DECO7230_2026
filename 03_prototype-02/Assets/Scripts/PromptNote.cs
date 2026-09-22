using UnityEngine;

// Hovers in front of the player when they enter the scene, telling them to
// clap to open the note creation panel. Positions itself once at Start
// (facing the direction the player happened to be looking on entry), then
// stays put until ClapGestureDetector's onClap event calls Hide().
public class PromptNote : MonoBehaviour
{
    [Header("Placement")]
    public Transform headTransform;      // the VR camera
    public float distance = 0.8f;

    void Start()
    {
        if (headTransform == null) return;

        Vector3 pos = headTransform.position + headTransform.forward * distance;
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(transform.position - headTransform.position);
    }

    // Wire this to ClapGestureDetector's onClap event so the prompt disappears
    // once the panel takes over.
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
