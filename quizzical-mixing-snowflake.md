# BMS Project — Ordered Fix Plan

## Context

The project review (`Review.txt`) identified 8 confirmed bugs / missing connections that must be fixed before the Simulation panel works correctly and the visual polish matches the reference. The most severe blockers are:

- The Simulation page is built at runtime but never wired to the left-nav button, so it cannot be opened.
- The runtime cameras render once but are not driven by the existing `SimCameraRenderer`, so the 3D view inside the panel stays black.
- Several `SimulationUI` references (severity slider, fault toggle, time label) are missing or mis-typed.
- Graphs may compute `RectTransform.rect` before UI layout has run, producing zero-size line data.
- Secondary thermal camera orbits around `BatteryPack` instead of the actual `CellGroup`.
- World-space labels have no ESP32 slot even though the `ESP32_Module` GameObject exists.
- The BMS board light is orange at rest instead of amber.

## Scope

This plan covers **Fix 1 → Fix 8** from the review.

Out of scope for this pass (listed for reference / later scheduling):

- Schematic view toggle for the 3D/schematic buttons.
- Timeline scrubber under the 3D view.
- CSV export confirmation toast.

## Assumptions / Open Items

1. **No GameObject named `BatteryPanel` exists in the scene.** The review says to reparent `SimulationController` and `SimulationUI` "under BatteryPanel". I will create a new empty persistent manager GameObject named **`BatteryPanel`** at the scene root and place the runtime managers under it. If you intended a different parent (e.g. `DashboardCanvas` or `BatteryPack`), this is the only item to change.
2. **`SimPanelCamera` in the review refers to the runtime `SimPackCamera`** created by `SimCameraSetup.Build()` (there is no separate `SimPanelCamera` GameObject).
3. The follow-up features will be planned separately unless you say otherwise.

## Implementation Plan

### Fix 1 — Wire Panel.Simulation to NavBtn.Simulation

Goal: Clicking the Simulation nav button shows/hides `Panel_Simulation` and pauses/resumes the simulation engine accordingly.

Changes:
- Add a registration API to `NavController`:
  - `public void RegisterPanel(int index, GameObject panel)` to fill `linkedPanels[index]` at runtime.
  - `public void RegisterSimulationController(SimulationController ctrl)` so `SelectNav` can call `SetSimPageOpen(bool)`.
- In `NavController.SelectNav`, after `_activeIndex` is set, call `simController?.SetSimPageOpen(index == 5)`.
- In `SimulationPanelBuilder.Start`, after `BuildSimulationPanel()` completes, find `NavController` (`FindAnyObjectByType<NavController>`) and:
  - `nav.RegisterPanel(5, PanelSimulation);`
  - `nav.RegisterSimulationController(_ctrl);`
- Because `SimulationPanelBuilder.Start` may run before or after `NavController.Start`, do the registration inside the existing `DeferredCameraSetup` coroutine (after `yield return null`) or add a dedicated `yield return null` registration step so `NavController` is fully awake.

Files: `NavController.cs`, `SimulationPanelBuilder.cs`.

### Fix 2 — Stabilize Manager Hierarchy

Goal: The runtime `SimulationController` and `SimulationUI` GameObjects are no longer floating orphans.

Changes:
- In `SimulationPanelBuilder.BuildLeft` and `BuildRight`, change:
  ```csharp
  simGO.transform.SetParent(transform, false);     // current: builder root
  ctrlGO.transform.SetParent(transform, false);
  ttHost.transform.SetParent(transform, false);
  ```
  to a dedicated persistent parent.
- Create `BatteryPanel` at scene startup if it does not exist:
  ```csharp
  var mgr = GameObject.Find("BatteryPanel") ?? new GameObject("BatteryPanel");
  DontDestroyOnLoad(mgr); // optional, keeps managers stable
  ```
- Set `simGO`, `ctrlGO`, and `ttHost` parents to `mgr.transform`.
- Make sure `SimulationPanelBuilder` itself is also parented under `mgr` after finishing, or leave it as a sibling — either is fine as long as the managers are under `BatteryPanel`.

Files: `SimulationPanelBuilder.cs`.

### Fix 3 — Make the 3D View Render Continuously

Goal: The center `RawImage` in `Panel_Simulation` shows a live 3D pack view.

Changes:
- `SimCameraSetup.Build` already creates `SimPackCamera` + `SimPackRT` and assigns the RT to the visualization `RawImage`. Extend it to also wire the cameras into `SimCameraRenderer`:
  - Find the existing scene `SimCameraRenderer` component (`FindAnyObjectByType<SimCameraRenderer>`).
  - Assign `packCam` and `thermCam` to its `packCamera` and `thermalCamera` fields.
- Ensure the pack camera position gives a consistent default orbit view. Currently `SimPackCamera` is placed at `batteryPack.position + (5.5, 3.5, 6.5)` and looks at the pack origin. Tune this offset (or set `transform.rotation` to match the default Main Camera tilt of ~28°) so the framing matches the reference screenshots.
- Optional safety: if `SimCameraRenderer` GameObject is missing, create one so the system is self-healing at runtime.

Files: `SimCameraSetup.cs`.

### Fix 4 — Correct Thermal Thumbnail Target

Goal: The thermal thumbnail orbits around the actual cell meshes, not the entire `BatteryPack` root.

Changes:
- In `SimulationPanelBuilder.BuildRight`, locate `CellGroup` under `batteryPackTransform` and pass that to `ThermalThumbnail`:
  ```csharp
  var cellGroup = batteryPackTransform.Find("CellGroup");
  _thermalThumb.cellGroupTransform = cellGroup != null ? cellGroup : batteryPackTransform;
  ```
- Tune `_thermalThumb.cameraOffset` so the 8 cells fill the thumbnail from a ¾ angle. Start with the existing `(3.5, 2.5, 4.0)` relative to `CellGroup`; adjust after visual review if cells are too close/far.

Files: `SimulationPanelBuilder.cs`.

### Fix 5 — Force UI Layout Before Graph Rebuild

Goal: `LineRenderer` positions are computed only after the graph `RectTransform` has valid size.

Changes:
- In `SimulationGraph.RevealUpTo`:
  - Add a `_layoutReady` flag.
  - On first call, call `Canvas.ForceUpdateCanvases()`.
  - If `_rect.rect.width <= 0 || _rect.rect.height <= 0` after the force-update, return early without setting positions.
  - Otherwise set `_layoutReady = true` and continue with the normal graph math.
- This avoids the "size 0×D" problem reported in the review without changing the coordinate math.

Files: `SimulationGraph.cs`.

### Fix 6 — Complete SimulationUI Inspector Wiring

Goal: Fault severity, fault enable toggle, and elapsed/total time labels all work.

Changes:
- **Severity control**: The UI builder creates a Slider named `Severity` with values 0/1/2. `SimulationUI` currently declares `dropSeverity` as `TMP_Dropdown`. Change it to `Slider` and wire the existing slider. Update `BuildData`:
  ```csharp
  d.faultSeverity = sliderSeverity != null
      ? (SimSeverity)Mathf.RoundToInt(sliderSeverity.value)
      : SimSeverity.High;
  ```
- **Fault enable toggle**: Add a `Toggle` in `SimColumnBuilders.BuildLeft` (e.g. checkbox "Enable Fault Injection" or place it before the `INJECT FAULT` button). Wire it to `_ui.toggleFault`. Update `BuildData` to use `toggleFault.isOn`.
- **Time labels**: The header currently creates one combined `TimeLabel` text. Refactor `SimulationUI` to expose a single `public TMP_Text lblTime;` and update it as `Time Elapsed: {elapsed} / {total}`. In `SimHeaderBuilder.Build`, assign the created `TimeLabel` to `_ui.lblTime` (via `SimulationPanelBuilder`). Update `OnPlayheadUpdated`, `SetDefaults`, and the duration slider listener to write to `lblTime`.
- After wiring, add a debug helper in `SimulationPanelBuilder` (editor-only) that logs any remaining `null` references in `SimulationUI` so we catch wiring regressions early.

Files: `SimulationUI.cs`, `SimColumnBuilders.cs`, `SimHeaderBuilder.cs`, `SimulationPanelBuilder.cs`.

### Fix 7 — Add / Fix ESP32 World-Space Label

Goal: The ESP32 module gets a working billboard label.

Changes:
- In `WorldSpaceLabels`:
  - Add `public Transform esp32Transform;` and a third label entry.
  - If `esp32Transform` is assigned, use its position (so the label follows the ESP32 mesh even if it moves); otherwise hide the ESP32 label slot.
  - Add a `GetValue` branch for `dataType == 2` returning a useful string (e.g. status or temperature). Start with the existing live/sim fallback if no better data is available.
- In `SimulationPanelBuilder` or `Start()` of `WorldSpaceLabels`, find `BatteryPack/ESP32_Module` and assign it to `esp32Transform`.
- Decide whether to activate `ESP32_Module` mesh. The review says "verify GameObject name or remove the dead slot". Because the mesh is inactive, I will keep it inactive and only show the floating label anchored to its transform. If you want the mesh visible, that can be toggled separately.

Files: `WorldSpaceLabels.cs`, `SimulationPanelBuilder.cs` (for auto-wire).

### Fix 8 — Tune BMS Board Light Color + Visual Verification

Goal: The BMS board light reads as amber at rest (≈22 °C), shifting to deeper red only as temperature rises.

Changes:
- In `SceneLightPulse`:
  - Change the cool/rest color from `new Color(1f, 0.42f, 0f)` to a warmer amber, e.g. `new Color(1f, 0.65f, 0.12f)`.
  - Keep a hot color such as `new Color(1f, 0.12f, 0f)`.
  - Maintain the `InverseLerp(20, 60, temperature)` driver.
- After the code changes, enter Play Mode and capture screenshots via `manage_camera` in the four states:
  1. Overview
  2. Thermal view mode
  3. Balancing view mode
  4. Simulation panel (after running a simulation)
- Compare screenshots against the reference. Iterate the amber base color and thermal camera offset if needed.

Files: `SceneLightPulse.cs`.

## Critical Files to Modify

| File | What it controls |
|------|------------------|
| `Assets/Scripts/NavController.cs` | Nav button → panel wiring + simulation pause callback. |
| `Assets/Scripts/SimulationPanelBuilder.cs` | Runtime panel build, manager parenting, `ThermalThumbnail` target. |
| `Assets/Scripts/SimCameraSetup.cs` | Camera + RT creation; needs to drive `SimCameraRenderer`. |
| `Assets/Scripts/SimCameraRenderer.cs` | (may need no change if wired by `SimCameraSetup`) |
| `Assets/Scripts/ThermalThumbnail.cs` | (likely no change; target wired by builder) |
| `Assets/Scripts/SimulationGraph.cs` | Layout-tolerant graph rebuild. |
| `Assets/Scripts/SimulationUI.cs` | Missing severity slider / fault toggle / time label refs. |
| `Assets/Scripts/SimColumnBuilders.cs` | Add fault toggle; severity is already a slider. |
| `Assets/Scripts/SimHeaderBuilder.cs` | Capture time label reference for `SimulationUI`. |
| `Assets/Scripts/WorldSpaceLabels.cs` | Add ESP32 anchored label. |
| `Assets/Scripts/SceneLightPulse.cs` | Amber base color tuning. |

## Verification Steps

1. Open Unity, enter Play Mode, open the Console.
2. Click each nav button. **Simulation** should open `Panel_Simulation`; clicking **Overview** should close it and resume dashboard side panels.
3. With Simulation open, press **RUN**. After a short time:
   - The center 3D view shows the battery pack.
   - The thermal thumbnail on the right updates.
   - The four graphs draw lines as the playhead advances.
   - The header time label advances and shows elapsed/total.
4. Inject a fault: enable the toggle, pick a cell, pick a type, set severity, click **INJECT FAULT** — the button should respond and the simulation should reflect the severity.
5. Switch to **Overview** while a simulation is running — it should pause (`SetSimPageOpen(false)`). Switch back — it should resume.
6. Observe the world-space labels; the ESP32 label should hover near the ESP32 module location.
7. Check the BMS board light color at rest (~22 °C); it should look amber, not orange-red.
8. Capture screenshots in the four requested states and confirm they match the visual reference.

## Risks & Notes

- **Runtime vs. serialized wiring**: Because `SimulationPanelBuilder` creates managers at `Start`, nav wiring must be done at runtime. Any future conversion to a pre-built prefab would remove the need for runtime registration.
- **Layout force-update cost**: `Canvas.ForceUpdateCanvases()` is global; calling it only on the first graph draw keeps the cost negligible.
- **BatteryPanel assumption**: This is the only naming/placement assumption in the plan. If you want `SimulationController`/`SimulationUI` under a different parent, tell me before implementation begins; otherwise I will create `BatteryPanel` as described.

## Follow-Up Work (Future Pass)

- Implement the **SCHEMATIC VIEW** toggle: build a second view with 8 flat cells + B+/B− wiring, colored by live SoC, and switch between 3D and schematic.
- Add a **time scrubber** under the 3D view tied to `SimulationData.playheadTime`, with graph-click snapping.
- Show a confirmation/toast after **EXPORT RESULTS** with the full `Application.persistentDataPath` file path.
