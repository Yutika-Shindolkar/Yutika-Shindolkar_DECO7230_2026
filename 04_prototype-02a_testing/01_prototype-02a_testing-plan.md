# Prototype-02a - Testing Plan

_"This project reimagines a collaborative digital whiteboard, in the style of Miro or FigJam, as a fully immersive VR creation space. Instead of scrolling a flat 2D canvas, users capture ideas as notes, physically pull threads between notes to build structure, and walk through their finished board rather than flipping through slides. The goal is to make capturing and connecting ideas feel embodied and spatial rather than menu-driven."_

### Testing Objective

Prototype-01 tested the core idea-capture loop on a desktop screen, using a mouse and keyboard as a stand-in for VR input. Prototype-02a is the same concept rebuilt for a Meta Quest headset, so this round tests that same loop with real controller input, and tests the interactions desktop could not: the clap gesture, the mic, and dropping a note into a bin. The question is whether a first-time user can independently discover and complete the capture-to-connection workflow in VR: clapping to create a note, creating notes in different shapes, connecting two of them, labeling that connection, and deleting a note, without being told how any of it works.

The assumptions being tested are that the clap gesture can be discovered from the floating on-screen prompt alone; that the mic reads as something that can be moved once it is seen on the creation panel; that the shape icons and panel buttons can be found from their hover-glow alone; that a note's connection handles (which appear when the controller ray is on the note) and the small green plus sphere on a connecting thread can be found and used without instruction; and that holding a note and lowering it into the bin on the floor reads as deleting it. Tour Mode is not tested this round, and nothing is demonstrated to the participant, so every task is attempted unaided first.

This test aims to discover where participants hesitate, guess wrong, or get stuck across the whole workflow, so it becomes clear which VR interactions are ready to carry forward and which still need a fix, an extra cue, or a rethink.

### Testing Methodologies

This testing plan uses moderated, task-based usability testing with a think-aloud protocol to evaluate the VR prototype built in Unity with the XR Interaction Toolkit, run on a Meta Quest headset with its two controllers. Tasks are grouped into broad chunks rather than individual gestures, so participants work out the specific steps themselves rather than being walked through each one, which keeps the whole test genuinely unaided. No audio or video is recorded this round, so the facilitator records everything live on the observation form and writes down the debrief answers. A structured five-question debrief follows each session to capture qualitative reactions.

### Prototype Description/Requirements

The prototype was built to let a participant clap to create a note, try the dictation mic, create notes in different shapes, connect two notes and label that connection, and delete a note.

It includes a clap gesture (bringing the two controllers together quickly) that opens a floating note creation panel, with a floating prompt on entry telling the player to clap; a shape picker with rectangle, circle, triangle, hexagon and star icons that glow on hover; a text field with a VR keyboard docked below the panel; placeholder Add Image, Add Video and Add Document buttons, which are not tested this round; a dictation mic clamped to the panel's edge on a boom arm, where the arm can be tilted closer to or further from the panel and the mic head can be turned, each within a limited range (the mic is a physical prop only and does not capture speech); direct controller grab-and-move for repositioning a note anywhere in 3D space; connection handles at the middle of each edge of every note (four on a rectangle or circle, three on a triangle, six on a hexagon, five on a star) that appear when the controller ray is on the note, turn yellow when aimed at, and can be pulled out with the grip to draw a thread to a handle on another note, where it snaps on and both handles turn green; a translucent green plus sphere on each thread that opens the keyboard to type a label, which can be edited again by pressing it; connection lines that can be grabbed and yanked to disconnect; and a trash can that appears on the floor a couple of metres ahead while a note is held and deletes the note when it is released over it, with a red glow outline and controller vibration. A wrist-mounted Tour Mode also exists in the build, but it is not a tested interaction this round.

Development-only shortcuts (a test clap button, a Create Test Note button and a debug readout) switch themselves off in a normal build, so the participant build is a standard non-development build and participants only ever see the real clap gesture and the full creation panel.

### Data Collection Method

Data is collected on the User Testing Observation Form, one per participant, included at the end of this document under User Testing Observation Form. For each task, three things are recorded: completion status, marked as Unaided, Prompted, or Failed; a hesitation/error tally, using a simple tick mark each time the participant pauses for more than a few seconds, reaches for the wrong object, or backtracks; and a task efficiency rating from 1 to 10, marked by circling a number, as a quick overall impression of how smoothly that task went. A free-text line under each task captures anything said aloud worth quoting. Because nothing is recorded, timing is not captured; the tally and efficiency rating stand in for it.

The clap task and the mic task each also get a direct comfort rating (Comfortable, Neutral, or Awkward), asked right after the participant tries it, since that is a stated reaction rather than an observed behaviour. The shapes task records whether two different shapes were chosen, the connect-and-label task records separately whether the connection was made and whether it was labelled. A five-question written debrief is filled in at the end of each form.

### Testing Setup

#### Setup

Before each session, the standard build is installed and running on the headset with an empty scene, the controllers are charged, a clear standing area is set up, the observation form is ready, and the two developer shortcuts are confirmed off. Consent is confirmed first, then the same task script is read to every participant so the test stays consistent. Nothing about how the interactions work is explained during any task.

#### Testing Process

1. Introduce the prototype to the participant, fit the headset and controllers, and explain that they can stop at any time if they feel unwell.
2. Confirm consent to take part and to written notes being taken for educational purposes.
3. Ask them to start creating a note.
4. Ask them to try the mic on the panel.
5. Ask them to create two notes in two different shapes, giving each some text.
6. Ask them to connect the two notes and label the connection.
7. Ask them to pick up one of the notes and get rid of it.
8. Debrief with the five questions below.

#### Task Script (Read Aloud to Each Participant)

**Step 1:** "This is a VR prototype for an idea-mapping tool I'm building. You use the two controllers to capture ideas as notes and link them together. There's no tutorial, so just explore as you go, and say out loud whatever you're thinking. If you feel unwell at any point, tell me and we'll stop straight away."

**Step 2:** "Before we start, do you consent to taking part in this testing session? And do you consent to me taking written notes on what you do and say, for educational purposes only?"

**Step 3:** "Try to start creating a note."

**Step 4:** "There's a mic on the panel. Have a play with it."

**Step 5:** "Create two notes, each in a different shape, and give each one some text."

**Step 6:** "Connect your two notes together, then give that connection a label."

**Step 7:** "Pick up one of your notes and get rid of it."

**Step 8 (debrief):** "A few questions to finish up." Ask the five questions listed under [Post-Testing Questionnaire](#post-testing-questionnaire) below.

#### User Testing Observation Form

Each participant's session is recorded live on the User Testing Observation Form, one copy filled in per participant. See [02_prototype-02a_user-testing-observation-form.pdf](./02_prototype-02a_user-testing-observation-form.pdf) for the printable version used during each user testing.

Participant number: ______     Date: ______

Consent:  Y / N

**Task 1 - Clap to create a note**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Clap comfort:  Comfortable / Neutral / Awkward
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 2 - Play with the mic**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Mic comfort:  Comfortable / Neutral / Awkward
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 3 - Create two notes in two different shapes**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Two different shapes:  Y / N
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 4 - Connect the two notes and label the connection**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Connected:  Y / N     Labelled:  Y / N
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

**Task 5 - Delete a note**

Completion:  Unaided / Prompted / Failed
Hesitation / wrong-reach tally: ________________________
Task efficiency (circle one):   1   2   3   4   5   6   7   8   9   10
Notes:
________________________________________________

#### Post-Testing Questionnaire

1. Overall, how intuitive did starting a note by clapping feel? (circle one):   1   2   3   4   5   6   7   8   9   10

2. What was the most confusing or frustrating moment during the tasks?
   ________________________________________________

3. How did the mic feel to interact with? Did it make sense for it to be attached to the panel like that?
   ________________________________________________

4. Did connecting two notes and labeling that connection feel like a natural next step, or did you have to think about what to point at and press?
   ________________________________________________

5. Would you want to use a tool like this for organizing your own ideas? Why or why not?
   ________________________________________________
