# PROJECT_CONTEXT.md — First-Person Delivery Life Simulator

> **Purpose of this file:** Give this file (plus `ROADMAP_PROGRESS.md`) to Claude / Antigravity at the start of any new chat.
> It contains everything needed to continue development without re-explaining the game, the code, the folders, or the conventions.
> Rewritten in full after every major step.

---

## 1. Game Pitch

A polished, realistic, immersive **single-player first-person package delivery life simulator** for a **paid Steam release**.

The player starts with very little money, rents a small home, owns no vehicle, and gets a basic delivery job in a small town. The goal is to experience the everyday life of a delivery worker and gradually become financially stable through hard work.

**World:** a compact town with residential houses, a delivery warehouse, a vehicle rental office, and the player's rented home.

## 2. Core Daily Loop

1. Wake up at home (7:00 AM).
2. Travel to the warehouse (on foot at first).
3. Collect the assigned batch of packages from the warehouse counter (during opening hours 7:00 AM – 6:00 PM). Earn **daily attendance salary ($20)** upon collecting.
4. Deliver each package to its correct house (matching House ID). Fixed pay per package (Small $5, Medium $8, Large $12).
5. When all packages of the day are delivered, earn **completion bonus ($15)**.
6. Energy drops gradually while working (more when carrying packages). Sleeping restores energy and advances the game to the next morning.
7. Pay **weekly** house rent, electricity bill and vehicle rental fee. Unpaid amounts become **debt**.
8. Eventually rent a car or van from the rental office to deliver faster.

## 3. Design Pillars and Scope Rules

- **Small but detailed town**, satisfying first-person interactions, believable daily routines, polished core gameplay.
- **NOT** a huge open world. **NOT** a complicated business-management sim.
- **Physical Hand-Carrying Over Menus:** Packages and physical items are carried, inspected, and placed with hands (`ItemCarrier`). No traditional RPG backpack inventory grids; modern information (deliveries, map, bills) lives on the player's upcoming Smartphone.
- Must stay achievable for a **solo developer**: prefer simple, reusable systems over complex ones.
- Final goal: complete, optimized, enjoyable, worth buying on Steam.

## 4. Technical Environment

| Item | Value |
|---|---|
| Engine | Unity 6 (`6000.3.23f1`) |
| Render pipeline | Universal Render Pipeline (URP) |
| Input | **New Input System** (`UnityEngine.InputSystem`), keyboard + mouse + gamepad |
| UI | **TextMeshPro**, authored via **`Main Canvas` and Panels** in the Unity Editor with drag-and-drop references |
| Font | Lilita One → TMP Font Asset assigned in the `Font` field of `InteractionHud` and UI elements (`UIFactory.DefaultFont`) |
| Script root | **`Assets/FPP/Scripts/<SubFolder>/`** |
| Active scene | `FPP Scene` (MainTown scene not created yet) |
| Currency | `$` (whole numbers) |

### Scene hierarchy (test scene)

```
FPP Scene
├─ Directional Light        (sun assigned to DayNightCycle)
├─ FPP Player               (tag: Player, layer: Ignore Raycast; CharacterController + scripts)
│   └─ View Pivot           (layer: Ignore Raycast; crouch moves this)
│       └─ Main Camera      (tag: MainCamera; HeadBob moves this)
├─ Ground                   (collider)
├─ Manager                  (DailyBatchManager, EconomyManager, TimeManager, DayNightCycle, DailyRewards)
├─ PackageCounter           (+ Spawn Point 1..3 children, Board TMP text)
├─ House_01..House_06       (HouseId + DeliveryPoint, optional PlacePoint child on porch)
├─ Main Canvas              (Screen Space - Overlay, CanvasScaler 2560x1440 Match 0.5)
│   ├─ Status_Panel         (MoneyText, MoneyChangeText, DayText, DeliveriesText)
│   ├─ Interaction_Panel    (Crosshair, RingBack, RingFill, PromptText, ToastText, HintsText, Inspect_SubPanel)
│   └─ DayTransition_Panel  (BlackScreen, TextContainer: DayTitleText, DaySubtitleText)
└─ EventSystem
```

Important: the player and View Pivot are on the **Ignore Raycast** layer so interaction rays never hit the player.

---

## 5. Script Folder Structure (ALWAYS follow this)

All scripts live in `Assets/FPP/Scripts/<SubFolder>/`. **Every time a script is delivered it must state its target folder.**
Move files by **dragging inside the Unity Project window** (never in Windows Explorer / Finder), so `.meta` links and Inspector references are not broken. To replace a script, copy the new file over the old one in the same folder (same file name keeps Inspector links).

| Folder (under `Assets/FPP/Scripts/`) | Scripts |
|---|---|
| `Player/` | `FppPlayerController.cs`, `HeadBob.cs`, `FootstepPlayer.cs`, `PlayerInputLock.cs` |
| `Interaction/` | `IInteractable.cs`, `PlayerInteractor.cs`, `InteractableObject.cs`, `HoldInteractable.cs`, `InteractableDoor.cs`, `InteractableHighlight.cs`, `PickupItem.cs` |
| `Items/` | `ItemData.cs`, `ItemCarrier.cs` |
| `Delivery/` | `PackageDefinition.cs`, `Package.cs`, `HouseId.cs`, `DailyBatchManager.cs`, `PackageCounter.cs`, `DeliveryPoint.cs` |
| `Economy/` | `EconomyManager.cs`, `DailyRewards.cs` |
| `Time/` | `TimeManager.cs`, `DayNightCycle.cs` |
| `UI/` | `UIFactory.cs`, `InteractionHud.cs`, `StatusHud.cs`, `DayTransition.cs` |
| `Core/` | `Notifier.cs`, `InputGlyphs.cs`, `GameAudio.cs` (also contains `ProceduralSfx`) |

**Planned folders (create when the system is built):**

| Folder (under `Assets/FPP/Scripts/`) | Future scripts |
|---|---|
| `Energy/` | `PlayerEnergy`, sleep, bed interaction |
| `Bills/` | `BillsManager`, rent, electricity, debt |
| `Vehicles/` | rental office, vehicle controller, enter/exit, parking |
| `Phone/` | smartphone UI, map, markers |
| `Save/` | save/load system |
| `Menus/` | main menu, pause menu, settings |

**Data / prefab folders:**
`Assets/FPP/Data/Items` (ItemData), `Assets/FPP/Data/Packages` (Package_Small / Medium / Large), `Assets/FPP/Prefabs/` (Package, Houses), `Scenes`, `Materials`, `Fonts/Lilita_One`.

---

## 6. Existing Code

### 6.1 Player (`Player/`)
- **`FppPlayerController`** — User's original script, unchanged. Sealed, `[RequireComponent(CharacterController)]`. Walk / sprint / jump / crouch, mouse + gamepad look, cursor lock (Esc unlocks, click re-locks). Public: `IsGrounded`, `IsCrouched`, `CurrentSpeed`, `ViewPivot`. Walk 5, sprint 8, crouch 2.5. Controls: WASD, Shift sprint, Space jump, Ctrl/C crouch, Esc unlock cursor.
- **`PlayerInputLock`** — Lets several systems freeze the player safely (disables `FppPlayerController`). `Lock(owner)` / `Unlock(owner)`, `IsLocked`. Refuses to lock while airborne. Auto-added at runtime.
- **`HeadBob`** — Bobs the **camera** (not View Pivot, avoiding conflict with crouch). Landing dip. Events: `Stepped`, `Landed`. Execution order -100.
- **`FootstepPlayer`** — Plays step/landing sounds from `HeadBob` events (procedural placeholders if no clips).

### 6.2 Interaction (`Interaction/`)
| Script | Role |
|---|---|
| `IInteractable` (+ `IHoldInteractable`) | Interface: `InteractionPrompt`, `CanInteract(interactor)`, `Interact(interactor)`. Hold variant adds `HoldDuration`. NOTE: `CanInteract == false` hides prompt completely; to show a "closed" prompt, return true and refuse inside `Interact`. |
| `PlayerInteractor` | On player. Raycast/spherecast from camera center (distance 3, radius 0.05). Finds `IInteractable` via `GetComponentInParent`. Auto-adds glow highlight. Tap + hold interaction. Public: `CurrentTarget`, `TargetIsHold`, `HoldProgress`, `InteractKey`, `Blocked`, `TargetChanged` event. Interact = **E** / gamepad West. Paused while `Blocked`, locked by `PlayerInputLock`, or cursor unlocked. **Interaction stays ACTIVE while carrying an item.** |
| `InteractableObject` | Generic tap interactable with `UnityEvent onInteract`, optional single use + cooldown. |
| `HoldInteractable` | Hold-to-interact with progress ring, `UnityEvent onComplete`. |
| `InteractableDoor` | Swinging door, optional lock + `ItemData requiredKey` (+ `consumeKey`). Unlocked when player interacts while holding the matching key in hands with `ItemCarrier`. |
| `InteractableHighlight` | Pulsing emission glow on looked-at object (auto-added at runtime; needs emission-capable material). |
| `PickupItem` | On any pickup (needs Collider + Rigidbody). Optional `ItemData`. Props: `DisplayName`, `Description`, `Weight`, `Data`, `IsHeld`. `SetInfo(name, description, weight)` runtime override (used by `Package`). `SetPickupEnabled(bool)`. `CanInteract`: false when held/disabled; carried items need free hands. Impact sound on collision. Public: `OnPickedUp()`, `OnReleased(velocity)`. |

### 6.3 Items (`Items/`)
| Script | Role |
|---|---|
| `ItemData` (ScriptableObject) | `Id`, `DisplayName`, `Description`, `Icon`, `Weight`, `Mode` (`PickupMode.Carry`), `WorldPrefab`. Menu: Create → Game → Item Data. |
| `ItemCarrier` | On player. Holds ONE `PickupItem` in front of camera (smooth follow, wall-pushback raycast). **Inspect** (F / right mouse / gamepad Y): freezes player, mouse rotates item, scroll zooms. **Drop** Q / gamepad B, **Throw** left mouse / gamepad RB. `holdToInspect` option. Public: `Pick(item)`, `DetachHeld()`, `IsHolding`, `IsInspecting`, `HeldItem`. |

### 6.4 Delivery (`Delivery/`)
| Script | Role |
|---|---|
| `PackageDefinition` (ScriptableObject) | One per package TYPE. `DisplayName`, `Size` (`PackageSize` Small/Medium/Large), `Description`, `Weight`, fixed `Reward`, `Dimensions` (applied as scale), `Tint`, `Prefab`. Menu: Create → Game → Package Definition. Current values: Small 1.5 kg / $5, Medium 4 kg / $8, Large 9 kg / $12. |
| `Package` | On the package prefab next to `PickupItem`. Applies definition (scale, tint, Rigidbody mass), sets HUD name/description via `PickupItem.SetInfo`. Holds destination: `DestinationId`, `DestinationLabel`; `Reward`, `IsDelivered`, `MarkDelivered()` (also disables pickup), `Initialize(def, houseId, address)`. ONE prefab serves all three sizes. |
| `HouseId` | On every house. Unique `Id` + `Address` + optional `deliveryPoint`. Static registry `HouseId.All`, `HouseId.TryGet(id, out house)`, duplicate-ID warning, Scene-view gizmo label. |
| `DailyBatchManager` | Singleton `Instance` on `Manager`. Weighted package types, min/max packages, deterministic per-day seed. `StartDay(day)` builds `Assignments` (`PackageAssignment`: definition, houseId, address, collected, delivered). `MarkCollected()`, `TryMarkDelivered(houseId, definition)`, `RemainingCount`, `DeliveredCount`, `TotalCount`, `Day`, `IsCollected`. Events: `BatchChanged`, `BatchCollected` (pays salary), `BatchCompleted` (pays bonus). Subscribes to `TimeManager.DayStarted`. Context menu "Debug: Start Next Day". |
| `PackageCounter` | `IInteractable` on the warehouse counter. **Opening hours** (`useOpeningHours`, `openHour` 7, `closeHour` 18): when closed, prompt says "Warehouse closed (opens 7:00 AM)" and pressing E refuses with a toast. Optional 3D TMP board lists day's deliveries plus OPEN/CLOSED and hours. Interact = collect batch: spawns packages from prefab, toasts, then `MarkCollected()`. On `TimeManager.DayStarted`, undelivered spawned packages are destroyed. |
| `DeliveryPoint` | `IInteractable` on house/porch. Only active while player holds a `Package`. Wrong house → toast + denied sound, no pay. Right house → `DetachHeld()`, `Package.MarkDelivered()` FIRST, `EconomyManager.Earn(reward)`, toast "Delivered to ... +$X", success sound, then `DailyBatchManager.TryMarkDelivered` (last triggers `BatchCompleted`), package placed on `placePoint`, destroyed after `cleanupDelay` (120 s). |

### 6.5 Economy (`Economy/`)
| Script | Role |
|---|---|
| `EconomyManager` | Singleton `Instance` on `Manager`. Player wallet. `Balance`, `startingMoney` (default 50 or 100), `Earn(amount, reason)`, `TrySpend(amount, reason)`, `CanAfford`, `SetBalance`. Events: `BalanceChanged(int)`, `TransactionMade(int delta, string reason)`. Context menu "Debug: Earn $100". |
| `DailyRewards` | On `Manager`. `attendanceSalary` ($20) paid on `DailyBatchManager.BatchCollected`; `completionBonus` ($15) paid on `BatchCompleted`. Shows toasts for both. |

### 6.6 Time (`Time/`)
| Script | Role |
|---|---|
| `TimeManager` | Singleton `Instance` on `Manager` (execution order -50). Clock in hours: `TimeOfDay` (0–24), `NormalizedTime`, `Hour`, `Minute`, `Day`, `IsPastMidnight`, `Paused`. Settings: `startDay` 1, `morningHour` 7, `gameMinutesPerRealSecond` 1, `use24HourClock` false/true. Events: `HourChanged(int)`, `DayEnded(int)`, `DayStarted(int)`. `RequestNextDay(reason)` (fades via `DayTransition`) → `CompleteNextDay()`. Passes out at `passOutHour` (2:00 AM). Context menus: +1 hour, jump to 5:55 PM, jump to 1:55 AM, next morning. |
| `DayNightCycle` | On `Manager`. Controls `Sun` (`Directional Light`). Rotates sun by time, tints with gradient, scales sun intensity, ambient light and skybox exposure. URP compatible. |

### 6.7 UI (`UI/`)
All UI is authored in the Unity Editor under **`Main Canvas`** and linked via serialized `[SerializeField]` drag-and-drop references (with automatic fallback to `UIFactory` if unassigned).
| Script | Role |
|---|---|
| `UIFactory` | Static helpers: canvas, rects, images, TMP text, generated sprites. `UIFactory.DefaultFont` sets the font. |
| `InteractionHud` | On player. Linked to `Interaction_Panel`: Crosshair, interaction prompt (keyboard/gamepad glyph aware), hold ring, carry hints, queued toast notifications, and inspect screen card. |
| `StatusHud` | On player. Linked to `Status_Panel`: Money counter (animated), floating `+$`/`-$`, **`Day N   7:00 AM` clock**, `Deliveries left: x / y`. |
| `DayTransition` | On player. Singleton `Instance`. Linked to `DayTransition_Panel`: Black fade image (`CanvasGroup`), "Day N" title + reason subtitle. Freezes player via `PlayerInputLock` and pauses clock. Timing: `fadeOutTime` 1s, `holdTime` 2s, `fadeInTime` 1.2s. |

### 6.8 Core helpers (`Core/`)
| Script | Role |
|---|---|
| `Notifier` | Static: `Notifier.Show("text")` → HUD toast. Centralized player messaging. |
| `InputGlyphs` | Static: tracks keyboard vs gamepad, returns coloured `[E]` style badge for prompts. |
| `GameAudio` + `ProceduralSfx` | `GameAudio.Play(clip, pos, volume, pitchVar, spatialBlend)`. `ProceduralSfx` generates placeholder sounds (Thud, Click, Step, Pickup, Whoosh, Unlock, Denied). |

### 6.9 Components currently on FPP Player
`CharacterController`, `FppPlayerController`, `PlayerInteractor`, `ItemCarrier`, `InteractionHud`, `HeadBob`, `FootstepPlayer`, `StatusHud`, `DayTransition` (+ `PlayerInputLock` auto-added). *(Inventory and InventoryPanel removed).*

### 6.10 Components currently on Manager
`EconomyManager`, `DailyBatchManager`, `TimeManager`, `DailyRewards`, `DayNightCycle`.

---

## 7. Conventions to Keep Using

- `sealed` classes where inheritance isn't needed; `[SerializeField] private` fields; public read-only properties.
- **ScriptableObjects for data** (items, packages, bills, vehicles). Compare assets by reference, never by name. Never modify SO values at runtime.
- Cross-system messages to the player go through `Notifier.Show(...)`. All money changes go through `EconomyManager`.
- Freeze the player only through `PlayerInputLock`, never by toggling `FppPlayerController` directly.
- Managers are scene singletons with a static `Instance` and C# events. Subscribe in `Start`, unsubscribe in `OnDestroy`.
- **Anything that happens "each day" listens to `TimeManager.DayStarted` / `DayEnded`; anything hourly listens to `HourChanged`.** The next morning is always started with `TimeManager.RequestNextDay(reason)`.
- Gameplay time uses scaled `Time.deltaTime`; UI and transitions use `Time.unscaledDeltaTime`. To freeze game time, set `TimeManager.Paused`.
- **UI Authoring:** Visual panels and TextMeshPro components are authored in the Unity Editor inside `Main Canvas` and drag-and-dropped into Inspector fields.
- Placeholder audio uses `ProceduralSfx` until real clips exist.
- Use the **new Input System** only (`Keyboard.current`, `Gamepad.current`, never `UnityEngine.Input`).
- New scripts go in the correct `Assets/FPP/Scripts/<SubFolder>/` (section 5).

---

## 8. Known Limitations / Notes

- Inspect and day transition freeze the player by disabling `FppPlayerController` (only allowed while grounded).
- `ItemCarrier` holds ONE item in hands. Carry packages one at a time.
- Passing out at 2:00 AM is temporary until Milestone 06 (energy and sleeping take over).
- `DailyBatchManager` requires `HouseId` objects in the scene before `Start`.
- Delivered packages stay on the porch for 120 s, then destroy. Undelivered packages destroy at the next morning.
- **Confirmed working in Unity (user tests through 2026-11-08):**
  - Movement, interaction, pickup/carry/inspect/drop
  - Warehouse counter, opening hours (7 AM - 6 PM), batch spawning
  - Delivery point validation (wrong/right house, pay once, remaining count)
  - Economy wallet, attendance salary ($20), completion bonus ($15)
  - TimeManager clock, DayNightCycle lighting, StatusHud clock & money
  - DayTransition smooth fade & morning wake-up
  - Authored Canvas Panels: `Status_Panel`, `Interaction_Panel`, `DayTransition_Panel`
  - Removal of RPG inventory grid; hands physical carrying confirmed as primary mechanic

---

## 9. Proposed Architecture for Upcoming Systems

- **`PlayerEnergy` (Milestone 06):** drains over time, extra while carrying (reads `ItemCarrier.IsHolding` and held weight), low-energy effects, restored by sleeping; bed calls `TimeManager.RequestNextDay("Good morning!")`; replaces temporary pass-out rule.
- **`BillsManager` (Milestone 07):** weekly cycle driven by `TimeManager.DayStarted` (rent, electricity, vehicle rental), auto-pay through `EconomyManager.TrySpend`, unpaid amounts become debt.
- **`Smartphone` (Milestone 09):** player's modern in-game device that acts as jobs/delivery list, map, wallet balance, and bills view (replaces conventional menus).
- **Save system (Milestone 12):** managers expose state through serializable data classes.

---

## 10. How to Resume

1. Read this file and `ROADMAP_PROGRESS.md` fully.
2. Find the **Current Step** in `ROADMAP_PROGRESS.md`.
3. Guide the Unity setup ONE small step at a time (wait for user confirmation before moving on).
4. For every script delivered, state its target folder (`Assets/FPP/Scripts/<SubFolder>/`).
5. After each major step is done, **REWRITE BOTH `.md` FILES IN FULL**.
