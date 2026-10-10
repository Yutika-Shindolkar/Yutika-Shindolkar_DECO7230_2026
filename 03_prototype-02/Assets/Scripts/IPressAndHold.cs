// Marks an interactable that needs grip held down (dragging a connector out of a handle,
// yanking a line), so XRButtonPressRouter never "clicks" it with a one-shot trigger or
// Editor mouse press, which would start an interaction that never ends.
public interface IPressAndHold { }
