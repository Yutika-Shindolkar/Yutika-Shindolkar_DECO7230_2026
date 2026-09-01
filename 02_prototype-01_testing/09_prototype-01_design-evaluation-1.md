# Prototype 01 - Design Evaluation 1

_"This project reimagines a collaborative digital whiteboard, in the style of Miro or FigJam, as a fully immersive VR creation space. Instead of scrolling a flat 2D canvas, users capture ideas as notes, physically pull threads between notes to build structure, and walk through their finished board rather than flipping through slides. The goal is to make capturing and connecting ideas feel embodied and spatial rather than menu-driven."_

**Prototype tested:** Prototype 01, VR collaborative whiteboard, desktop build (mouse and keyboard)  
**Testing round covered:** Week 5 studio, 28 August 2026, 4 participants

## Objective and Validation Metrics

This test aimed to find out whether a first time user can independently discover and complete the full idea capture to connection workflow using only the prototype's own visual cues: creating an item, editing it, connecting two items via a handle, and labeling that connection. Depth control and Tour Mode have no on-screen visual cue, so those two tasks were demonstrated directly rather than tested unaided.

Success was defined per group of tasks:

- **Create, edit, delete (Tasks 1 and 5):** validated if most participants complete them unaided, with a high efficiency rating and a low hesitation tally.
- **Connect and label (Task 3):** validated if participants can complete the full workflow using the prototype's own cues alone, without being told how any part of it works.
- **Depth control and Tour Mode (Tasks 2 and 4):** validated if, once shown, participants operate them comfortably and describe them positively in the debrief.

## Results

The prototype was tested on a desktop build using a mouse and WASD keyboard controls, as a stand in for the VR headset and controller input the concept is ultimately designed for. This is most relevant to the depth control task below, since holding an item and adjusting its distance from the camera is a natural arm movement in VR but a key press on a keyboard here. The five tasks are broken out separately below.

| Participant | Task 1: Create marker, add text | Task 2: Depth control | Task 3: Connect and label | Task 4: Tour Mode  | Task 5: Create and delete a note |
|---|---|---|---|---|---|
| **P1** | Prompted, efficiency 8, hesitation 1 | Prompted, efficiency 8, Confusing | Prompted, efficiency 6, hesitation 2 | Unaided, efficiency 10 | Unaided, efficiency 10, hesitation 0 |
| **P2** | Unaided, efficiency 10, hesitation 0 | Prompted, efficiency 8, Confusing | Unaided, efficiency 8, hesitation 3 | Unaided, efficiency 10 | Unaided, efficiency 10, hesitation 0 |
| **P3** | Unaided, efficiency 10, hesitation 0 | Unaided, efficiency 8, Comfortable | Prompted, efficiency 8, hesitation 1 | Prompted, efficiency 8 | Unaided, efficiency 8, hesitation 1 |
| **P4** | Unaided, efficiency 10, hesitation 0 | Unaided, efficiency 10, Comfortable | Prompted, efficiency 8, hesitation 0 | Unaided, efficiency 10 | Unaided, efficiency 10, hesitation 0 |

**Task 1, create marker and add text.** 3/4 participants (P2, P3, P4) completed this unaided. P1 needed a prompt for one hesitation, efficiency 8. P3 needed a prompt to press Enter to confirm text after entering it.

**Task 2, depth control.** All four completed this once it was demonstrated. P3 and P4 rated it Comfortable. P1 and P2 rated it Confusing: P1 did not have an item selected or held when pressing Page Up and Page Down, and P2 had an item selected but was not actively dragging it. P3 completed it comfortably but noted it did not seem too useful with only a couple of items on screen.

**Task 3, connect and label.** All four eventually connected the two items and labeled the connection. Three (P1, P3, P4) needed a prompt clarifying that the thread has to be released exactly on the handle. P2 self corrected on a second attempt without a prompt. P4 specifically noted that the highlight cue appears over the whole item rather than the handle, and initially tried releasing the connection on the note itself for that reason.

**Task 4, Tour Mode.** 3/4 (P1, P2, P4) stepped through it unaided with an efficiency of 10. P3 needed a prompt after missing the on screen tooltip text, efficiency 8.

**Task 5, create and delete a note.** All four completed this unaided, efficiency 8 to 10. P3 tried to disconnect the note from its connection before deleting it.

**Selected debrief quotes:**

On connecting: P1, _"the idea to connect was intuitive, but the reactivity of the connection points could be" improved. P4, "the highlight only pops up when your cursor's on the sphere, but when you're on the actual connection points, it's hard to, you have to be exactly on it."_

On Tour Mode: P1, _"I would like to have it," comparing it to a presentation. P2, "the next, next, previous, previous, loved it, and that it zoomed in." P3, "especially because Miro board and all, they are in 2D, but here you can actually be in that space." P4 wants a way to jump directly between items: "I'd just like to have a list, a mapping that says here's point ABC or 123, and then maybe have the option to jump between other nodes."_

**On wanting to use the tool:** P1, yes, especially in an XR or MR format. P2, yes, _"I'm buying it today."_ P3, maybe, held back by a general VR learning curve rather than anything specific to this concept. P4, yes, but would prefer using it on a tablet with a pen rather than a desktop.

## Analysis and Insights

Connecting two items is already conceptually intuitive. Both P1 and P2 described understanding the mechanic without being told what to do. What is not yet discoverable on its own is the precision it needs: 3/4 participants only completed it after being told the thread has to be released exactly on the handle. P4's comment points to a likely reason, the current hover highlight is tied to the whole item rather than the handle, so the feedback tells a user "you are near something" rather than "you are on the exact spot that matters." This reads as a feedback gap rather than a conceptual one.

Depth control was operable by everyone once shown, so the mechanic itself is not the problem. What came through instead, particularly from P3, is that with only one or two items in the test scene there was no situation where pushing something further away or pulling it closer actually mattered. Testing it with so few items on screen, and with a keyboard standing in for what will eventually be a hand movement, may make it seem less useful and less natural than it actually is.

For tour mode all four participants reacted positively in the debrief, and the comparison several drew, unprompted, to a flat 2D Miro board is close to the value proposition the whole concept is built around. The one piece of suggestion, from P4, was to have a key map as way to jump directly to a specific item instead of stepping through everything connected in between.

Capture and delete (Tasks 1 and 5) are in good shape, high efficiency, mostly unaided, no repeated pattern of confusion. 

## Evaluation of Aims

Discoverability of capture and delete: validated.  
Both were completed unaided by most participants with high efficiency and minimal hesitation.

Discoverability of the connect and label workflow from the prototype's own cues alone: partially validated.  
The underlying idea, drag from one item toward another to connect them, then label the thread, was understood by every participant without being told. The precise execution, releasing exactly on the handle, was not discoverable from the current visual cues alone. The concept is validated, the current feedback design for handles needs improvement.

Tour Mode feeling natural once demonstrated: mostly validated.  
Debrief reaction was unanimously positive, but completion was not fully unaided, 3 of 4 stepped through it without a prompt and one missed the on-screen tooltip, so it is mostly rather than fully validated.

Depth control feeling natural once demonstrated: inconclusive rather than invalidated.  
Everyone could operate it, but the sparse test scene and desktop input mean this round cannot really say whether it feels natural in the situation it is meant for.

## Concept Iteration

1. Give each handle its own highlight or glow, separate from the item's general hover state, so the exact interactive point is visually distinct from the rest of the note or marker. This answers P4's observation directly, and should let the interface communicate the "release exactly here" requirement itself, rather than needing a facilitator to say it out loud.
2. Add a small tolerance radius around each handle so a release that lands close, not only exactly on top, still completes the connection. This keeps the same interaction model everyone already understood, while reducing how much it depends on pixel perfect precision.
3. Add a non-linear way to move through Tour Mode, for example a small list or keymap along the side showing the sequence of connected items, so a user can jump straight from item one to item ten instead of stepping through everything in between.
4. Leave the depth control mechanic as is for now, and retest it in a scene with several items placed at different depths, ideally with an actual controller instead of a keyboard, before deciding whether it needs a design change or just a busier context to prove its use.
5. Build more on-screen micro-interactions and feedback in general, not only for handles. Task 4 had an on-screen tooltip and reached a higher unaided completion rate (3 of 4) than Task 2 or Task 3, both of which relied on a verbal explanation instead. Visual, on-screen cues appear to carry the instructions better than a facilitator saying them out loud, so new features should lean on this rather than needing to be explained.
6. Add the ability to edit a connection's label after it has already been set, rather than only being able to set it once at the moment the connection is made.

## Reflection on the Concept, Design, Methodologies, and Future Testing and Planning

This round of testing did what it needed to do. Splitting tasks into demonstrated versus unaided cleanly separated what is genuinely discoverable in the concept, capture, delete, the idea of connecting, from what still relies on someone explaining it, the precision handles need. Recording completion type, hesitation tally, and an efficiency rating on the same form made it straightforward to compare all four participants against each other rather than reading each session in isolation.

The main thing to change for the next round is testing conditions rather than method. Depth control in particular was tested in a close to empty scene and through a keyboard proxy for what will eventually be a physical VR gesture, so this round cannot really say whether the mechanic itself needs to change. The next testing round should test depth control and connection precision inside a denser canvas of several connected items, and if the build supports it by then, on an actual headset and controller rather than a desktop proxy.

Future testing should not stop at retesting what this round already flagged under better conditions. It should also cover whatever gets newly built on top of the current prototype in response to these findings. The keymap feature proposed above is a good example, once it exists, it needs its own round of testing rather than being assumed to work just because it responds to real feedback.

## Appendix: Raw Data

Full per participant observation form data (scanned form and transcribed data) and post testing debrief transcripts for all four participants are held in the project repository and referenced here rather than reproduced in full:

- `02_prototype-01_testing/01_prototype-01_testing-plan.docx` (testing plan and task script this round followed)
- `02_prototype-01_testing/02_prototype-01_user-testing-observation-form.pdf` (blank observation form template used during testing)
- `02_prototype-01_testing/03_prototype-screenshots/` (screenshots of the test scene and a participant testing session)
- `02_prototype-01_testing/04_raw-testing-footage/` (full session recordings, linked via OneDrive)
- `02_prototype-01_testing/05_user-testing-observation-form-data/` (observation forms, participants 1 to 4)
- `02_prototype-01_testing/06_post-testing-questionnaire-recordings/` (debrief audio recordings, linked via OneDrive)
- `02_prototype-01_testing/07_post-testing-questionnaire-transcripts/` (debrief transcripts, participants 1 to 4)
- `02_prototype-01_testing/08_post-testing-questionnaire-data/` (debrief data, participants 1 to 4)