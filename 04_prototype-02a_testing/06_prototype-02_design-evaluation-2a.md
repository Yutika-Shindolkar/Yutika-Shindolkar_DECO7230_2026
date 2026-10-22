# Prototype 02a - Design Evaluation 2a

_"This project reimagines a collaborative digital whiteboard, in the style of Miro or FigJam, as a fully immersive VR creation space. Instead of scrolling a flat 2D canvas, users capture ideas as notes, physically pull threads between notes to build structure, and walk through their finished board rather than flipping through slides. The goal is to make capturing and connecting ideas feel embodied and spatial rather than menu-driven."_

**Prototype tested:** Prototype 02a, VR collaborative whiteboard, Meta Quest headset with controllers  
**Testing round covered:** 9 October 2026, 5 participants

## Objective and Validation Metrics

This test aimed to find out whether a first time user can independently discover and complete the capture to connection workflow in VR with real headset and controller input. Nothing was demonstrated, so every task was attempted unaided first and any facilitator help is recorded as a prompt.

Success was defined per group of tasks:

- **Clap to create a note (Task 1):** validated if most participants complete it unaided with a high efficiency rating, and most rate the clap Comfortable.
- **Mic (Task 2):** validated if most participants pick it up and move it unaided, and most rate it Comfortable.
- **Two shapes, connect and label (Tasks 3 and 4):** validated if most participants complete both unaided with a low hesitation tally, using the prototype's own cues alone.
- **Delete (Task 5):** validated if most participants complete it unaided with a high efficiency rating.
- **Debrief:** validated if participants find the clap intuitive (Question 1) and say they would use the tool.

## Results

Five participants attempted all five tasks on a Meta Quest headset with two controllers. Each cell shows completion type, efficiency (1 to 10), hesitation tally, and the comfort rating where recorded.

| Participant | Task 1: Clap To Create A Note | Task 2: Mic | Task 3: Two Different Shapes | Task 4: Connect And Label | Task 5: Delete A Note |
|---|---|---|---|---|---|
| **P1** | Unaided, Efficiency 10, Hesitation 0, Comfortable | Unaided, Efficiency 8, Hesitation 0, Neutral | Unaided, Efficiency 9, Hesitation 0 | Unaided, Efficiency 8, Hesitation 1 | Unaided, Efficiency 10, Hesitation 0 |
| **P2** | Unaided, Efficiency 10, Hesitation 0, Comfortable | Prompted, Efficiency 6, Hesitation 2, Awkward | Prompted, Efficiency 7, Hesitation 1 | Unaided, Efficiency 6, Hesitation 0 | Unaided, Efficiency 10, Hesitation 0 |
| **P3** | Prompted, Efficiency 9, Hesitation 0, Awkward | Unaided, Efficiency 8, Hesitation 0, Neutral | Prompted, Efficiency 7, Hesitation 1 | Unaided, Efficiency 7, Hesitation 0 | Prompted, Efficiency 9, Hesitation 1 |
| **P4** | Unaided, Efficiency 9, Hesitation 1, Awkward | Unaided, Efficiency 8, Hesitation 0, Awkward | Unaided, Efficiency 9, Hesitation 0 | Unaided, Efficiency 7, Hesitation 0 | Unaided, Efficiency 10, Hesitation 0 |
| **P5** | Unaided, Efficiency 9, Hesitation 1, Awkward | Unaided, Efficiency 8, Hesitation 1, Neutral | Unaided, Efficiency 9, Hesitation 0 | Prompted, Efficiency 7, Hesitation 2 | Unaided, Efficiency 10, Hesitation 0 |

**Task 1, clap to create a note.** 4/5 completed this unaided, efficiency 9 to 10. P3 needed a prompt to clap faster after the first attempt did not register. P1 and P2 rated the clap Comfortable; P3, P4 and P5 rated it Awkward, and P4 and P5 bumped the controllers together. Question 1 ratings were 10, 10, 9, 10 and 6 (mean 9.0).

**Task 2, mic.** 4/5 completed this unaided, efficiency 8. P2 tried to poke the mic and needed to be told to grab and move it, efficiency 6. Three rated it Neutral, two Awkward, and nobody Comfortable.

**Task 3, two different shapes.** All five created two shapes; 3/5 did so unaided, efficiency 9. P2 forgot to clap again for a second note, and P3 tried to move the first note to make one and needed a prompt.

**Task 4, connect and label.** All five connected and labeled, 4/5 unaided. Efficiency was 6 to 8, the lowest mean of the five tasks (7.0). P1 was unsure which node to use, P2 found far apart notes hard to join, and P5 needed a prompt to start.

**Task 5, delete a note.** 4/5 completed this unaided, efficiency 10. P3 did not see the trash can at first and needed a prompt.

**Selected debrief quotes:**

On the clap: P5, _"I did not like the clap thing because it bumped the controller and I almost dropped it. I kind of got scared. I wouldn't like it like every time."_

On the mic: P2, _"I didn't know what to do. I felt like it wasn't really of any use."_ P4, _"It felt weird to move it around because I didn't know how much it will move, or what to do with it."_

On connecting: P1, _"There were so many dots and circles around the notes. I was a little confused, but it was pretty easy once I understood I can connect it to anything."_ P5, _"at first I didn't know how to start a connection, but then like a tooltip would have helped"_. P4, _"I like the fact that we could label things, like each line connections also again."_

**On wanting to use the tool:** all five were positive. P1 and P4 said they would definitely use it. P2, _"if I keep using a headset often, then it would be nice to have to ideate in 3D space."_ P3 found wearing the headset every time a little tiresome, but _"if it was like more accessible, I'd definitely try using this more often."_

## Analysis and Insights

The clap is discoverable but physically risky. 4/5 started a note unaided and the Question 1 mean of 9.0 shows the gesture reads as an intuitive way to begin. The problem is performing it with a controller in each hand: 3/5 rated it Awkward, two bumped the controllers, and P5's rating of 6 comes from nearly dropping one. P3's first clap did not register until told to clap faster, which points to a feedback gap, since the user cannot see how hard or fast a clap needs to be.

The mic is the weakest interaction. Nobody rated it Comfortable, and the confusion was about purpose and affordance rather than skill. P2 poked at it because nothing showed it could be grabbed, P4 did not know how far it would move, and the task wording ("play with the mic") left room for misreading.

Creating the first note was easy, but creating a second was where memory failed. The on-screen cue covers the first clap, yet P2 forgot to clap again and P3 tried to duplicate the first note by moving it.

Connecting and labeling is conceptually understood. All five completed it. It still had the lowest mean efficiency (7.0), and the difficulty sits around where to start and where to end: P5 did not know how to start, P1 was unsure which node to use among many dots and circles, and P2 struggled when the notes were far apart. This reads as a visual noise and guidance problem. In Prototype 01, 1 of 4 participants completed connect and label unaided, against 4 of 5 here, though the input and scenes differed, so the comparison is directional only.

Delete works: 4/5 did it unaided at efficiency 10, and P3's difficulty was seeing the bin, not understanding the action. Overall enthusiasm is high, so the concept is landing and the remaining work is in the physical details of the interactions.

## Evaluation of Aims

Discoverability of creating a note by clapping: **partially validated.**  
4/5 started a note unaided and Question 1 averaged 9.0, but only 2/5 rated the clap Comfortable. The idea is understood, the physical execution needs work.

Mic interaction: **not validated.**  
4/5 moved it unaided, but nobody rated it Comfortable and participants could not see what it was for.

Creating notes in different shapes: **mostly validated.**  
All five created two different shapes and 3/5 did it unaided. The gap was remembering how to create a second note.

Connect and label from the prototype's own cues alone: **mostly validated.**  
All five completed it and 4/5 did so unaided, with a positive debrief. The cues for where to start, which node to use and how to reach distant notes are not clear enough yet.

Delete by dropping a note in the bin: **validated.**  
4/5 completed it unaided at efficiency 10. The one miss was not seeing the bin.

## Concept Iteration

1. Make the clap forgiving and add feedback. Show a visual and haptic confirmation when the hands meet and lower the speed and closeness needed, so a soft clap registers (P3). Add an alternative way to open the panel (P4, P5), and test the clap with hand tracking, which removes the controller bump.
2. Give the mic a visible grab cue and purpose. Highlight it on hover (P2), show how far it can move (P4), and give a visible response while held, such as a recording indicator. Live dictation is the stronger fix.
3. Add a persistent cue for making another note, such as a small "clap for a new note" hint near the last note (P2, P3).
4. Reduce visual noise and guide connections. Show connection points only when a note is hovered or grabbed (P1), highlight valid target points while pulling (P1, P5), and add a short first use tooltip (P5).
5. Make connecting distant notes easier, for example a thread that snaps to the nearest connection point (P2).
6. Make the bin easier to spot the moment a note is picked up, for example with a brief pulse (P3).

## Reflection on the Concept, Design, Methodologies, and Future Testing and Planning

Running every task unaided did what it needed to do. It is clear which parts of the concept stand on their own, such as delete, and which still rely on someone explaining them, such as grabbing the mic, making a second note and starting a connection. The comfort rating also paid off: the clap scored 9.0 for intuitiveness but was rated Awkward by 3/5, so intuitive and comfortable are separate measures.

Two things should change in the method. Task wording should describe an action, since "play with the mic" was misread by P2. The hesitation tally stayed between 0 and 2, so timing should be captured next round. With five participants and only a couple of notes in the scene, results are directional, not conclusive.

The next round should test the iterations above on the headset: the clap with feedback and an alternative trigger, the mic with a purpose, and the connection cues in a denser scene. Tour Mode and depth control were not part of this round and still need headset testing.

## Appendix: Raw Data

Full per participant observation form data (scanned form and transcribed data) and post testing debrief transcripts for all five participants are held in the project repository and referenced here rather than reproduced in full:

- `04_prototype-02a_testing/01_prototype-02a_testing-plan.docx` (testing plan and task script this round followed)
- `04_prototype-02a_testing/02_prototype-02a_user-testing-observation-form.pdf` (blank observation form template used during testing)
- `04_prototype-02a_testing/03_prototype-screenshots/` (screenshots of the prototype and a participant testing session)
- `04_prototype-02a_testing/04_user-testing-observation-form-data/` (observation forms, participants 1 to 5)
- `04_prototype-02a_testing/05_post-testing-questionnaire-transcripts/` (debrief transcripts, participants 1 to 5)
