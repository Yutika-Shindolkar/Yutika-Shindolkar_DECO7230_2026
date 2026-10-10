using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Makes every 3D button in the scene (anything with an XRSimpleInteractable: panel
// buttons, keyboard keys, the connection "+" sphere, label edit, Exit Tour) press the way
// Quest users expect: point the controller ray at it and pull the TRIGGER.
//
// Why this exists: the XRI starter rig maps "select" to GRIP (so grip grabs notes), and
// every button listens to selectEntered, so on its own a button only fired on point+grip.
// This router watches each controller's trigger (its activate input) and, when it's
// pulled while hovering a button, fires that button's selectEntered - so all the existing
// button wiring keeps working unchanged. Poke and grip still work as before.
//
// Editor only: a left mouse click on a 3D button in the Game view presses it too, so the
// panel flow can be tested on a desktop without the simulator. That code is compiled out
// of device builds.
//
// Created automatically when a scene loads, so it doesn't need to be placed in a scene.
public class XRButtonPressRouter : MonoBehaviour
{
    // Ignores a second press of the same button within this window, so pulling trigger
    // while also gripping (or a poke landing on the same frame) can't double-fire it.
    const float RepressDelay = 0.25f;

    readonly Dictionary<XRSimpleInteractable, float> lastPressTime = new Dictionary<XRSimpleInteractable, float>();
    readonly List<IXRInteractor> interactors = new List<IXRInteractor>();
    XRInteractionManager manager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInstance()
    {
        if (FindFirstObjectByType<XRButtonPressRouter>() != null) return;
        GameObject go = new GameObject(nameof(XRButtonPressRouter));
        DontDestroyOnLoad(go);
        go.AddComponent<XRButtonPressRouter>();
    }

    void Update()
    {
        if (manager == null) manager = FindFirstObjectByType<XRInteractionManager>();
        if (manager != null) HandleControllerTriggers();

#if UNITY_EDITOR
        HandleEditorMouseClick();
#endif
    }

    void HandleControllerTriggers()
    {
        manager.GetRegisteredInteractors(interactors);
        foreach (IXRInteractor interactor in interactors)
        {
            if (!(interactor is XRBaseInputInteractor inputInteractor) || !inputInteractor.isActiveAndEnabled) continue;
            if (inputInteractor.activateInput == null || !inputInteractor.activateInput.ReadWasPerformedThisFrame()) continue;

            // Press the first button this controller is hovering (the ray target, or the
            // nearest one when the controller is right up against the panel).
            foreach (IXRHoverInteractable hovered in inputInteractor.interactablesHovered)
            {
                if (hovered is XRSimpleInteractable button && IsPressable(button))
                {
                    Press(button, inputInteractor);
                    break;
                }
            }
        }
    }

    // Press-and-hold interactables (note handles, line grab areas - see IPressAndHold) need
    // grip held down; a one-shot click would start an interaction that never ends.
    public static bool IsPressable(XRSimpleInteractable button) =>
        button != null && button.isActiveAndEnabled && button.GetComponent<IPressAndHold>() == null;

    void Press(XRSimpleInteractable button, IXRSelectInteractor interactor)
    {
        float now = Time.unscaledTime;
        if (lastPressTime.TryGetValue(button, out float last) && now - last < RepressDelay) return;
        lastPressTime[button] = now;

        button.selectEntered.Invoke(new SelectEnterEventArgs
        {
            interactorObject = interactor,
            interactableObject = button,
            manager = manager,
        });
    }

#if UNITY_EDITOR
    void HandleEditorMouseClick()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // Leave clicks on screen-space UI (the "TEST: Click to Clap" button) to uGUI.
        if (IsOverScreenSpaceUI(mouse.position.ReadValue())) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // The first interactable along the ray wins; a note or mic in front of a button
        // blocks it, the same as a controller ray would.
        foreach (RaycastHit hit in hits)
        {
            XRBaseInteractable interactable = hit.collider.GetComponentInParent<XRBaseInteractable>();
            if (interactable == null) continue;
            if (interactable is XRSimpleInteractable button && IsPressable(button))
                Press(button, null);
            return;
        }
    }

    // World-space canvases (note text, Tour menu) shouldn't block clicks on 3D buttons,
    // so only screen-space overlay UI counts here.
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    bool IsOverScreenSpaceUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPos }, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            Canvas canvas = hit.gameObject.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace) return true;
        }
        return false;
    }
#endif
}
