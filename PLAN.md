# Repo City VR — Next Session Plan

Written because we're about to hit a rate limit mid-session. This is meant to be handed to
5-10 parallel agents at once next time. Read the "Context for every agent" section first,
then pick tasks — each task below is written to be self-contained (own frozen contracts,
own file list) so agents don't need to coordinate with each other, only with the orchestrator
(you, reading this) who does integration + rebuild + test afterward, same pattern as every
prior pass in this project.

## Context for every agent

- Unity 6000.6.5f1, project at `c:\Users\wilen\Desktop\VR\repo-city-unity-project`. Built
  ENTIRELY via CLI/batchmode automation — no one has ever opened the Unity Editor GUI. All
  gameplay code is under `Assets/Scripts/RepoCity/*.cs`, namespace `RepoCity`.
- UI is legacy UGUI only (`com.unity.ugui` 3.0.0-exp.4: Canvas/Text/Image/Button/InputField).
  TextMeshPro is NOT installed — don't reference it.
- Input: `UnityEngine.XR` device polling for VR controllers, WASD+mouse-drag-look desktop
  fallback, both in `Locomotion.cs`.
- **Two Unity gotchas have each caused a real, screenshot-confirmed bug here already:**
  1. A world-space `Canvas` or legacy `TextMesh` reads correctly only from its **local -Z**
     side. If you aim it with `transform.LookAt(target)`, that points local +Z at the
     target, so it renders mirrored unless you immediately follow with
     `transform.Rotate(0f, 180f, 0f)`. Reference-correct example:
     `CityBuilder.cs` → `BuildDistrictLabel`.
  2. `Transform.position`/`.rotation` **setters are always world-space**, regardless of
     parent. A prefab instance only inherits a parent's *later* movement if it was
     instantiated while that parent was still at the identity transform. Reference-correct
     example: `WorldBuilder.cs` → `BuildAll` (builds each island at identity, moves the
     island root to `worldCenter` only at the very end).
- Write plain terse C#, no XML doc comments, matching existing style. Only
  `UnityEngine`/`UnityEngine.UI`/`System`/`System.Collections` namespaces are available —
  no new packages.

## How to build & test (no VR headset needed)

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
PROJ="/c/Users/wilen/Desktop/VR/repo-city-unity-project"
# Compile-check / run an editor method without a full build:
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" -logFile "/c/Users/wilen/Desktop/VR/compilecheck.log"
grep -n "error CS" "/c/Users/wilen/Desktop/VR/compilecheck.log"   # empty output = clean compile

# Full standalone build:
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
  -buildWindows64Player "/c/Users/wilen/Desktop/VR/repo-city-build/RepoCityVR.exe" \
  -logFile "/c/Users/wilen/Desktop/VR/buildN.log"
```

Then launch `RepoCityVR.exe` with `-logFile <path>` (background), `grep -niE
"exception|error"` the log (ignore the benign `XR_ERROR_INITIALIZATION_FAILED` / no-headset
noise), and screenshot the live window via Win32 (`GetWindowRect` + `CopyFromScreen`) — a
working helper script was at
`...\scratchpad\screenshot.ps1` (session-local temp dir, probably gone — just rewrite it,
it's ~20 lines, see git history of this conversation if available, or recreate: get the
process's `MainWindowHandle`, `GetWindowRect`, `Graphics.CopyFromScreen`, save PNG).

**Known environment issue:** `SendInput` (Win32) for simulating WASD/mouse-drag to drive the
desktop camera around for testing **stopped actually moving the OS cursor partway through
the last session**, despite the API call itself reporting success — looks like a sandboxing
change in this tool environment, not a game bug. Don't sink time re-debugging it; if it's
still broken, screenshot-at-spawn + log inspection is the fallback verification method.

## Current state (as of commit `d4beded`)

Done and working (screenshot/log-confirmed): a `RepoWorld` scene (now the startup scene,
index 0; the older single-repo `RepoCity` scene is still there at index 1) that builds 3
"islands" side by side (your flagship repo + the earlier Three.js and Unity-staging repo
snapshots), each a full city (buildings/roads/traffic/vegetation/NPCs), joined by connector
roads + portal signs, plus a "Programming Central" plaza with 6 sandboxed coding-challenge
kiosks (custom mini-language interpreter in `MiniLang.cs`). Density/lighting/color pass
landed (sun + fog added — the scene previously had **zero light sources**, which was the
main cause of the "dull" look). Audio system (`AudioManager.cs`, CC0 Kenney clips in
`Assets/Resources/RepoCity/Audio/`), footsteps, teleport whoosh, kiosk pass/fail sounds,
NPC-chat/narration blips, a day-night cycle (`DayNightCycle.cs`), and a milestone/achievement
tracker (`AchievementTracker.cs`, fires an `OnToast` event) are all wired in.

**NOT yet verified this compiles** — the rate limit hit right after writing
`DayNightCycle.cs`/`AchievementTracker.cs` to unblock references that `WorldBootstrapper.cs`
and `CodeKiosk.cs` already make to them. A `grep -rn` pass confirmed no code references any
*other* not-yet-built type (`MiniMapUI`, `Landmarks.`, `AchievementToastUI` all appear 0
times outside one comment), so it SHOULD compile, but **Task 0 below must confirm this
before anything else starts**, since several tasks below build on top of a working build.

A background code-review workflow (run ID `wf_4293e93b-70b`, 5 reviewers + adversarial
verify over every existing script) was started but may not have finished before the session
ended — check
`C:\Users\wilen\.claude\projects\c--Users-wilen-Desktop-VR\b65c0922-542c-4a09-ba52-32ae5000aa00\subagents\workflows\wf_4293e93b-70b\journal.jsonl`
for its `{"type":"result",...}` lines before re-running it; if it has results, fold its
confirmed findings into Task 0.

## Existing APIs you'll want (don't re-derive, don't rename)

- `FileEntry` (`RepoData.cs`): `path, dir, name, ext, size (long), commits (int),
  lastModified (string, ISO date or null)`.
- `BuildingInfo : MonoBehaviour` (`CityBuilder.cs`): `public FileEntry file;` — on every
  building GameObject.
- `NpcInfo : MonoBehaviour` (`NpcController.cs`): `public string personaName, personaJob;`
- `CityLayoutResult` (`CityLayout.cs`): `.districts, .buildings (List<CityBuilding>),
  .bounds (CityBounds{minX,maxX,minZ,maxZ})`.
- `WorldIslandConfig`/`WorldIslandResult` (`WorldConfig.cs`/`WorldBuilder.cs`):
  `WorldIslandResult` has `config (.label), root (GameObject), layout (CityLayoutResult),
  worldCenter (Vector3), carLoopWaypoints/sidewalkLoopWaypoints (List<Vector3>, WORLD
  space)`. `WorldBuilder.BuildAll(Transform parent)` returns `List<WorldIslandResult>`,
  first entry is always the "home" island pinned at world origin.
- `RoadNetwork.BuildConnector(Vector3 from, Vector3 to, Transform parent)` — lays a straight
  line of road tiles between two axis-aligned points, returns waypoints.
- `AudioManager` (static): `EnsureInitialized()`, `PlayUi(string clipName, float volume=1)`
  (non-positional), `PlayAt(string clipName, Vector3 pos, float volume=1)` (positional),
  `PlayMusic(string clipName, bool loop=true)`, `StopMusic()`,
  `SetMasterVolume/SetMusicVolume/SetSfxVolume(float)` (0-1, persisted via PlayerPrefs),
  read current values via `AudioManager.MasterVolume/MusicVolume/SfxVolume`. Clips load from
  `Resources.Load<AudioClip>("RepoCity/Audio/" + clipName)` — existing clip names (no
  extension): `ui_click, ui_hover, ui_back, ui_confirm, test_pass, test_fail,
  challenge_complete, npc_blip, teleport, menu_jingle, achievement, footstep_00..footstep_04`.
- `GameSettings` (static): `EnsureLoaded()`, `MouseSensitivity/QualityLevel/Fullscreen`
  (read-only properties), `SetMouseSensitivity/SetQualityLevel(int)/SetFullscreen(bool)`,
  all persisted via PlayerPrefs.
- `AchievementTracker` (static): `event Action<string> OnToast;` plus
  `NotifyFileInspected/NotifyNpcTalkedTo/NotifyChallengeCompleted/NotifyIslandEntered(string)`
  — all already called from `WorldBootstrapper.cs`/`CodeKiosk.cs`. **No UI consumer exists
  yet** — that's Task 3 below.
- `DayNightCycle : MonoBehaviour` — already attached once in `WorldBootstrapper.Start()`.
  Finds the sun via `GameObject.Find("Sun")` (created by `WorldLighting.Setup()`).
- Audio source packs already downloaded + extracted this session (CC0, Kenney.nl) — if the
  session temp dir is gone, re-derive download URLs with:
  `curl -sL "https://kenney.nl/assets/<slug>" | grep -oE "https://kenney\.nl/media/pages/assets/[^']*\.zip"`
  (slugs used so far: `interface-sounds, ui-audio, digital-audio, impact-sounds,
  music-jingles`; `rpg-audio` was also downloaded but barely used — check it for more
  footstep/ambient material before re-downloading).

## Task 0 — MUST run first, sequential, before anything else touches the project

1. Run the compile-check command above. If `error CS` shows up, read the errors and fix them
   (small/targeted fixes — this is maintenance, not a redesign).
2. Check the audit workflow journal (path above). If it has results, triage: fix anything
   confirmed+high-severity yourself right now (small, targeted); for anything confirmed but
   larger, turn it into an extra task below before firing the parallel batch.
3. Commit the fix (if any) by itself before the parallel batch starts, so later tasks branch
   from a known-good compiling state.

## Tasks 1-8 — independent, can run as 5-10 parallel agents

Each should: read the files it needs, write only the files it's told to write, and NOT touch
any file another task owns (listed under "owns" below). The orchestrator wires everything
together afterward (new types get instantiated from `WorldBootstrapper.cs`, which only the
orchestrator should edit, to avoid merge conflicts across agents).

### Task 1 — Landmarks.cs
Owns: `Assets/Scripts/RepoCity/Landmarks.cs` (new).
```
namespace RepoCity {
    public static class Landmarks {
        public static void Highlight(GameObject islandRoot) { /* ... */ }
    }
}
```
Per island (called once after `WorldBuilder.BuildAll` returns, by the orchestrator), find via
`islandRoot.GetComponentsInChildren<BuildingInfo>()`: the building with highest
`file.commits` ("most active file") and the one with earliest parseable `file.lastModified`
(ISO date string, may be null — skip the "oldest" beacon entirely if none parse). Add a thin
glowing vertical beacon (scaled `CreatePrimitive(Cylinder)`, no Collider — must not block the
existing inspect-raycast) as a CHILD of that building's own Transform (inherits its world
position for free — don't compute world positions), gold for most-active, cyan for oldest,
plus a legacy `TextMesh` label above it using the LookAt+180 pattern (local-space target
offset only, e.g. `transform.position + Vector3.back * 10f` since it's parented to the
building). Keep it thin so it doesn't swallow the building visually.

### Task 2 — MiniMapUI.cs
Owns: `Assets/Scripts/RepoCity/MiniMapUI.cs` (new).
```
namespace RepoCity {
    public class MiniMapUI : MonoBehaviour {
        public void Initialize(List<WorldIslandResult> islands, Transform playerRig) { /* ... */ }
    }
}
```
Builds a small schematic top-down map as a world-space Canvas child of `transform` (reuse
`HudPanel.cs`'s Canvas/CanvasScaler/GraphicRaycaster pattern; the orchestrator parents and
positions `transform` itself as a camera-corner HUD element — don't move/rotate `transform`
beyond its own children). Compute one combined world bounding rect across every island
(`worldCenter + layout.bounds`, plus a margin), draw one small Image rectangle per island
sized/positioned via linear mapping into UI space, a text label per island (`config.label`),
and one bright player-dot Image repositioned every `Update()` by mapping `playerRig.position`
through the same stored mapping (cache everything from `Initialize` in fields — `Update`
must do zero `GetComponentsInChildren`/`Resources.Load`/recomputation). Guard the
divide-by-zero case of a near-zero combined rect (single tiny island) with a fallback scale.

### Task 3 — AchievementToastUI.cs
Owns: `Assets/Scripts/RepoCity/AchievementToastUI.cs` (new). `AchievementTracker.cs` already
exists and fires `OnToast` — don't modify it, just subscribe.
```
namespace RepoCity {
    public class AchievementToastUI : MonoBehaviour { /* no public API needed */ }
}
```
`gameObject.AddComponent<AchievementToastUi>()` will be called by the orchestrator on an
already-positioned child of the player camera — don't move/rotate/scale `transform` itself,
only add children under it. Build a small world-space Canvas (same HudPanel.cs-derived
pattern) with a background Image + one Text, hidden initially. Subscribe to
`AchievementTracker.OnToast` in `OnEnable` (store the delegate so `OnDisable` can
unsubscribe the SAME instance, not a second separately-allocated lambda), show the message
for ~3 seconds via a restartable coroutine (a new toast while one is showing just restarts
the timer, doesn't queue). On showing a toast, also call
`AudioManager.PlayUi("achievement")` once.

### Task 4 — Main Menu scene + flow
Owns: `Assets/Scripts/RepoCity/MainMenu.cs` (new), `Assets/Editor/SetupScene.cs` (add a
`[MenuItem("Repo City/Create Main Menu Scene")] CreateMainMenu()` method alongside the
existing `Create`/`CreateWorld` methods — don't remove those), build-settings scene order.
Goal: a `MainMenu` scene becomes the new startup scene (index 0), `RepoWorld` moves to index
1 (loaded on Play), `RepoCity` (single-repo) stays after that. `MainMenu.cs` is a
MonoBehaviour on one `Bootstrapper` GameObject (same pattern as `SceneBootstrapper.cs`):
calls `AudioManager.EnsureInitialized(); GameSettings.EnsureLoaded();
AudioManager.PlayMusic("menu_jingle", loop:false);`, sets up a camera + a WORLD-SPACE Canvas
(**not screen-space-overlay** — overlay canvases don't render inside a VR headset, so the
menu must use the same world-space pattern as every other UI in this project) a few meters
in front of the camera, with a title ("REPO CITY") and three buttons: **Play** (loads
`RepoWorld` via `UnityEngine.SceneManagement.SceneManager.LoadScene("RepoWorld")`),
**Settings** (shows a settings panel — see Task 5, reuse its component, don't reimplement
sliders here), **Quit** (`Application.Quit()`). Needs its own `EventSystem` +
`StandaloneInputModule` (check one doesn't already exist via `FindObjectOfType` first) for
desktop mouse-click to work, same gap noted in Task 7 for VR. Play a `ui_click` via
`AudioManager.PlayUi` on every button press.

### Task 5 — SettingsPanel.cs
Owns: `Assets/Scripts/RepoCity/SettingsPanel.cs` (new). Must be reusable from BOTH the main
menu (Task 4) and the in-game pause menu (Task 6) without those two owning conflicting
copies — build it as one MonoBehaviour component that either of them can
`gameObject.AddComponent<SettingsPanel>()` + call `Initialize(Action onClose)` on, so it
works standalone.
```
namespace RepoCity {
    public class SettingsPanel : MonoBehaviour {
        public void Initialize(System.Action onClose) { /* ... */ }
    }
}
```
World-space Canvas (HudPanel.cs-derived pattern) with: 3 sliders (Master/Music/SFX volume,
0-1, initial value from `AudioManager.MasterVolume/MusicVolume/SfxVolume`, `onValueChanged`
calling `AudioManager.SetMasterVolume` etc. live), 1 slider for mouse sensitivity (0.1-5,
from/to `GameSettings.MouseSensitivity` via `SetMouseSensitivity`), a quality dropdown
(options from `QualitySettings.names`, current index `QualitySettings.GetQualityLevel()`,
`onValueChanged` calling `GameSettings.SetQualityLevel`), a fullscreen toggle (from/to
`GameSettings.Fullscreen` via `SetFullscreen`), and a "Back" button calling the `onClose`
callback (don't destroy `gameObject` yourself — let the caller decide, just hide the canvas
or invoke the callback and let the caller handle disposal). Legacy UGUI `Slider`/`Dropdown`/
`Toggle` components — check actual available UGUI version supports `Dropdown` (the older
one, not `TMP_Dropdown` since TextMeshPro isn't installed). Play `ui_click`/`ui_back` via
`AudioManager.PlayUi` on button/back presses.

### Task 6 — PauseMenu.cs
Owns: `Assets/Scripts/RepoCity/PauseMenu.cs` (new). Depends on Task 5's `SettingsPanel`
frozen contract above (code against it, don't wait for it to literally exist).
```
namespace RepoCity {
    public class PauseMenu : MonoBehaviour { /* no public API needed beyond being addable */ }
}
```
Added once to the player rig or a persistent GameObject by the orchestrator (not
self-installing). In `Update()`, toggle a `paused` bool on `Input.GetKeyDown(KeyCode.Escape)`;
when paused, `Time.timeScale = 0f` and show a world-space Canvas (positioned in front of
wherever the camera is looking at the moment of pausing — compute once at pause-time from
the main camera's current transform, not a fixed world position, since the player could be
anywhere in the now-large world) with Resume / Settings (adds/shows a `SettingsPanel`) /
Quit to Main Menu (`SceneManager.LoadScene("MainMenu")`, make sure to reset
`Time.timeScale = 1f` first) / Quit App buttons. Unpausing restores `Time.timeScale = 1f`.
Needs an `EventSystem` too — check for an existing one via `FindObjectOfType` before adding
another (the world scene may already have one from the Programming Central kiosks).

### Task 7 — VrUiPointer.cs
Owns: `Assets/Scripts/RepoCity/VrUiPointer.cs` (new). **Closes a real gap**: right now,
clicking any UI Button (kiosks, the new menus above) only works via desktop mouse —
`GraphicRaycaster` needs pointer events, and nothing currently feeds it from a VR
controller. The XR Interaction Toolkit package is deliberately NOT installed (avoiding
version-churn risk per this project's existing choices), so build this by hand.
```
namespace RepoCity {
    public class VrUiPointer : MonoBehaviour {
        public XRNode hand; // XRNode.LeftHand or XRNode.RightHand, set by whoever adds this component
    }
}
```
In `Update()` (only if `InputDevices.GetDeviceAtXRNode(hand).isValid`, mirroring the
`vrActive` check pattern in `Locomotion.cs`): get controller position/rotation the same way
`Locomotion.cs`'s `UpdateHandInspect` does (`transform.TransformPoint`/`TransformDirection`
from the device's local pose), `Physics.Raycast` a short visual laser line (a `LineRenderer`,
like the existing teleport-arc line) — but for UI, you can't `Physics.Raycast` a Canvas
Graphic directly; instead find every `GraphicRaycaster` in the scene once (cache on enable,
refresh if none found yet — menus/kiosks may not exist at the moment this component wakes
up), build a `PointerEventData` with `position` set by projecting the ray onto each
raycaster's canvas plane (or simpler: call `raycaster.Raycast(pointerEventData,
resultsList)` after setting `pointerEventData.position` to a simulated screen-space point —
research `GraphicRaycaster.Raycast`'s actual signature/needs before assuming; a simpler
viable alternative is iterating every `Button`/interactable `Graphic` in the scene, doing a
3D ray-vs-RectTransform-plane intersection test yourself, and calling
`ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler)` directly on
whatever you hit — pick whichever approach you can actually get working and verify, this is
the hardest task in this batch). On the VR trigger button going down (`CommonUsages.
triggerButton`, edge-triggered like the existing grip-press pattern in `Locomotion.cs`), fire
a click on whatever the laser currently hits. This needs an `EventSystem` present in the
scene (don't add a second one — check first). Flag clearly in your summary whether you
could get this fully working or only partially, since it's genuinely the trickiest task
here.

### Task 8 — More ambient audio (optional/stretch)
Owns: nothing code-wise — this is an asset-sourcing + two-line-wiring task. Check the
already-downloaded `rpg-audio` Kenney pack (re-download if gone, see URL-pattern note above)
for anything loopable (wind/door/cloth ambience) to use as a sparse environmental layer, and
check for a car engine/horn-ish clip across the packs already pulled
(`digital-audio`/`impact-sounds`). If something decent is found, copy it into
`Assets/Resources/RepoCity/Audio/` and report back the clip name + a one-line suggestion for
where the orchestrator should wire it in (e.g. attached to each spawned car in
`TrafficController.cs`'s `CarFollower` via a looping positional `AudioSource`, or a one-shot
on NPC pause/idle). Do not edit `TrafficController.cs` or any other gameplay file yourself —
report only, the orchestrator wires it in.

## Orchestrator-only work (after the parallel batch lands)

1. Read every new file, sanity-check it against its contract.
2. Wire into `WorldBootstrapper.cs`: `Landmarks.Highlight(island.root)` per island after
   `WorldBuilder.BuildAll`; create + position the `MiniMapUI` and `AchievementToastUI`
   GameObjects as camera children (see Task 2/3 notes on who positions what); add
   `VrUiPointer` components to both hand anchors if `Locomotion.cs` exposes/creates them (it
   currently doesn't track hand GameObjects explicitly — may need a small `Locomotion.cs`
   edit to expose left/right hand transforms, or just create two independent hand-tracking
   GameObjects directly in `WorldBootstrapper.cs` for the pointer to live on); add
   `PauseMenu` to the player rig.
3. Create the `MainMenu` scene via the new editor method, reorder build settings (`MainMenu`
   → `RepoWorld` → `RepoCity`).
4. Full compile-check, then full build, then launch + log-check + screenshot exactly as
   described above, from the main menu through Play into the world, pause, settings, and (if
   reachable) a kiosk run.
5. Commit with a clear message; update this PLAN.md's "Current state" section (or delete it
   if everything above is done and verified).
