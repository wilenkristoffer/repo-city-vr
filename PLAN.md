# Repo City VR — Status

All work from the previous plan is complete and verified as of commit after `4986054`. This
file is now just a short status note instead of a task list.

## What shipped this pass

- Fixed all remaining audit findings from `audit-digest.txt` (MiniLang recursion-depth +
  per-step-cost guards, CityBuilder multi-material recolor + perf, Locomotion per-hand
  teleport visuals, SceneBootstrapper coroutine-stop + fallback camera, NpcController
  animation-clip caching, WorldBuilder cleanup-on-exception, RepoData malformed-JSON message,
  NpcChat JSON escaping, CodeKiosk error line-number offset, HudPanel legend overflow) plus
  the CodeArcadeBuilder/WorldBootstrapper approach-direction mismatch.
- New: `Landmarks.cs` (gold/cyan beacons on each island's most-active/oldest file),
  `MiniMapUI.cs` (schematic corner HUD map, confirmed visible in testing), `AchievementToastUI.cs`
  + `AchievementTracker.cs` (exploration milestones), a full `MainMenu` scene (Play/Settings/Quit,
  now the startup scene) + `SettingsPanel.cs` (volume/sensitivity/quality/fullscreen, reused by
  both the main menu and `PauseMenu.cs`), and `VrUiPointer.cs` (hand-rolled VR laser pointer for
  legacy UGUI, since XR Interaction Toolkit isn't installed).
- Scene boot order is now `MainMenu` → `RepoWorld` → `RepoCity` (`SetupScene.cs` has
  `Create()`/`CreateWorld()`/`CreateMainMenu()`/`ReorderForMainMenuStartup()`).

## Verified

- Clean Unity batchmode compile after every change.
- Full standalone build succeeded; launched and screenshotted both the `MainMenu` scene (title/
  buttons render correctly, no overlap after a layout fix) and the `RepoWorld` scene (temporarily
  swapped to startup for testing, then restored) — all 3 islands build with zero exceptions, and
  the MiniMap is visibly rendering in-frame.
- `VrUiPointer.cs` is code-reviewed and logically sound (geometric ray-vs-canvas-plane hit test +
  `ExecuteEvents.Execute` click dispatch) but **not verified against a real VR headset or
  controller** — no hardware available in this environment. Worth a real-headset check next
  time you put the headset on.

## Known minor items, not blocking

- `Assets/Resources/RepoCity/Audio/door_creak.ogg` was sourced (Kenney CC0, from `rpg-audio`)
  but never wired into code — suggested use was a sparse randomized one-shot near building
  entrances, or on NPC idle. Nobody's using it yet.
- Full raw audit-fix-batch agent output saved as `batch2-findings-raw.json` /
  `batch2-digest.txt` at the project root, for reference if anything here needs re-checking.
