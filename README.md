# Horse Mixed-Reality Simulation — Veterinary Training Application

An interactive, passthrough-enabled 3D horse for Meta Quest 3 that reacts believably to student touch and approach patterns. Built for veterinary radiography students at University College Dublin (UCD), combining realistic horse behaviour simulation with mixed-reality spatial awareness to teach safe animal handling.

**GitHub repo status:** Actively developed. See [Known Issues](#known-issues) for current workstreams.

---

## Purpose & context

This application is the front-end of a larger mixed-reality veterinary radiography training system. Students in VR interact with a realistic virtual horse that responds to:
- **Where** they touch (neck, rump, legs, hooves) 
- **How** they approach (gentle sustained petting vs. abrupt contact)
- **Body position** relative to the horse (safe distance vs. unsafe rear approach)

The system logs student positioning data for post-session analysis and supports two interaction modes (Static / Dynamic) to vary pedagogical constraints.

---

## Quick start

### Prerequisites
- **Meta Quest 3** headset with the project APK sideloaded (not launched via Editor Link/Air Link).
- **ADB** (Android Platform Tools) installed on the PC for data retrieval after sessions.
- An open physical space large enough to walk around the virtual horse on all sides (≥3m radius recommended).

### Build & deploy
1. Open the project in **Unity 2022.3.14f1**.
2. Build an Android APK via **Build Settings → Android → Build**.
3. Sideload to Quest 3:
   ```bash
   adb install -r path/to/build.apk
   ```
4. Enable the required experimental feature flag on the headset:
   ```bash
   adb shell setprop debug.oculus.experimentalEnabled 1
   ```
5. Launch from the Quest's **Library / Unknown Sources** menu.

### Running an experiment
See [`EXPERIMENT.md`](./EXPERIMENT.md) for detailed session setup, data collection, and ADB retrieval procedures. The guide covers both Static and Dynamic modes, hand-tracking checks, boundary setup, and video/log archiving.

---

## Engine & tooling

| Component | Version / Details |
|-----------|-------------------|
| **Engine** | Unity 2022.3.14f1 |
| **XR Stack** | Meta XR Interaction SDK (Meta Building Blocks) + custom hand tracking (XR Origin Hands) |
| **Target Hardware** | Meta Quest 3, passthrough-enabled |
| **Language** | C# (all game code); code comments in English |

---

## Core architecture

### Finite-state machine (FSM)
**File:** `HorseFsm.cs`

Central state engine managing the horse's behaviour and emotional state:

| FSM State | Description | Transitions |
|-----------|-------------|-------------|
| `None` | Default idle state; horse is neutral and receptive to first interactions | → `Feeding`, `Anxious`, `Walking` |
| `Feeding` | Triggered by sustained neck/mouth contact; calming state | → `None` |
| `Anxious` | Triggered by non-gentle contact or unsafe approach; horse is defensive | → `None`, `Walking` |
| `Walking` | Triggered by sidestep zones or explicit walk commands | → `None` |

**Nested emotional state** (`Neutral` / `Happy` / `Anxious`) operates independently of FSM state:
- Reflected in ear animations (dedicated `EarsLayer` on Animator)
- Gated by first-touch zone and contact continuity
- **Happy**: first touch on neck/nape → ears up
- **Anxious**: first touch elsewhere, or broken contact chain → ears back
- **Neutral**: no active contact for continuity window → default

**Key responsibilities:**
- Body-touch ref-counting (handles overlapping colliders cleanly via `bodyTouchCount`)
- Per-leg kick targeting logic (`hoofTouched`, `legTouched` with per-leg timeouts)
- Flinch system (distinct from kick; hoof-touch reactions)
- Trust/greeting gating (whether rump/leg touches are considered "safe" for escalation)
- `centerOfGravity` transform reference used by `PlayerPositionLogger` for relative positioning

### Zone detection
**File:** `BodyZoneTrigger.cs`

Collider-based anatomical zone identification. Each zone collider reports which body part was touched:

| Zone | Enum | Colliders | Special Rules |
|------|------|-----------|---------------|
| **Neck** | `BodyZone.Neck` | Nape/neck region | Triggers "Happy" emotional state on first touch |
| **Head** | `BodyZone.Head` | Mouth, forehead | Feeding state trigger; adjacent to Neck for continuity |
| **Body** | `BodyZone.Body` | Torso/flank | Default zone; trust-gated kick path |
| **Rump** | `BodyZone.Rump` | Hindquarters | Trust-gated kick; requires gentle petting history |
| **Legs** (1–4) | `BodyZone.LegFL` / `LegFR` / `LegBL` / `LegBR` | Thighs | Per-leg touch tracking for kick targeting |
| **Hooves** (1–4) | Per-leg (not a zone, tracked separately) | Lower legs | Triggers flinch on first contact |
| **RearApproachLeft / Right** | `RearApproachLeft` / `RearApproachRight` | Zones behind the horse | Body-position-based (not hand contact); immediate, trust-bypassing kick |

**Zone adjacency** supports continuous petting chains:
```
Head ↔ Neck ↔ Body ↔ Rump
       └─────────────┘
```
Continuous movement between adjacent zones preserves emotional state/trust logic without re-gating.

### Player body tracking
**File:** `PlayerBodyTracker.cs`

Kinematic physics proxy that mirrors the player's body (not hands) position:
- Follows player head anchor X/Z position (room-scale)
- Y locked to a configurable body height (prevents head tilt from bobbing the collider)
- Tagged `VRPlayer` (distinct from `VRHand`)
- Used exclusively by rear-approach zones to detect unsafe positioning independent of hand contact

### Player-position logging
**File:** `PlayerPositionLogger.cs`

Continuous position sampler for post-session analysis:
- **Metric:** Player head position relative to horse's `centerOfGravity`
- **Frequency:** Configurable sampling rate (default 0.5s intervals)
- **Output:** CSV file (`player_position_log_YYYYMMDD_HHMMSS.csv`) in `Application.persistentDataPath`
- **Lifecycle:** Starts on scene load; flushes regularly to disk (crash-safe); closes cleanly on app quit
- **Retrieval:**
  ```bash
  adb pull /sdcard/Android/data/<package-name>/files/player_position_log_*.csv
  ```

### Debug & control
**File:** `HorseFsmDebugController.cs` *(formerly `HorseController.cs`)*

In-app debug panel for manual FSM state triggering during testing. Not part of runtime logic; disabled in production builds.

---

## Interaction model

### Touch-based reactions

#### Kick (trust-gated)
- **Trigger:** Abrupt contact on rump or legs *without* a history of gentle, continuous petting
- **Safety:** Trust is earned by petting continuously in the zone; breaking contact resets trust
- **Animation:** Per-leg kick (FL, FR, BL, BR) with brief repeat-kick lockout
- **Related systems:** `RumpTouchIsTrusted` check; `legTouched`/`hoofTouched` per-leg timeouts

#### Kick (rear-approach)
- **Trigger:** Player body enters zone directly behind the horse (left or right side)
- **Safety:** Immediate, trust-bypassing — standing behind a horse is taught as inherently unsafe
- **Detection:** `PlayerBodyTracker` + `VRPlayer` tag (body position, not hand contact)
- **Implementation:** `TriggerImmediateKick(int forcedLeg)` with explicit leg forcing

#### Flinch
- **Trigger:** First contact on a hoof
- **Duration:** Short animation; auto-resets a few seconds after contact stops
- **Distinct from kick:** Does not necessarily escalate into one

#### Petting & emotional response
- **Neck/nape (first touch)** → Happy (ears up)
- **Anywhere else (first touch)** → Anxious (ears back)
- **Continuous adjacent-zone petting** → State persists (e.g. Neck → Body → Neck stays Happy)
- **No contact** for continuity window → Neutral (ears return to default)

#### Sidestep
- **Trigger:** Touch behind the ear
- **Effect:** Horse shifts laterally; independent of FSM state
- **Implementation:** `SideStepController.cs` with zone-adjacency gating

#### Leg lift (GrabLeg)
- **Availability:**
  - **Static mode** — always available
  - **Dynamic mode** — requires prior per-leg petting confirmation ("qualifying" that leg first)
- **Implementation:** `LegLiftInteractable.cs` gates on interaction mode and per-leg touch history

### Application modes
The wrist-mounted toggle button (color-coded) switches between modes **live, in-session**:

| Mode | Button | Behaviour |
|------|--------|-----------|
| **Static** | 🟢 Green | All interactions always available; no prerequisite petting needed |
| **Dynamic** | 🟠 Orange | Leg lift requires per-leg petting confirmation; teaches sequenced approach |

Mode is **saved automatically** and persists across app restarts.

---

## Script reference

### Core FSM & state management
- `HorseFsm.cs` — Central FSM, emotional state, kick/flinch gating, body-touch ref-counting
- `HorseFsmDebugController.cs` — Debug panel for manual state triggering

### Interaction triggers & touch detection
- `BodyZoneTrigger.cs` — Zone detection dispatcher; reports to `HorseFsm.NotifyZoneEnter()`
- `TouchTrigger.cs` — Basic touch detection
- `HeadPetDetection.cs`, `HeadTriggerScript.cs` — Head/mouth-specific petting
- `MouthTrigger.cs` — Mouth zone (Feeding state)
- `BehindTrigger.cs` — Rump/behind-body touch (trust-gated kick path)
- `BehindEarTrigger.cs` — Behind-ear sidestep trigger
- `LegTouchDetector.cs` — Per-leg touch tracking

### Animation & deformation
- `HorseIdleController.cs` — Idle animation sequencing (alternates `idle1_Baked`/`idle2_Baked` with configurable repeat counts)
- `HoofRotateLimit.cs` — Hoof rotation limits
- `LegMover.cs` — Leg IK/animation blending
- `ChangeLegRigWeight.cs` — Rig weight transitions (kick state)

### Locomotion & positioning
- `HorseTargetFollow.cs` — Horse follows player/target
- `TriggerHorseWalk.cs` — Walk state triggers
- `SideStepController.cs` — Sidestep logic
- `RootMotionScaler.cs` — Root motion animation scaling
- `TransformSync.cs` — Transform synchronization

### Tracking & awareness
- `HandTracking.cs` — Hand pose tracking (XR Interaction SDK integration)
- `PlayerBodyTracker.cs` — Body-position physics proxy for rear-approach detection
- `HorseAware.cs` — Horse awareness state and reactions
- `KickDisplay.cs` — Visual kick feedback

### Data collection
- `PlayerPositionLogger.cs` — Head-position CSV logging at configurable intervals

### Tutorial & UI
- `TutorialManager.cs` — Tutorial flow orchestration
- `TutorialStep.cs` — Individual tutorial steps
- `PokeButton.cs`, `PokeRotate.cs` — Interactive UI widgets
- `WristModeButton.cs` — Static/Dynamic mode toggle
- `LegLiftInteractable.cs` — Leg-lift interaction gating

---

## Idle animation system

**File:** `HorseIdleController.cs`

The horse alternates between two idle clips to improve realism:
- `idle1_Baked` (320 frames @ 30fps ≈ 10.7s)
- `idle2_Baked` (240 frames @ 30fps ≈ 8.0s)

`idle1` repeats a configurable number of times (`idle1Repeats`, default 3) before switching to `idle2` once; then cycles back. This produces subtle head movement, tail sway, and posture shifts without feeling repetitive.

**Current work:** Diagnostic testing on repeat-count tuning to optimize perceived frequency and realism. See [Known Issues](#known-issues).

---

## Known issues & active workstreams

### 1. Hand mesh overlay in passthrough mode
**Status:** Root cause confirmed; fix pending.

`OVRMeshRenderer` re-enables the synthetic hand mesh every frame based on tracking confidence, overriding manual disabling. This produces a gray overlay over real hands in passthrough, which is cosmetic only but visually distracting on recordings.

**Workaround:** Disable `OVRMeshRenderer` itself (not `SkinnedMeshRenderer`) while leaving the tracking skeleton (`OVRSkeleton`/`OVRHand`/`OVRMesh`) intact for collisions and interactions.

**Related:** GitHub issue with video evidence and updated acceptance criteria.

### 2. Idle animation frequency tuning
**Status:** In progress; diagnostic testing phase.

Perceived idle animation frequency may stem from clip length, static-pose ratio, or long same-clip streaks rather than repetition logic. Current diagnostic: testing `idle1RepeatRange = (1,1)` to isolate the root cause.

**Related:** `HorseIdleController.cs`, `HorseFsm.cs` (collateral sidestep exit condition bug flagged).

### 3. Hand-tracking stability around wrist button
**Status:** Observed; low priority.

The wrist-mounted mode toggle button is linked to occasional hand-tracking degradation mid-session. Monitor hand-tracking stability when toggling modes during testing.

---

## Advanced features

### Procedural animation layering (future work)
If procedural head motion is added later, use an **Additive Animator layer** to layer procedural motion on top of baked animations without overwriting them. A Perlin-noise `MultiAimConstraint` approach would break this.

### Passthrough occlusion in MR contexts
In mixed-reality passthrough, improving synthetic mesh shading is less important than removing mesh rendering entirely — real hands are already visible. The correct approach: preserve the tracking skeleton for collisions/interactions while disabling visual rendering.

---

## Experimental setup & data collection

For detailed instructions on conducting a testing session, collecting player-position logs, and archiving results, see [`EXPERIMENT.md`](./EXPERIMENT.md).

Key points:
- Experiments run twice per participant (Static mode, then Dynamic mode)
- Each run generates one video and one CSV position log
- Data is pulled off-device via ADB after the session
- Session folder naming convention: `{participant_id}_{date}/`

---

## Contributing

- **Code style:** Follow existing C# conventions. All code comments in English.
- **Scene changes:** Use prefabs for reusable components (e.g., `MainHorse.prefab`).
- **FSM extensions:** Add new states to the `HorseStates` enum; document transitions.
- **New interaction zones:** Add to `BodyZone` enum; update `ZoneAdjacency` graph in `BodyZoneTrigger.cs`.
- **Testing:** Use `HorseFsmDebugController` panel for manual state triggering; refer to `EXPERIMENT.md` for session procedures.

---

## References

- **Meta XR Interaction SDK:** https://developer.oculus.com/documentation/unity/
- **Passthrough mixed reality:** Meta Quest 3 passthrough features and spatial anchoring
- **Veterinary training context:** University College Dublin (UCD) veterinary radiography curriculum