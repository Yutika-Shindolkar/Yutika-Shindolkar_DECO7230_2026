using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// The XRI starter controllers aim with a narrow cone (about 8 cm wide at arm's length), so
// small things next to big things are ambiguous: aiming right at a note's 3 cm connection
// handle could grab the whole note, and aiming at one keyboard key could press its
// neighbour. This filter keeps XRI's normal candidates but moves whatever the exact
// centre line of the ray hits to the front, so you get what you're pointing at.
// Installed automatically on every Near-Far interactor when a scene loads.
public class ExactRayTargetFilter : IXRTargetFilter
{
    public bool canProcess => true;
    public void Link(IXRInteractor interactor) { }
    public void Unlink(IXRInteractor interactor) { }

    readonly RaycastHit[] hits = new RaycastHit[16];

    public void Process(IXRInteractor interactor, List<IXRInteractable> targets, List<IXRInteractable> results)
    {
        results.Clear();
        results.AddRange(targets);
        if (results.Count < 2 || !(interactor is IXRRayProvider ray)) return;

        Transform origin = ray.GetOrCreateRayOrigin();
        if (origin == null) return;

        int count = Physics.RaycastNonAlloc(origin.position, origin.forward, hits, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        IXRInteractable exact = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance >= nearest) continue;
            XRBaseInteractable hitInteractable = hits[i].collider.GetComponentInParent<XRBaseInteractable>();
            if (hitInteractable == null) continue;
            // A handle's collider is a child of its note; GetComponentInParent finds the
            // handle's own interactable first, which is the one we want.
            nearest = hits[i].distance;
            exact = hitInteractable;
        }

        int index = exact != null ? results.IndexOf(exact) : -1;
        if (index > 0)
        {
            results.RemoveAt(index);
            results.Insert(0, exact);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallOnNearFarInteractors()
    {
        foreach (NearFarInteractor nearFar in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (nearFar.targetFilter == null) nearFar.targetFilter = new ExactRayTargetFilter();
    }
}
