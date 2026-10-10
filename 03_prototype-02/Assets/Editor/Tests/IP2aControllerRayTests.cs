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

    // Miro-style connector with the real controller ray: grip a handle, point the ray at
    // another note (the loose end snaps to it and it glows), let go to connect; letting go
    // over empty space cancels.
    [UnityTest]
    public IEnumerator RayDragsConnectorFromHandleToAnotherNote()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        NoteCreationPanel panel = Object.FindFirstObjectByType<NoteCreationPanel>(FindObjectsInactive.Include);
        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        Transform right = rig.transform.Find("Camera Offset/Right Controller");
        right.gameObject.SetActive(true);
        foreach (Behaviour beh in right.GetComponents<Behaviour>())
            if (beh.GetType().Name.Contains("TrackedPoseDriver")) beh.enabled = false;
        NearFarInteractor nearFar = right.GetComponentInChildren<NearFarInteractor>(true);
        nearFar.selectInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;

        // No lambdas capturing locals here: Play mode's domain reload would null them.
        panel.CreateTestNote();
        NoteData a = Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None)[0];
        panel.CreateTestNote();
        NoteData b = null;
        foreach (NoteData n in Object.FindObjectsByType<NoteData>(FindObjectsSortMode.None))
            if (n != a) b = n;
        b.transform.position += panel.headTransform.right * 0.6f;
        VRHandleConnector handleA = a.GetComponentInChildren<VRHandleConnector>();
        VRHandleConnector handleB = b.GetComponentInChildren<VRHandleConnector>();

        // The rig smooths the ray's aim over time, so after each move wait until the ray has
        // actually settled (with a time limit) instead of a fixed number of frames.
        // Stand back 0.8 m and aim at A's handle, then hold grip.
        right.position = handleA.transform.position - a.transform.forward * 0.8f;
        right.rotation = Quaternion.LookRotation(handleA.transform.position - right.position);
        float t0 = Time.realtimeSinceStartup;
        while (!nearFar.interactablesHovered.Contains(handleA.GetComponent<XRSimpleInteractable>()) && Time.realtimeSinceStartup - t0 < 3f)
            yield return null;
        Assert.IsTrue(nearFar.interactablesHovered.Contains(handleA.GetComponent<XRSimpleInteractable>()), "Ray should hover A's handle.");
        nearFar.selectInput.QueueManualState(true, 1f, true, false);
        for (int i = 0; i < 3; i++) yield return null;

        ConnectionLine line = Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).SingleOrDefault();
        Assert.IsNotNull(line, "Gripping a handle with the ray should start a connector.");
        Assert.Less(Vector3.Distance(line.followWhilePulling.position, handleA.transform.position), 0.05f,
            "Loose end should start at the ray tip (on the handle), not at the controller.");

        // Swing the ray onto the middle of note B: the loose end should snap to B's handle.
        right.rotation = Quaternion.LookRotation(b.transform.position - right.position);
        t0 = Time.realtimeSinceStartup;
        while (Vector3.Distance(line.followWhilePulling.position, handleB.transform.position) > 0.001f && Time.realtimeSinceStartup - t0 < 3f)
            yield return null;
        Assert.Less(Vector3.Distance(line.followWhilePulling.position, handleB.transform.position), 0.001f,
            "Pointing at note B should snap the loose end to B's handle.");
        Assert.IsTrue(handleB.glowOutline.activeSelf, "B's handle should glow while it's the snap target.");

        // Let go.
        nearFar.selectInput.QueueManualState(false, 0f, false, true);
        for (int i = 0; i < 3; i++) yield return null;
        Assert.AreEqual(handleB.transform, line.pointB, "Releasing on B should connect to B.");
        Assert.IsFalse(handleB.glowOutline.activeSelf, "B's glow should switch off after connecting.");

        // Second drag from B, released pointing at empty space: cancelled.
        right.position = handleB.transform.position - b.transform.forward * 0.8f;
        right.rotation = Quaternion.LookRotation(handleB.transform.position - right.position);
        t0 = Time.realtimeSinceStartup;
        while (!nearFar.interactablesHovered.Contains(handleB.GetComponent<XRSimpleInteractable>()) && Time.realtimeSinceStartup - t0 < 3f)
            yield return null;
        Assert.IsTrue(nearFar.interactablesHovered.Contains(handleB.GetComponent<XRSimpleInteractable>()),
            "Ray should hover B's handle (not B's note body) when aimed straight at it.");
        nearFar.selectInput.QueueManualState(true, 1f, true, false);
        for (int i = 0; i < 3; i++) yield return null;
        Assert.AreEqual(2, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length, "Second connector should start.");

        right.rotation = Quaternion.LookRotation(Vector3.up);
        t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 1f) yield return null; // let the ray swing fully away
        nearFar.selectInput.QueueManualState(false, 0f, false, true);
        for (int i = 0; i < 3; i++) yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<ConnectionLine>(FindObjectsSortMode.None).Length,
            "Releasing over empty space should remove the new connector.");

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
