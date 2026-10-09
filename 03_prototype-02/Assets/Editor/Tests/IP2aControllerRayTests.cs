using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Points the rig's real right-controller Near-Far interactor at panel buttons and checks
// it actually hovers them - the step that failed on the headset (no hover glow). The
// other tests fire button events directly, so they can't catch a ray that never hovers.
public class IP2aControllerRayTests
{
    const string ScenePath = "Assets/Scenes/IP2a.unity";

    [UnityTest]
    public IEnumerator RightControllerRayHoversPanelButtons()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        Transform controller = rig.transform.Find("Camera Offset/Right Controller");
        Assert.IsNotNull(controller, "No Right Controller under the rig.");
        controller.gameObject.SetActive(true);

        // Stop tracking (and the simulator) from moving the controller so the test can aim it.
        foreach (Behaviour b in controller.GetComponents<Behaviour>())
            if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;

        NearFarInteractor nearFar = controller.GetComponentInChildren<NearFarInteractor>(true);
        Assert.IsNotNull(nearFar, "No NearFarInteractor on the right controller.");

        panel.OpenPanel();
        yield return null;

        string[] buttons = { "CircleButton", "CreateButton", "DiscardButton", "TestNoteButton", "NoteTextField" };
        var report = new StringBuilder();
        bool allHovered = true;
        foreach (string name in buttons)
        {
            XRSimpleInteractable target = panel.transform.Find(name).GetComponent<XRSimpleInteractable>();
            Vector3 aim = target.GetComponent<Collider>().bounds.center;
            controller.position = aim - panel.transform.forward * 0.6f;  // 60 cm in front of the panel face
            controller.rotation = Quaternion.LookRotation(aim - controller.position);

            for (int i = 0; i < 5; i++) { yield return new WaitForFixedUpdate(); yield return null; }

            bool hovered = nearFar.interactablesHovered.Contains(target);
            allHovered &= hovered;
            string hoverList = string.Join(", ", nearFar.interactablesHovered.Select(h => ((Component)h).name));
            Physics.Raycast(controller.position, controller.forward, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide);
            report.AppendLine($"{name}: hovered={hovered} | interactor hovers [{hoverList}] | physics ray hits '{(hit.collider ? hit.collider.name : "nothing")}' " +
                $"| nearFar active={nearFar.isActiveAndEnabled} layers={nearFar.interactionLayers.value} target layers={target.interactionLayers.value} " +
                $"| target registered={target.isActiveAndEnabled && target.interactionManager != null}");
        }

        Debug.Log("RAY REPORT\n" + report);
        Assert.IsTrue(allHovered, "Right controller ray did not hover every panel button:\n" + report);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RightRayStillHoversWithTourMenuOpenAndCanHoverHandles()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        Transform right = rig.transform.Find("Camera Offset/Right Controller");
        right.gameObject.SetActive(true);
        Transform left = rig.transform.Find("Camera Offset/Left Controller");
        left.gameObject.SetActive(true); // the Tour menu lives on the left controller
        foreach (Behaviour b in left.GetComponents<Behaviour>())
            if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;
        foreach (Behaviour b in right.GetComponents<Behaviour>())
            if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;
        NearFarInteractor nearFar = right.GetComponentInChildren<NearFarInteractor>(true);

        // The wrist menu opening by accident on the headset: does it steal the right ray?
        VRTourMode tour = Object.FindFirstObjectByType<VRTourMode>();
        tour.OpenMenu();
        tour.enabled = false; // keep it open: nobody is really looking at the wrist in a test
        panel.OpenPanel();
        // Left hand held out to the side at chest height, roughly where it is while pointing with the right.
        left.position = panel.headTransform.position + panel.headTransform.right * -0.3f + Vector3.down * 0.4f + panel.headTransform.forward * 0.3f;
        yield return null;
        Assert.IsTrue(tour.menuCanvas.activeInHierarchy, "Test setup: the Tour menu should be open.");

        XRSimpleInteractable create = panel.transform.Find("CreateButton").GetComponent<XRSimpleInteractable>();
        Vector3 aim = create.GetComponent<Collider>().bounds.center;
        right.position = aim - panel.transform.forward * 0.6f;
        right.rotation = Quaternion.LookRotation(aim - right.position);
        for (int i = 0; i < 5; i++) { yield return new WaitForFixedUpdate(); yield return null; }
        string uiHit = nearFar.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult ui) && ui.gameObject != null
            ? $"{ui.gameObject.name} at {ui.distance:0.00}m (canvas under '{ui.gameObject.GetComponentInParent<Canvas>().transform.parent?.name}')"
            : "none";
        Debug.Log($"TOUR CHECK: tour menu active={tour.menuCanvas.activeInHierarchy} at {tour.menuCanvas.transform.position}, right ray UI hit: {uiHit}, controller at {right.position}");
        Assert.IsTrue(nearFar.interactablesHovered.Contains(create),
            "With the Tour menu open, the right ray stopped hovering Create. UI hit: " + uiHit + ". Hovering: " +
            string.Join(", ", nearFar.interactablesHovered.Select(h => ((Component)h).name)));

        // A note's connection handle must be hoverable by the controller ray.
        tour.CloseMenu();
        tour.enabled = true;
        panel.CreateTestNote();
        NoteData note = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None).First();
        XRSimpleInteractable handle = note.GetComponentInChildren<VRHandleConnector>().GetComponent<XRSimpleInteractable>();
        panel.ClosePanel();
        Vector3 handlePos = handle.transform.position;
        right.position = handlePos - note.transform.forward * 0.5f;
        right.rotation = Quaternion.LookRotation(handlePos - right.position);
        for (int i = 0; i < 5; i++) { yield return new WaitForFixedUpdate(); yield return null; }
        Assert.IsTrue(nearFar.interactablesHovered.Contains(handle),
            "Right ray doesn't hover a note handle. Hovering: " +
            string.Join(", ", nearFar.interactablesHovered.Select(h => ((Component)h).name)));

        yield return new ExitPlayMode();
    }
}
