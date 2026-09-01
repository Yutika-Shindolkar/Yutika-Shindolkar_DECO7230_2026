# Prototype-01 - Testing Plan

_"This project reimagines a collaborative digital whiteboard, in the style of Miro or FigJam, as a fully immersive VR creation space. Instead of scrolling a flat 2D canvas, users capture ideas as notes, physically pull threads between notes to build structure, and walk through their finished board rather than flipping through slides. The goal is to make capturing and connecting ideas feel embodied and spatial rather than menu-driven."_

### Testing Objective

This concept raises questions about whether a first-time user can independently discover and complete the full idea-capture-to-connection workflow: creating a marker or note, editing it, connecting two of them via a handle, and labeling that connection, without being told how any of it works.

The assumptions being tested are that this workflow is discoverable from the prototype's own visual cues alone (hover-glow on the palette icons, handles that appear on an item's edges, and the small plus button on a connecting thread), and that once shown, the depth control for repositioning an item toward or away from the camera while dragging feels natural and easy to use. Depth control and Tour Mode both have no on-screen visual cue, so those two parts of the test are prompted rather than unaided.

This test aims to discover where participants hesitate, guess wrong, or get stuck across the unaided portion of the workflow, so it becomes clear whether the core interaction loop of the concept, capture, connect, structure, is intuitive enough to build further iterations on.

### Testing Methodologies

This testing plan uses moderated, task-based usability testing with a think-aloud protocol to evaluate the digital prototype built in Unity. Tasks are grouped into broad chunks rather than individual clicks, so participants work out the specific steps themselves rather than being walked through each one, which keeps the unaided portion of the test genuinely unaided. Depth control and Tour Mode are the exception, both are demonstrated directly, since neither has a visual cue to discover. A structured five-question debrief follows each session to capture qualitative reactions.

### Prototype Description/Requirements

The prototype was built to let a participant capture an idea as a marker or a note, move it in 3D space, connect it to a second item, label that connection, and revisit connected items in sequence.

It includes a first-person free-fly camera controlled with mouse and keyboard, a palette with buttons to spawn a sticky note or a circular marker and a trash button to delete them, drag-and-drop repositioning of items in 3D space with a depth control that lets a held item be pushed further away or pulled closer to the camera, click-to-edit text entry on any item, four connection handles per item that can be pulled to draw a curved thread to another item with a small button on the thread that reveals an editable label, and a Tour Mode that lets the user step through every connected item in the order they were linked.

### Data Collection Method

Data is collected on the User Testing Observation Form, one per participant, included at the end of this document under User Testing Observation Form. For each task, three things are recorded: completion status, marked as Unaided, Prompted, or Failed; a hesitation/error tally, using a simple tick mark each time the participant pauses for more than a few seconds, clicks the wrong object, or backtracks; and a task efficiency rating from 1 to 10, marked by circling a number, as a quick overall impression of how smoothly that task went. A free-text line under each task captures anything said aloud worth quoting. The whole session is also recorded, so exact timing can be pulled from the recording afterward if it's ever needed, rather than tracked live during the test.

The depth-control task also gets a direct comfort rating (Comfortable, Neutral, or Confusing), asked right after the participant tries it, since that is a stated reaction rather than an observed behaviour. A five-question written debrief is filled in at the end of each form.

### Testing Setup

#### Setup

Before each session, the Unity build is running in Play mode with an empty scene, with the observation form ready and the session recording started. Consent is confirmed first, then the same task script is read to every participant so the test stays consistent. Nothing about how the interactions work is explained during the unaided task chunks, only the depth control and Tour Mode are demonstrated, at the point each is needed.

#### Testing Process

1. Introduce the prototype to the participant, including that there are two item types, a marker and a note.
2. Confirm consent to take part and to be recorded for educational purposes.
3. Ask them to create a marker and give it some text.
4. Demonstrate the depth control and have them try it, then rate it.
5. Ask them to create a note, connect it to the marker, and label the connection.
6. Demonstrate Tour Mode and have them step through their connected items.
7. Ask them to create one more note, then delete it.
8. Debrief with the five questions below.

#### Task Script (Read Aloud to Each Participant)

**Step 1:** "This is a prototype for an idea-mapping tool I'm building. There are two types of items you can create, a marker and a note. I'm going to ask you to try a few small tasks with them using your mouse and keyboard. There's no tutorial, so just explore as you go, and say out loud whatever you're thinking."

**Step 2:** "Before we start, do you consent to taking part in this testing session? And do you consent to this session being recorded, through notes and/or screen capture, for educational purposes only?"

**Step 3:** "Create a marker, and give it some text."

**Step 4:** "While you're holding a marker or note, you can push it further away or pull it closer to you using the Page Up and Page Down keys. Try that now, then tell me how comfortable that felt."

**Step 5:** "Create a note, connect it to the marker, and give that connection a label."

**Step 6:** "There's also a Tour Mode. Press T to enter it, use Space and Backspace to move between your connected items, and press Esc to exit whenever you're ready."

**Step 7:** "Create one more note, then delete it."

**Step 8 (debrief):** "A few questions to finish up." Ask the five questions listed under [Post-Testing Questionnaire](#post-testing-questionnaire) below.

#### User Testing Observation Form

Each participant's session is recorded live on the User Testing Observation Form, one copy filled in per participant. See [02_prototype-01_user-testing-observation-form.pdf](./02_prototype-01_user-testing-observation-form.pdf) for the printable version used during each user testing.

Participant number: ______     Date: ______

Consent:  Y / N

**Task 1 — Create a marker and give it text**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-click tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 2 — Depth control (Page Up / Page Down while holding an item)**

Completion:  Unaided / Prompted / Failed
Comfort rating:  Comfortable / Neutral / Confusing
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 3 — Create a note, connect it to the marker, label the connection**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-click tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 4 — Tour Mode**

Completion:  Unaided / Prompted / Failed
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 5 — Create a note and delete it**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-click tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

#### Post-Testing Questionnaire

1. Overall, how intuitive did creating and editing a marker or note feel? (circle one):   1   2   3   4   5   6   7   8   9   10

2. What was the most confusing or frustrating moment during the tasks?
   ________________________________________________

3. Did connecting two items and labeling that connection feel like a natural next step, or did you have to think about where to click?
   ________________________________________________

4. How did Tour Mode compare to the idea of walking through your connected ideas instead of scrolling? Did it make sense as a way to revisit your work?
   ________________________________________________

5. Would you want to use a tool like this for organizing your own ideas? Why or why not?
   ________________________________________________
