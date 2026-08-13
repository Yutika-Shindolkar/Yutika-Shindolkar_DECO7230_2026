# Week 2 Studio – Paper Prototyping & Testing Notes

**Session:** Week 2 Studio, Semester 2 2026 (DECO7230)
**Activity:** Low-fidelity paper prototyping and testing (per Week 2 Studio slides)
**Concept tested:** Infinite-canvas VR whiteboard — note creation and note-connection

## What was prototyped

A physical paper prototype was built to test two of the concept's core interactions:

- **Creating a note** (clap-to-create, then fill in content)
- **Connecting two notes** (pull a handle, extend a thread, attach to another note)

Paper cards acted as the screens/interface elements and were swapped or updated by hand
as the interaction progressed. Once two notes existed in the "scene," the original cards
were replaced with versions that had small paper handles on each side, representing the
micro-interaction that signals a note can be connected. Pulling a handle drew out an
attached thread; the participant pulled it free and placed it against the second card
to "connect" them.

## Test plan

- **What we were testing:** whether the note-creation and note-connection interactions
  are intuitive and physically make sense without explanation.
- **Methodology:** Wizard-of-Oz. Each participant was told the concept and given a task
  ("create two notes and connect them"), while the facilitator manually swapped cards
  and added elements in response to their actions, simulating how the system would
  respond.
- **Participants:** 2.
- **Data collected:** direct observation of task completion, plus verbal feedback
  gathered during and after each test. Both sessions were recorded on video
  (see `raw-testing-footage/`).

## Results

Both participants completed the task intuitively and successfully. Both also discovered
the "+" icon on the connecting thread (used to label the relationship between two notes)
without being prompted.

Three points of feedback came up:

1. One participant noted they might forget to clap to create a note until it becomes a
   habit.
2. It was unclear whether one note could connect to more than one other note, since each
   side of a note seemed to offer only a single, one-use handle.
3. Clapping felt slightly awkward while holding controllers.

## What this changed in the concept

1. **Handles are now reusable anchor points, not single-use plugs.** Pulling an
   already-connected handle starts a second thread, so one note can branch out to
   several others from the same point. An already-connected handle keeps a subtle
   glow/pulse rather than going dim, so it still visually invites another pull.
2. **A one-time onboarding hint** (a brief pulsing hand icon or short text cue) now
   appears the first time only, fading after the user's first successful clap, to
   bridge the initial learning curve without a persistent tutorial overlay.
3. **Hand-tracking is offered as an alternative to controller-based clapping** where the
   headset supports it, since clapping with bare hands is more natural than clapping
   with controllers gripped in each hand.

## Next round of testing (planned for the 2 Sept checkpoint)

The above resolutions haven't been tested yet, and Goal 3 (presentation/teleport) plus
several parts of Goal 1 (dictation, attaching images/video/documents) weren't covered by
this round at all. Next round should test:

- Whether the reusable, still-glowing handle actually communicates that a note can
  support more than one connection.
- Whether the one-time onboarding hint is enough for users to remember the clap gesture
  across a longer session.
- Whether hand-tracking clapping feels meaningfully easier than the controller version.
- The mic-tilt-to-dictate flow, attaching images/video/documents to a note, grouping
  connected flows into labelled sections, and the teleport-along-connections
  presentation mode.
