# Infinite-Canvas VR Whiteboard

## Testing plan for Interactive Prototype 1

**Pitch:** This project is a collaborative digital whiteboard tool, in the style of Miro or FigJam, but reimagined for full VR. Instead of dragging sticky notes on a flat screen, you clap to spawn a note in 3D space, pull glowing handles to thread connections between notes, and teleport along those connections to present your board to a group. The canvas is infinite and borderless, so the workspace can grow past the limits of any physical room.

## Testing Objective

From my concept, the paper-prototype round (Week 2) validated that people can understand and use clap-to-spawn and handle-based connections *in principle*, but paper cards can't test two things that only exist once the interaction is actually embodied in VR: whether the clap gesture is reliably detected as a deliberate input, and whether the reusable, still-glowing handle actually reads as "you can connect this note to more than one other note" once it's a real 3D object with real motion, rather than a flat paper cue.

This test aims to discover: (1) whether first-time users spawn a note using the clap gesture without being told the gesture, and (2) whether users understand, unprompted, that an already-connected handle can still be pulled again to start a second connection.

## Testing Methodology

This testing plan uses moderated, task-based usability testing with think-aloud, run one participant at a time in the VR headset. Each participant works through a short task sequence while I observe and log outcomes; I ask no leading questions about the gesture or the handles before they attempt each task, so first-attempt behaviour reflects genuine discoverability rather than instruction-following.

## Prototype description/requirements

The prototype was built to let a first-time user create and connect notes on an infinite VR canvas without spoken instruction, so I can observe whether the interaction design communicates itself.

It includes: a blank, borderless white canvas; a clap-detection system (proximity between the two hand/controller positions) that spawns a note with a floating input panel at the clap point (text field is functional; image/video/document and mic controls are present but visual-only placeholders for this round, since a horizontal prototype only needs to *appear* complete for the goals under test); and notes with four grabbable handles per side that glow, extend a connecting thread when pulled to another note, and stay lit afterward so a second thread can be pulled from the same handle. Presenting/teleport (Goal 3) is out of scope for this round; it is not the assumption being tested and would spread the build too thin for a first Unity project in this timeframe.

## Data collection method

During testing, I will log for each participant: whether the clap gesture was attempted unprompted and within how many tries; whether they connected a note to a second note without help; whether they connected the same note to a *third* note without being told the handle was reusable, and if not, what they assumed instead; a short post-task rating (1–5) on "It was clear I could connect one note to more than one other note"; and any think-aloud comments, captured as written notes and, with verbal consent, short audio/video matching the convention already used for the Week 2 footage.

## Testing Setup

Headset charged and paired, build deployed and confirmed running before the first participant arrives. Canvas reset to blank between participants (delete any spawned notes/threads). Data collection sheet (paper or phone) ready with one row per participant for the metrics above. Consent line ready to say out loud before recording starts. Because slots are short, have the next participant already briefed and waiting so headset handover is fast.

## Testing process (target ~4 minutes, leaves buffer under the 5-minute cap)

1. Welcome + one-sentence framing: "This is an early build of a VR whiteboard tool — I'll give you a few small tasks, just try things and talk out loud." No gesture hints given. (20 seconds)
2. Task 1 — "Create a note anywhere on the canvas, however you think might work." Observe: attempted gesture, tries to success, any verbal frustration. (60 seconds)
3. Task 2 — "Now create a second note and connect it to the first one." Observe: handle discovery, drag-to-connect success. (60 seconds)
4. Task 3 — "Create one more note, and connect that first note to it as well." Observe: does the already-glowing handle get tried again unprompted, or do they assume it's used up. (60 seconds)
5. Quick rating + 1–2 verbal questions: rating above, plus "what did you think that glowing handle meant before you tried it?" (60 seconds)
6. Thank you, reset canvas for next participant. (20 seconds)
