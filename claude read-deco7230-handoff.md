# DECO7230 handoff synopsis — for Claude Code

Context: Yutika Shindolkar, Master of Interaction Design student at UQ, building VR
prototypes in Unity for DECO7230 (Digital Prototyping and Extended Reality). First-time
Unity/XR dev, wants ELI5-first explanations with industry context, not just
assignment-minimum. This doc hands off from a Claude (chat/Cowork) session to a Claude
Code session working directly in the repo.

**Today's date: 2026-10-03.** Design Concept Report checkpoint 3 is due **7 Oct 2026,
12pm — 4 days away.** It's an iteration pass on the same concept (not a new one),
2-page max, submitted on Blackboard. Confirm with Yutika whether that's the immediate
priority before picking up any of the Unity work below.

## Repo and folders (confirm live, has been renumbered before)

- Git repo: `N:\GitHub Desktop\Yutika-Shindolkar_DECO7230_2026\` — the authoritative,
  submitted/tracked source.
- Holding folder (not submitted via repo): `N:\Work\UQ\SEM 02\Digital Prototyping and
  Extended Reality (DECO7230)\` — briefs, raw testing footage.
- Confirmed-current repo layout (as of this session, verify with a fresh `ls` before
  trusting it — this has changed multiple times):
  - `00_design-concept-reports\` — submitted report PDFs.
  - `01_prototype-01\` — **IP1 Unity project**, VR template, Editor 6000.3.24f1(ish —
    verify), the graded "Interactive Prototype 1" coursework (35%, hurdle, already
    tested in class 27-28 Aug 2026 with 4 participants — that round is done).
  - `02_prototype-01_testing\` — IP1's testing plan + observation form PDF + digitized
    participant data.
  - `03_prototype-02\` — **IP2a Unity project**, 3D template + manually-added XR
    Interaction Toolkit/OpenXR, Editor version **6000.3.24f1 specifically** (course
    requirement, differs from 01_prototype-01's Editor version — check before opening).
    This is the real VR build: clap-gesture note creation, shape picker, media attach,
    grabbable mic, handle-based connect+label, wrist-mounted Tour Mode, foot-bin delete.
  - `04_prototype-02a_testing\` — IP2a's testing plan (`.md`+`.docx`) and observation
    form (`.pdf`), both finished and committed this session (2026-09-25 work).
  - IP2a itself was due 25/09/2026 — **already past as of today, confirm with Yutika
    whether it was actually submitted/tested in class.**
  - IP2b (the actual graded 35% final, due 22-23 Oct 2026, must be tested on a real
    Quest headset, must differ meaningfully from IP2a) maps to a `05_prototype-02b_*`
    folder per earlier planning notes — not yet started as of last check, confirm.
  - `06_applied-class\kitchen-diorama\` (or similar numbering) — ungraded 3D-template
    learning project, unrelated to the graded coursework, don't conflate with the above.

## IP1 (`01_prototype-01`) — current state

Was pure desktop mouse/keyboard (PlayerMove.cs free-fly camera, DragNote.cs mouse-drag,
UI Button clicks) — this is what was actually tested with the 4 participants in August.
**This session added real VR support** so it can also run on a Quest headset:

1. `Assets/Editor/VRSetup.cs` (new file, uncommitted) — a `[MenuItem("Tools/VR Setup/
   Add XR Origin To World Scene")]` script that instantiates the `XR Origin (XR Rig)`
   prefab from `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/`
   into `Assets/Scenes/World.unity` (the actual whiteboard scene — NOT `SampleScene.
   unity`, which is just a leftover Meta sample demo scene), copies the old Main
   Camera's position/heading onto it, disables (doesn't delete) the old Main Camera,
   and saves the scene.
   **Status: written, but not yet confirmed run.** Yutika was told to open World.unity
   and run Tools > VR Setup > Add XR Origin To World Scene herself — confirm whether
   she actually did this before assuming the rig exists in the scene.
2. `ProjectSettings/EditorBuildSettings.asset` — fixed: Build Settings' scene list was
   pointing at `Assets/Scenes/SampleScene.unity` (a Meta sample scene, not the
   coursework scene) — this alone would explain a headset build either showing the
   wrong thing or hanging. Changed to `Assets/Scenes/World.unity`. **Committed? No —
   verify git status, this was a direct file edit, not yet committed.**
3. `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` — fixed: both the Android and
   Standalone OpenXR loader entries had `m_AutomaticLoading: 0` / `m_AutomaticRunning:
   0` (i.e. OpenXR was present but never actually turned on for either platform,
   despite the Meta Quest OpenXR feature group itself being correctly enabled). Flipped
   both to `1`. This was very likely the actual root cause of an unresolved "three-dot
   loading screen never finishes" hang on-headset that was being debugged via `adb
   logcat` right before this fix (session context strongly suggests this was IP1, not
   IP2a, confirm with Yutika) — a scene with no properly-loading OpenXR session would
   produce exactly that symptom. **Not yet committed, not yet verified on-headset.**
4. OpenXR Android feature config (Meta Quest Support, Oculus Touch + Meta Quest Touch
   Plus controller profiles, hand tracking) was **already correctly set up** from the
   original VR template install — did not need touching.

**Immediate next steps for IP1:**
- Confirm Yutika ran the Tools menu item and the XR Origin now exists in World.unity.
- `git status` in `01_prototype-01` and commit the three changes above (new
  VRSetup.cs, EditorBuildSettings.asset, XRGeneralSettingsPerBuildTarget.asset) if not
  already committed.
- Do a real on-headset test build to confirm the three-dot hang is actually fixed.
- Scope was deliberately limited to "just get it running on-headset" — existing
  mouse-driven interactions (dragging notes, clicking the palette, Tour Mode) do NOT
  yet respond to VR controllers. That's a separate, bigger follow-up if Yutika wants
  IP1 fully controller-interactive (not currently requested/required for IP1 itself).

## IP2a (`03_prototype-02`) — current state, has an UNCOMMITTED, UNVERIFIED fix in flight

This is the one most likely to need immediate attention — there's a real, working-tree
change sitting uncommitted with unknown headset-launch status.

**`Assets/Editor/IP2aSceneBuilder.cs`** — the idempotent editor-scripted scene builder
(`[MenuItem]`-driven, `FindOrCreate`/`FindOrCreateChild` pattern) that builds the whole
IP2a scene. Current working tree has two changes beyond the last real commit
(`fb2d78b`, "Add typed VR keyboard and dummy test note button to IP2a"):

1. **Wrist-mounted Tour Mode wiring** (`BuildTourMode()` + `BuildTourListItemPrefab()`)
   — menu-driven list of connected notes mounted on the left wrist, poke-to-jump,
   separate Exit Tour button. Matches the locked spec: rotate-wrist-to-reveal trigger,
   scrollable button list (not thumbnails), poke selection.
2. **Fixed a real, previously-undiscovered bug**: the rig's `Left Controller` /
   `Right Controller` transforms are nested under `Camera Offset` (siblings of Main
   Camera), not direct children of the XR Origin root. The old code did
   `rig.transform.Find("Left Controller")` (single segment, no path) which **always
   returned null** — meaning `ClapGestureDetector.leftHand/rightHand` were never
   actually wired, so **the real clap gesture (hands-together proximity) has likely
   never worked on a headset**, only the "TEST: Click to Clap" dev-shortcut button was
   ever actually exercised. Fixed to `rig.transform.Find("Camera Offset/Left
   Controller")` / `"...Right Controller"`.

**This fix is staged (`git add`-ed) but THREE separate `git commit` attempts timed out
(30s, then 90s) without completing or erroring — cause never diagnosed** (not a stale
`.git/index.lock`, confirmed each time; `git status`/`git add`/`git checkout` all work
fine on this repo, only `git commit` hangs). HEAD is still at `fb2d78b`. **Also never
re-verified live** — the scene was never re-built in the Editor after this fix to
confirm Tour Mode objects actually get created, or that the real clap gesture now
works on-headset. A duplicate-`VRKeyboard`-GameObjects issue was also noticed once
mid-troubleshooting and never explicitly re-checked after a separate file-reversion
incident (see below) was recovered from.

**File-reversion incident (recovered, cause never found):** earlier in this work,
`IP2aSceneBuilder.cs`'s working copy silently reverted to an older, pre-`fb2d78b`
version outside of any git operation performed, while GitHub Desktop was open —
recovered via `git checkout HEAD -- <file>` and re-applied the edits. Leading
suspect was GitHub Desktop or a second concurrent Claude session touching the same
file; never confirmed. Worth keeping an eye out for a repeat.

**Open, unresolved bug report from Yutika: "none of the buttons click in note
creation panel."** Investigation was started but got interrupted before reaching a
diagnosis:
- `NoteCreationPanel.cs` (235 lines, `Assets/Scripts/`) was read in full — logic looks
  correct (shape buttons, media buttons, Discard/Create all call into real public
  methods, media buttons correctly gate on Rectangle being selected via
  `interactable.enabled = active`).
- `IP2aSceneBuilder.cs` was grepped for the button-wiring code — `XRSimpleInteractable`
  components, colliders (`RemoveCollider`/`AddComponent<BoxCollider>`), and
  `UnityEventTools.AddVoidPersistentListener`/`AddIntPersistentListener` calls for
  Discard/Create/shape buttons/media buttons all look correctly wired in the code as
  written (lines ~240-373 of the current 1602-line file).
- **Not yet checked:** whether this is a live-scene problem (stale built scene
  predating a code fix, listeners not re-wired because the builder wasn't re-run) vs.
  an actual code bug; whether this report is about controller/ray interaction, hand
  tracking/poke, or the in-Editor XR Device Simulator; whether it's connected to the
  uncommitted Tour Mode/hand-transform fix above (unlikely — different code paths —
  but the scene may be stale relative to either fix); Interaction Layer Mask
  mismatches between the poke/ray interactor and the buttons; whether an
  `XR Interaction Manager` exists and is correctly referenced in the scene.
- Computer-use (live screen control of Yutika's Unity Editor) was requested for this
  investigation and **declined by Yutika** — so this needs either her describing what
  she's seeing/doing in more detail, or another computer-use request with a narrower,
  clearly-justified ask.

**Immediate next steps for IP2a, in priority order:**
1. Diagnose why `git commit` hangs on this specific repo (try `git commit --no-verify`
   to rule out a hook; check `.git/hooks/` for anything slow; check repo size /
   `.gitignore` coverage of `Library/`, `Temp/`, `Logs/` — if Unity's `Library/` folder
   is accidentally tracked, a commit touching any file can be extremely slow to diff/
   hash). Once diagnosed, commit the staged Tour Mode + hand-transform fix.
2. Re-run `Tools > IP2a > Build Clap-To-Panel Flow` (or whatever the current builder
   menu path is — check `[MenuItem]` attributes in `IP2aSceneBuilder.cs`) to rebuild
   the scene fresh against the current script, confirm Tour Mode objects now exist,
   and resolve the duplicate-VRKeyboard check.
3. Test the real clap gesture and Tour Mode in Play mode and/or on-headset.
4. Pick back up the "buttons don't click" report with the rebuilt scene — may already
   be resolved by step 2 if it was a stale-scene issue; if not, check Interaction
   Layer Masks and whether an XR Interaction Manager is present/referenced.

## Key gotchas learned this session (save yourself the rediscovery time)

- **`GameObject.Find()` cannot locate inactive GameObjects**, and only searches root-
  level objects (use `Transform.Find()` with a slash-separated path for children,
  which works regardless of active state). This is why e.g. `VRKeyboard` (a root
  object) can't safely be deactivated at build time, but child objects like `TourMenu`
  can.
- **This project's XR Origin hierarchy nests `Left Controller`/`Right Controller`
  under `Camera Offset`**, not directly under the rig root — `rig.transform.Find
  ("Left Controller")` silently returns null; you need `rig.transform.Find("Camera
  Offset/Left Controller")`.
- **OpenXR's "automatic loading/running" flags are a separate switch from the feature
  group config** — a project can have Meta Quest Support, controller profiles, and
  hand tracking all correctly ticked under OpenXR's Android feature list, and still
  never actually launch an XR session, because `XRGeneralSettingsPerBuildTarget.asset`
  has loading disabled per-platform. Check both.
- **`git commit` has hung/timed out multiple times on this repo via the remote shell
  tooling**, while `git status`/`add`/`checkout`/`diff` all work fine — not caused by
  a stale `.git/index.lock` (checked and ruled out each time). Root cause undiagnosed;
  worth checking `.gitignore` coverage of Unity's `Library/`/`Temp/` folders early.
- This course is **OpenXR only** — never install the Meta XR All-in-One SDK or use
  Meta's Building Blocks alongside it, the two stacks conflict.

## Communication preferences

Direct, firm, non-sycophantic. No em dashes/double hyphens in anything she'll submit
or send externally (plain prose for chat is fine). For step-by-step UI instructions:
name the item, one-line reason it matters, then explicit numbered steps with exact
menu paths. She wants ELI5-first explanations with real industry/career context
(render pipelines, Editor vs Engine architecture, adjacent tools), not just
assignment-minimum answers — she's headed into game dev/XR professionally.
