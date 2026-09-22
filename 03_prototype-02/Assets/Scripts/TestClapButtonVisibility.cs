using UnityEngine;

// Desktop-testing convenience: keeps the TEST clap button hidden while the
// NoteCreationPanel is open, and brings it back automatically once the panel
// closes, so you don't have to rerun the Editor tool to test the flow again.
// Lives on the always-active canvas so its Update keeps running even while
// the button itself is switched off.
public class TestClapButtonVisibility : MonoBehaviour
{
    public GameObject panel;
    public GameObject button;

    void Update()
    {
        if (panel == null || button == null) return;
        bool shouldShow = !panel.activeInHierarchy;
        if (button.activeSelf != shouldShow)
            button.SetActive(shouldShow);
    }
}
