# Horse VR Safety Trainer — Experiment Guide

This guide explains how to set up, run, and close a testing session with the horse VR safety trainer. It is written for anyone who needs to reproduce an experiment on this project — including future developers who did not work on the original build.

---

## 1. Prerequisites

### Hardware
- **Meta Quest 3** headset, with the project's Android build (APK) already sideloaded.
- **PC** connected to the headset via **ADB** (Android Debug Bridge), used after the session to retrieve recorded data over the command line.

### Software
- The project's **APK build**, installed on the headset **before testing begins** — not launched from the Unity Editor via Link/Air Link. This avoids editor/connection overhead affecting the session and matches how the app will actually be used in the field.
- **ADB** (Android Platform Tools) installed on the PC, used for pulling files from the headset after the session (see [§6](#6-ending-the-experience)).

### Location
- An open space **large enough for the participant to walk freely all the way around the virtual horse** — front, both sides, and behind it.
- This isn't just comfort: the horse's rear-approach kick zones (see [§5](#5-running-the-experience)) trigger based on the participant's **body position**, not hand contact, so the play space needs enough room for the participant to naturally approach from any side without being physically boxed in.

---

## 2. Setting up the experience

### 2.1 Launching the application
The app is installed as a standalone application and appears directly in the **Quest's main Meta Horizon menu** (under Library / Unknown Sources, since it's a sideloaded build). No PC connection or Editor is required to launch it — the headset runs independently once installed.

### 2.2 Preparing internal video recording
The project does not use a custom in-app recorder — internal capture relies on the **Quest 3's native screen recording**. Set this up before launching the app so recording can be started immediately once the session begins (see [§3](#3-starting-the-experience)).

### 2.3 Creating and saving the play space (boundary)
Configure the headset's boundary before starting:
1. Put on the headset and follow the system boundary setup flow.
2. Draw a boundary matching the space requirement from [§1](#1-prerequisites) — enough room to walk fully around the horse.
3. Save the boundary.

If the same physical space is reused across sessions, the saved boundary can be reused directly instead of being redrawn each time — just double-check it's still accurate for the room before starting.

### 2.4 Hand-tracking check
Before starting a session, confirm hand tracking is working correctly:
- Enable hand tracking in the Quest system settings if not already active.
- Check, at the system level (before entering the app), that both hands are tracked cleanly and consistently under the room's current lighting.

> ⚠️ See [§7 Remarks](#7-remarks) — this project has a known issue where a UI element can cause hand-tracking to degrade mid-session. Worth keeping in mind if tracking looks unstable partway through.

---

## 3. Starting the experience

Follow this order:

1. **Start internal video recording** — Quest 3 native capture shortcut, started before entering the app.
2. **Start the application** — launch from the Library.
3. **Start external video recording** — a person outside the headset films the session with a camera, once the participant is ready to begin.

---

## 4. Running the experience

### 4.1 Interactions with the horse and environment

Each interaction is explained below by **what triggers it** and **what it looks like in-session**, so an observer can recognize what's happening without reading code.

#### Kick
There are **two independent trigger paths**:

- **Trust-gated kick (rump / leg touch)** — fires when the horse's rump or a leg is touched **without** an established history of continuous, gentle petting beforehand. If the participant has been petting continuously in that area, this path stays quiet; break contact and touch again abruptly, and it can fire.
- **Immediate rear-approach kick** — fires purely from the **participant's body position**, not hand contact: walking into the zone directly behind the horse (left or right side) triggers it immediately, regardless of touch history. This path deliberately bypasses all trust checks, since standing directly behind the horse is treated as an unsafe position in itself.

Both paths play a per-leg kick animation (front-left/right or back-left/right, whichever leg is relevant) and briefly lock out repeat kicks right after.

#### Flinch
A smaller, separate reaction from the kick: triggered by the **first contact on a hoof**. Plays a short flinch animation and resets automatically a few seconds after contact stops. Distinct from the kick — a flinch does not necessarily escalate into one.

#### Ears (emotional state)
The horse's ears reflect an underlying emotional state — **Neutral**, **Happy**, or **Anxious**:

- **First touch on the neck/nape** → Happy (ears up).
- **First touch anywhere else**, or a broken contact chain → Anxious (ears back).
- **Continuous petting** through adjacent body zones (e.g. neck → body → neck) keeps whichever state was set at first contact — it doesn't re-evaluate mid-stroke.
- **No active contact** for a short window → the horse settles back to Neutral.

This is separate from the sidestep reaction (touching behind the ear), which is its own, independent trigger.

#### GrabLeg (leg lift)
Whether a leg can be lifted depends on the current **interaction mode** (see [4.2](#42-application-modes-static--dynamic)):

- **Static mode** — leg lift is always available, no prerequisite.
- **Dynamic mode** — the participant must first **pet continuously near that specific leg** to "confirm" it before the lift interaction unlocks for that leg. This is per-leg: confirming one leg doesn't unlock the others.

### 4.2 Application modes (Static / Dynamic)
The mode is switched **live, in-headset**, via a single wrist-mounted toggle button — no menu needed:
- **Green** = Static
- **Orange** = Dynamic
- One tap toggles between the two.

The chosen mode is **saved automatically** and persists across app restarts. This means a new session does **not** necessarily start in the mode you expect — check the button's color right after launch and toggle if needed before beginning.

> ⚠️ See [§7 Remarks](#7-remarks) — this wrist button is the same UI element linked to a hand-tracking degradation issue observed in this project. Keep an eye on hand-tracking stability, particularly around the wrist, during sessions.

### 4.3 Player position logging
Runs automatically in the background from the moment the app starts — no manual action required:
- Samples the participant's head position every **0.5 seconds**, relative to the horse's center of gravity.
- Written live to a CSV file as the session runs (regular flush to disk, so a crash won't lose the whole session).
- Closed cleanly when the app quits.

---

## 5. Running the experience — quick reference

*(See [§4](#4-running-the-experience) above for full detail; this section is just the interaction/mode summary in one place for quick lookup during a session.)*

| Interaction | Trigger | Notes |
|---|---|---|
| Kick (trust-gated) | Rump/leg touch without prior continuous petting | Can be avoided with gentle continuous contact |
| Kick (rear-approach) | Participant's body enters rear zone (L/R) | Immediate, bypasses trust checks |
| Flinch | First hoof contact | Short, resets automatically |
| Ears — Happy | First touch on neck | Persists through continuous adjacent-zone petting |
| Ears — Anxious | First touch elsewhere, or broken contact chain | Same persistence rule |
| GrabLeg | Leg-lift attempt | Static: always allowed. Dynamic: requires per-leg petting confirmation first |

---
 
## 6. Ending the experience
 
1. **Stop the application** — quit through the app's own menu / system Meta button.
2. **Stop both video recordings** — internal Quest capture, then the external camera.
3. **Retrieve the player-position CSV** via ADB, command line, with the headset connected to the PC (USB or Wi-Fi ADB):
```powershell
   adb pull /sdcard/Android/data/<package_name>/files/player_position_log_YYYYMMDD_HHMMSS.csv
```
 
   Replace `<package_name>` with this project's Android package identifier. Here it's `com.UnityTechnologies.com.unity.template.urpblank` If you don't have it handy:
 
```powershell
   adb shell pm list packages | Select-String <keyword>
```
 
   > The exact filename includes the session's start timestamp (`player_position_log_YYYYMMDD_HHMMSS.csv`) — list the files in that folder if unsure which one is the latest:
   > ```powershell
   > adb shell ls /sdcard/Android/data/<package_name>/files/
   > ```
   >In case you want to empty the folder of the package files: 
   >```powershell
   >adb shell rm /sdcard/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/player_position_log_*.csv
   >```
 
4. **Archive the video and CSV together** for that session — group them under a consistent folder name per session, e.g. `{participant_id}_{date}/`, containing both video files and the CSV.
---

## 7. Remarks

- **Known issue — hand mesh overlay:** `OVRMeshRenderer` can re-enable the synthetic hand mesh every frame, causing a gray overlay over the passthrough hands. This is cosmetic only and does not affect tracking or interactions, but can look alarming on recordings if not anticipated.