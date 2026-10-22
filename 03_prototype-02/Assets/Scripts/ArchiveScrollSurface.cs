using UnityEngine;

// Marks the archive window's card as a surface that is dragged with grip held, so
// XRButtonPressRouter never "clicks" it with a trigger press (see IPressAndHold), and so
// the drawer-pull gesture still works while pointing at the window.
public class ArchiveScrollSurface : MonoBehaviour, IPressAndHold { }
