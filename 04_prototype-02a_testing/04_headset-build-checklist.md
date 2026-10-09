# IP2a headset checklist (Quest 2, Friday class)

First real device build of IP2a. Work top to bottom. Tick each box, and note anything that fails or feels off in the "Notes" lines; that list is what we fix next.

## Before class (at home, ~5 min)

- [ ] Commit or note your current work, so you know exactly what build you tested.
- [ ] Bring a **data** USB-C cable (a charge-only cable won't show the headset to Unity).
- [ ] Charge the headset and both controllers.

## 1. Connect the headset (~5 min)

- [ ] Plug the Quest into the laptop. **Put the headset on** and accept **"Allow USB debugging"** (tick "Always allow from this computer").
- [ ] Optional cleanup: in the headset, **Settings > Apps > Unknown Sources**, uninstall the old app with the template name (`com.UnityTechnologies.com.unity.template.urpblank`), so you can't open the old broken build by mistake. The new app is called **VR Whiteboard (IP2b)**.

## 2. Build And Run (~10-20 min the first time)

1. Open the project in Unity and open `IP2a` (Assets/Scenes).
2. **File > Build Profiles**, select **Android** on the left.
3. **Run Device**: pick your Quest 2 from the dropdown. If it isn't listed, click Refresh; if still missing, replug and re-accept USB debugging in the headset.
4. Tick **Development Build**. This keeps the test buttons ("Create Test Note") visible on the headset. Untick it later for participant sessions.
5. Click **Build And Run**. Save the APK **outside the repo**, e.g. `N:\GitHub Desktop\Builds\IP2a.apk`.
6. Put the headset on. The app opens by itself when the build finishes.

The first build is slow (IL2CPP compiles everything); later builds are faster.

## 3. Does it launch? (the original bug)

- [ ] The app gets past the three loading dots into the scene.
- [ ] You see your controllers and the "Clap to create a note" prompt in front of you.
- [ ] Your eye height feels like your real height (not on the floor, not floating). If wrong, re-run the Guardian/floor setup in the headset and relaunch.

Notes: ______________________________________________

If it hangs on the dots: don't keep rebuilding. Capture the log (section 6) and send it to Claude.

## 4. Feature checks (controls: **grip** = grab, **trigger** = press buttons, or touch buttons with the controller tip)

**Clap → panel**
- [ ] Clap the two controllers together: the panel opens. Try ~10 claps: how many work? ___/10
- [ ] It doesn't open by accident while just moving your hands around.

**Panel**
- [ ] Point and pull trigger on a shape: it glows and the text field changes colour.
- [ ] Trigger on the text field: the keyboard opens. Type, press Confirm: the text shows in the field.
- [ ] Pick Rectangle, then Add Image: the media buttons only work on Rectangle.
- [ ] Create: the note appears in front of you, facing you, with your shape and text. Panel closes.
- [ ] Discard closes the panel; clapping again shows a blank panel.
- [ ] The mic: grip it and pull it toward/away from the panel; it stays on its arm.
- [ ] Everything is comfortable to read and reach (not too close, far, high or low).

**Notes**
- [ ] Grip a note from a distance with the ray: it moves with the ray and stays where you let go.
- [ ] Grip a note's small dark **handle**, drag to another note, let go: a line connects them. (You need to bring the controller to the other note.)
- [ ] Trigger on the green **+** on the line: keyboard opens; type and Confirm: the label shows. Trigger on the label to edit it.
- [ ] Grip a note: a red square appears at your feet. Lower the note onto it and let go: the note, its line and label disappear. Is reaching down that far OK?

**Tour Mode**
- [ ] Turn your **left** wrist to look at the controller like checking a watch: the Tour menu appears. Is it easy to trigger? Does it pop up when you don't want it?
- [ ] Trigger on a note in the list: you jump in front of that note, facing it.
- [ ] The red **Exit Tour** button on your left wrist brings you back.

Notes: ______________________________________________

## 5. Comfort

- [ ] Any discomfort, dizziness or nausea (especially when Tour jumps you)?
- [ ] Frame rate feels smooth (no stutter when the panel or keyboard opens)?

Notes: ______________________________________________

## 6. If something breaks: capture the log

Either:
- **Meta Quest Developer Hub (MQDH) > Device Manager > Logs**: start logging, reproduce the problem, save the log, or
- In a terminal on the laptop (headset plugged in):
  ```
  "N:\unity intalls\6000.3.24f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" logcat -s Unity
  ```
  Reproduce the problem, then copy the output.

Send Claude the log plus one line on what you did when it broke.
