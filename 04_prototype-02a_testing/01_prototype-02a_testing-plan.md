# Prototype-02a - Testing Plan

_"This project reimagines a collaborative digital whiteboard, in the style of Miro or FigJam, as a fully immersive VR creation space. Instead of scrolling a flat 2D canvas, users capture ideas as notes, physically pull threads between notes to build structure, and revisit their finished board by moving through it rather than flipping through slides. Prototype-02a rebuilds this same concept for real VR hardware, replacing Prototype-01's mouse-and-keyboard proxy with actual hand/controller input, and adds the capture and structuring pieces that couldn't be tested on desktop: a clap gesture to spawn a note, a grabbable dictation mic, media attachments, and a wrist-mounted menu for revisiting connected notes."_

### Testing Objective

Prototype-01 tested this workflow on desktop as a proxy for VR input. Prototype-02a moves the same core loop, create a note, connect it to another, label that connection, and revisit connected notes, onto real hand/controller input, and tests the pieces of the original Design Concept Report that desktop couldn't: the clap gesture, the grabbable mic, and attaching media.

The assumptions being tested are: that the clap gesture is discoverable from the fading onboarding prompt alone; that a note's connection handle reads as reusable (one note can connect to more than one other note) rather than single-use; that the wrist-mounted Tour Mode menu, once shown how to trigger it, is easy to browse and jump from; and that picking up and holding the mic feels natural now that it's a real grabbable object rather than a description on paper. This test also collects the first real data on comfort with hand-tracking versus controller-based clapping, and first-time reactions to media attachment and the mic prop, none of which existed as testable interactions before this build.

This test aims to discover where participants hesitate, guess wrong, or get stuck across the unaided portion of the workflow, so it's clear which parts of the concept are ready to carry into Prototype-02b and which still need a fix, an extra cue, or a rethink.

### Testing Methodologies

This testing plan uses moderated, task-based usability testing with a think-aloud protocol to evaluate the VR prototype built in Unity with the XR Interaction Toolkit, run on a Meta Quest headset. Tasks are grouped into broad chunks rather than individual gestures, so participants work out the specific interaction themselves rather than being walked through each one, keeping the unaided portion of the test genuinely unaided. Tour Mode is the exception: like depth control in Prototype-01, it has no on-screen visual cue to discover it by, so it's demonstrated directly. A structured five-question debrief follows each session to capture qualitative reactions, including a direct comparison to Prototype-01's desktop version where relevant.

### Prototype Description/Requirements

The prototype was built to let a participant clap to spawn a note, pick its shape, give it text, attach a placeholder media item and try the dictation mic, connect it to a second note, label that connection, revisit connected notes through a wrist-mounted menu, and delete a note.

It includes a clap gesture (hands together, either hand-tracking or the two controllers) that opens a floating note-creation panel; a shape picker (rectangle, circle, triangle, hexagon, star) with a hover-glow on each icon; a text field; placeholder Add Image / Add Video / Add Document buttons; a grabbable dictation mic mounted on the panel's edge on a constrained boom arm that can be pulled closer or pushed back within a fixed range; direct hand/controller grab-and-move for repositioning a note anywhere in 3D space; one connection handle per note that glows on hover and can be pulled to draw a thread to another note; a translucent poke-button on that thread that opens a VR keyboard to type and edit a free-text label; a wrist-mounted Tour Mode menu, triggered by rotating the wrist to look at it, listing every connected note as a scrollable list of poke-buttons that jump the player there, with a separate Exit Tour button to return; and a foot-level trash bin that deletes a note dropped into it.

Two developer-only shortcuts exist in the build for iteration speed, a "TEST: Click to Clap" button and a "Create Test Note" button, and must stay hidden or unused during actual participant sessions; participants should only ever see the real clap gesture and the full creation panel.

### Data Collection Method

Data is collected on the User Testing Observation Form, one per participant, included at the end of this document under User Testing Observation Form. For each task, three things are recorded: completion status, marked as Unaided, Prompted, or Failed; a hesitation/error tally, using a simple tick mark each time the participant pauses for more than a few seconds, reaches for the wrong object, or backtracks; and a task efficiency rating from 1 to 10, marked by circling a number, as a quick overall impression of how smoothly that task went. A free-text line under each task captures anything said aloud worth quoting. The whole session is also recorded, so exact timing and gesture detail can be pulled from the recording afterward if it's ever needed, rather than tracked live during the test.

The clap task also records which input the participant used, hand-tracking or controllers, and a direct comfort rating (Comfortable, Neutral, or Awkward) asked right after, since that is a stated reaction rather than an observed behaviour, and is one of the specific comparisons this round of testing exists to capture. A five-question written debrief is filled in at the end of each form.

### Testing Setup

#### Setup

Before each session, the build is running on the headset (or the XR Interaction Simulator, if the headset build isn't available on the day) with an empty scene, the observation form ready, and the session recording started. Consent is confirmed first, then the same task script is read to every participant so the test stays consistent. Nothing about how the interactions work is explained during the unaided task chunks, only Tour Mode is demonstrated, at the point it's needed. The two developer-only shortcut buttons are hidden or disabled before the session starts.

#### Testing Process

1. Introduce the prototype to the participant, including that notes are created by clapping.
2. Confirm consent to take part and to be recorded for educational purposes.
3. Ask them to clap to create a note, pick a shape, and give it some text.
4. Ask them to attach a placeholder image, then pick up the mic and try it.
5. Ask them to create a second note, connect it to the first, and label the connection, then connect a third note to one of the first two.
6. Demonstrate Tour Mode and have them jump between their connected notes using it.
7. Ask them to drop a note into the trash bin at their feet to delete it.
8. Debrief with the five questions below.

#### Task Script (Read Aloud to Each Participant)

**Step 1:** "This is a VR prototype for an idea-mapping tool I'm building. You create ideas by clapping your hands together. I'm going to ask you to try a few small tasks with it. There's no tutorial, so just explore as you go, and say out loud whatever you're thinking."

**Step 2:** "Before we start, do you consent to taking part in this testing session? And do you consent to this session being recorded, through notes and/or screen capture, for educational purposes only?"

**Step 3:** "Try clapping your hands together to create a note, then pick a shape and give it some text."

**Step 4:** "Try attaching an image to your note. Then pick up the mic sitting next to the panel and try speaking into it."

**Step 5:** "Create a second note, and connect it to your first one, then give that connection a label. Now create a third note and connect it to one of your first two."

**Step 6:** "There's also a Tour Mode. Rotate your wrist so you're looking at the back of your hand to bring up a menu, then poke an item on the list to jump to it. When you're ready, poke Exit Tour to come back."

**Step 7:** "Pick up one of your notes and drop it into the bin by your feet to delete it."

**Step 8 (debrief):** "A few questions to finish up." Ask the five questions listed under [Post-Testing Questionnaire](#post-testing-questionnaire) below.

#### User Testing Observation Form

Each participant's session is recorded live on the User Testing Observation Form, one copy filled in per participant.

Participant number: ______     Date: ______

Consent:  Y / N

Input used for clapping (circle one):  Hand-tracking / Controllers

**Task 1 — Clap to create a note, pick a shape, give it text**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Clap comfort rating:  Comfortable / Neutral / Awkward
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 2 — Attach a media item and try the mic**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 3 — Create a second note, connect it, label the connection, connect a third note**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Did the participant realise a note could connect to more than one other note without being told?  Y / N
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 4 — Tour Mode**

Completion:  Unaided / Prompted / Failed
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 5 — Delete a note using the trash bin**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

#### Post-Testing Questionnaire

1. Overall, how intuitive did clapping to create a note feel, compared to how you'd expect to start in a tool like this? (circle one):   1   2   3   4   5   6   7   8   9   10

2. What was the most confusing or frustrating moment during the tasks?
   ________________________________________________

3. Once you'd connected your first two notes, did it feel obvious that a note could connect to more than one other note, or did that surprise you?
   ________________________________________________

4. How did picking up and using the mic feel? Did it feel natural to interact with, physically?
   ________________________________________________

5. How did Tour Mode's wrist menu compare to the idea of walking through your connected ideas instead of scrolling? Did jumping between notes from a list make sense?
   ________________________________________________
