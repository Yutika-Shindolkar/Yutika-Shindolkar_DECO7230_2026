using UnityEngine;

// Put on testing aids (the "Create Test Note" button, the screen-space test clap button)
// so they show in the Editor and in Development Builds, but switch themselves off in a
// normal build that participants use. Tick File > Build Profiles > Development Build to
// keep them on the headset while testing.
public class DevOnly : MonoBehaviour
{
    void Awake()
    {
        if (!Debug.isDebugBuild) gameObject.SetActive(false);
    }
}
