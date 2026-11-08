# PROJECT_CONTEXT.md — First-Person Delivery Life Simulator

> **Purpose of this file:** Give this file (plus `ROADMAP_PROGRESS.md`) to Claude at the start of any new chat.
> It contains everything needed to continue development without re-explaining the game, the code, or the conventions.
> Keep it updated whenever a system is added or changed (see "How to update" at the bottom).

---

## 1. Game Pitch

A polished, realistic, immersive **single-player first-person package delivery life simulator** for a **paid Steam release**.

The player starts with very little money, rents a small home, owns no vehicle, and gets a basic delivery job in a small town. The goal is to experience the everyday life of a delivery worker and gradually become financially stable through hard work.

**World:** a compact town with residential houses, a delivery warehouse, a vehicle rental office, and the player's rented home.

## 2. Core Daily Loop

1. Wake up at home.
2. Travel to the warehouse (on foot at first).
3. Collect the assigned batch of packages from the warehouse counter.
4. Deliver each package to its correct house (matching House ID).
5. Earn money: **fixed payment per package + daily base salary + possible completion bonus**.
6. Energy drops gradually while working (more when carrying packages). Sleeping restores energy and advances the game to the next morning.
7. Pay **weekly** house rent, electricity bill and vehicle rental fee. Unpaid amounts become **debt**.
8. Eventually rent a car or van from the rental office to deliver faster.

## 3. Design Pillars and Scope Rules

- **Small but detailed town**, satisfying first-person interactions, believable daily routines, polished core gameplay.
- **NOT** a huge open world. **NOT** a complicated business-management sim.
- Must stay achievable for a **solo developer**: prefer simple, reusable systems over complex ones.
- Final goal: complete, optimized, enjoyable, worth buying on Steam.

## 4. Technical Environment

| Item | Value |
|---|---|
| Engine | Unity (version: **fill in**, e.g. Unity 6 / 2022 LTS) |
| Render pipeline | **fill in** (Built-in / URP) |
| Input | **New Input System** (`UnityEngine.InputSystem`), keyboard + mouse + gamepad |
| UI | **TextMeshPro**, UI is **built from code** at runtime (no hand-made Canvas) |
| Font | Lilita One (`Assets/Fonts/Lilita_One/LilitaOne-Regular.ttf`) → TMP Font Asset assigned in the Font field of `InteractionHud` and `InventoryPanel` |
| Scripts folder | `Assets/Scripts` |
| Test scene | `FPP Scene` |

### Scene hierarchy (test scene)

```
FPP Scene
├─ Directional Light
├─ FPP Player        (tag: Player, layer: Ignore Raycast; CharacterController + scripts)
│   └─ View Pivot    (layer: Ignore Raycast; crouch moves this)
│       └─ Main Camera (tag: MainCamera; HeadBob moves this)
└─ Ground            (collider)
```

Important: the player and View Pivot are on the **Ignore Raycast** layer so interaction rays never hit the player.

---

## 5. Existing Code (all in `Assets/Scripts`)

### 5.1 Player movement (user's original script, unchanged)
- **`FppPlayerController`** — sealed, `[RequireComponent(CharacterController)]`. Walk / sprint / jump / crouch, mouse + gamepad look, cursor lock (Esc unlocks, click re-locks). Public: `IsGrounded`, `IsCrouched`, `CurrentSpeed`, `ViewPivot`. Walk 5, sprint 8, crouch 2.5.
- Controls: WASD, Shift sprint, Space jump, Ctrl/C crouch, Esc unlock cursor.

### 5.2 Interaction system
| Script | Role |
|---|---|
| `IInteractable` (+ `IHoldInteractable`) | Interface: `InteractionPrompt`, `CanInteract(interactor)`, `Interact(interactor)`. Hold variant adds `HoldDuration`. |
| `PlayerInteractor` | On player. Raycast/spherecast from camera centre (distance 3, radius 0.05). Finds `IInteractable` via `GetComponentInParent`. Auto-adds glow highlight. Handles tap + hold interaction. Public: `CurrentTarget`, `TargetIsHold`, `HoldProgress`, `InteractKey`, `Blocked`, `TargetChanged` event. Interact = **E** / gamepad West. |
| `InteractableObject` | Generic tap interactable with a `UnityEvent onInteract`, optional single use + cooldown. |
| `HoldInteractable` | Hold-to-interact with progress ring, `UnityEvent onComplete`. |
| `InteractableDoor` | Swinging door, optional lock + `ItemData requiredKey` (+ `consumeKey`). |
| `InteractableHighlight` | Pulsing emission glow on looked-at object (auto-added at runtime; needs emission-capable material). |
| `PlayerInputLock` | Lets several systems freeze the player safely (disables `FppPlayerController`). `Lock(owner)` / `Unlock(owner)`. Refuses to lock while airborne. Auto-added at runtime. |

### 5.3 Carrying / items
| Script | Role |
|---|---|
| `PickupItem` | On any pickup (needs Collider + Rigidbody). Optional `ItemData`. Props: `DisplayName`, `Description`, `Weight`, `Data`, `IsHeld`. Store-mode items go to inventory, otherwise carried. Impact sound on collision. Public: `OnPickedUp()`, `OnReleased(velocity)`, `TryStore(inventory)`. |
| `ItemCarrier` | On player. Holds ONE `PickupItem` in front of camera (smooth follow, wall-pushback raycast). **Inspect mode** (F / right mouse / gamepad Y): freezes player, mouse rotates item, scroll zooms. **Drop** Q / gamepad B, **Throw** left mouse / gamepad RB, **Store** R / gamepad LB. `holdToInspect` option. Public: `Pick(item)`, `IsHolding`, `IsInspecting`, `HeldItem`, `CanStoreHeld`. Sets `PlayerInteractor.Blocked = true` while holding. |
| `ItemData` (ScriptableObject) | `Id`, `DisplayName`, `Description`, `Icon`, `Weight`, `MaxStack`, `Mode` (`PickupMode.Carry` / `Store`), `CanStoreWhenCarried`, `WorldPrefab`. Menu: Create → Game → Item Data. |
| `Inventory` | On player. Slot + weight based list. `Add`, `Remove`, `Has`, `Count`, `DropFromSlot`, `Changed` event, `Slots`, `SlotCapacity`, `MaxWeight`, `TotalWeight`. |

### 5.4 UI (built from code, TextMeshPro)
| Script | Role |
|---|---|
| `UIFactory` | Static helpers: create canvas, rects, images, TMP text, generated sprites (ring, dot, vignette). `UIFactory.DefaultFont` sets the font. |
| `InteractionHud` | On player. Crosshair, interaction prompt (keyboard/gamepad glyph aware), hold ring, carry hints, toast messages, inspect screen (vignette + name/description panel). Has `Font` field. |
| `InventoryPanel` | On player. Tab / gamepad View opens inventory, W/S select, Q drop. Freezes player. Has `Font` field. |
| `Notifier` | Static: `Notifier.Show("text")` → toast on HUD. Use this for ALL player messages (payments, errors, etc). |
| `InputGlyphs` | Static: tracks keyboard vs gamepad, `Pick(Key, "padLabel")` returns coloured `[E]` style badge for prompts. |

### 5.5 Feel
| Script | Role |
|---|---|
| `HeadBob` | On player. Bobs the **camera** (not View Pivot, to avoid fighting crouch). Events `Stepped`, `Landed`. |
| `FootstepPlayer` | On player. Plays step/landing sounds from `HeadBob` events. |
| `GameAudio` + `ProceduralSfx` | `GameAudio.Play(clip, pos, volume, pitchVar, spatialBlend)`. `ProceduralSfx` generates placeholder sounds (Thud, Click, Step, Pickup, Whoosh, Unlock, Denied) so nothing needs audio assets. Swap in real clips later. |

### 5.6 Components currently on FPP Player
`CharacterController`, `FppPlayerController`, `PlayerInteractor`, `ItemCarrier`, `InteractionHud`, `Inventory`, `InventoryPanel`, `HeadBob`, `FootstepPlayer` (+ `PlayerInputLock` auto-added).

### 5.7 Delivery system (Milestone 02)
| Script | Role |
|---|---|
| `PackageDefinition` (ScriptableObject) | One per package TYPE. `DisplayName`, `Size` (`PackageSize` Small/Medium/Large), `Description`, `Weight`, fixed `Reward`, `Dimensions` (applied as scale), `Tint`, `Prefab`. Menu: Create → Game → Package Definition. |
| `Package` | On the package prefab next to `PickupItem`. Applies definition (scale, tint, Rigidbody mass) and sets the HUD name/description via `PickupItem.SetInfo(...)`. Holds destination: `DestinationId`, `DestinationLabel`; `Reward`, `IsDelivered`, `MarkDelivered()`, `Initialize(def, houseId, address)`. ONE prefab serves all three sizes. |
| `PickupItem.SetInfo(name, description, weight)` | Runtime override of the displayed info (used by `Package`). |
| `HouseId` | On every house. Unique `Id` + `Address` + optional `deliveryPoint`. Static registry `HouseId.All`, `HouseId.TryGet(id, out house)`, duplicate-ID warning, gizmo label in the Scene view. |
| `DailyBatchManager` | One in the scene (singleton `Instance`). Weighted list of package types, min/max packages, deterministic per-day seed. `StartDay(day)` builds `Assignments` (`PackageAssignment`: definition, houseId, address, collected, delivered). `MarkCollected()`, `RemainingCount`, `Day`, `IsCollected`, `BatchChanged` event. Context-menu "Debug: Start Next Day". |
| `PackageCounter` | `IInteractable` on the warehouse counter. Optional 3D TMP board listing the day's deliveries. Interact = collect the whole batch: spawns every package (from `Package` prefab) at spawn points / in front of the counter, then `MarkCollected()`. |

Flow: `DailyBatchManager` picks houses + types → board shows them → player presses E at counter → physical packages spawn → player carries them ONE AT A TIME with `ItemCarrier` (delivery point is Milestone 03).

---

## 6. Conventions to Keep Using

- `sealed` classes where inheritance isn't needed; `[SerializeField] private` fields; public read-only properties.
- **ScriptableObjects for data** (items, packages, bills, vehicles). Compare assets by reference, never by name. Never modify SO values at runtime.
- Cross-system messages to the player go through `Notifier.Show(...)`.
- Freeze the player only through `PlayerInputLock`, never by toggling `FppPlayerController` directly.
- New UI is built through `UIFactory` so it shares the font and style. Screen-space HUD canvases use sorting order: HUD 10, Inventory 20 (use 30+ for phone, menus).
- Placeholder audio uses `ProceduralSfx` until real clips exist.
- Use the **new Input System** only (`Keyboard.current`, `Gamepad.current`, never `UnityEngine.Input`).
- Time-based code must be ready for a future `TimeManager` (use scaled time for gameplay, `unscaledDeltaTime` for UI/inspect).
- Keep systems loosely coupled with C# events (like `Inventory.Changed`, `HeadBob.Stepped`).

## 7. Known Limitations / Notes

- Inspect and inventory freeze the player by **disabling `FppPlayerController`** (movement and look both stop). Only allowed while grounded.
- Esc pressed to close inspect/inventory can also unlock the cursor on the same frame (minor, click to re-lock).
- Highlight is an emission glow (not a true outline); unlit materials won't glow.
- TextMeshPro Essentials must be imported (Window → TextMeshPro → Import TMP Essential Resources).
- Only ONE item can be carried at a time. Delivery batches therefore need either a container (box/trolley/van) or a "one package at a time" rule. **Decide this at Milestone 03.**
- Placeholder: `PickupItem` has no "destination" concept yet; packages need a `PackageDefinition` + House ID (Milestone 02/03).

## 8. Proposed Architecture for Upcoming Systems (suggestions, confirm before building)

> Already BUILT (see 5.7): `PackageDefinition`, `Package`, `HouseId`, `DailyBatchManager`, `PackageCounter`. Remaining below are still proposals.

- **`PackageDefinition` (ScriptableObject):** size (Small/Medium/Large), weight, base reward, prefab, display name. Fixed reward per size.
- **`Package` (MonoBehaviour, on the prefab):** runtime data: definition + destination `HouseId` + `delivered` flag. Works with the existing `PickupItem`/`ItemCarrier`.
- **`HouseId`:** unique string/int per house, validated for duplicates in the editor.
- **`DeliveryPoint` (IInteractable or trigger):** at each door/porch; checks held package's destination vs own `HouseId`; pays once, marks delivered (prevents duplicate payment).
- **`DailyBatchManager`:** generates the day's assigned packages (list of definition + house) and spawns them at the counter.
- **`EconomyManager`:** wallet, `Earn`, `TrySpend`, debt; fires `BalanceChanged` event; used by rewards, bills and rental.
- **`TimeManager`:** in-game clock, multiplier, day counter, sunrise/sunset lighting, events `HourChanged`, `DayStarted`, `DayEnded`.
- **`PlayerEnergy`:** drains over time, extra while carrying (read `ItemCarrier.IsHolding`/weight), low-energy effects, restored by sleeping.
- **`BillsManager`:** weekly cycle (rent, electricity, vehicle rental), auto-pay, debt.
- **Save system (later):** every manager exposes its state through a simple serializable data class so save/load stays easy.

---

## 9. How to Resume (instructions for Claude)

1. Read this file and `ROADMAP_PROGRESS.md` fully.
2. Find the **Current Step** in `ROADMAP_PROGRESS.md`.
3. Ask the user ONLY if their status is unclear; otherwise start the current step.
4. Work **one task at a time**: give a short goal, the scripts (as files) and a numbered Unity Editor guide, plus a "how to test" checklist.
5. Wait for the user to confirm the task works before moving to the next one.
6. When a task is confirmed, tell the user exactly what to tick in `ROADMAP_PROGRESS.md` and which lines to add/change here in `PROJECT_CONTEXT.md` (new scripts, new conventions, new known issues).
7. Re-use existing systems (`PickupItem`, `ItemCarrier`, `Notifier`, `UIFactory`, `ItemData`...) instead of writing parallel ones.
8. Keep answers focused for a solo developer: small, polished, testable steps.

## 10. How to Update This File

After each completed task add: new scripts (name, role, public API), new components on the player, new conventions, and any new known limitations. Keep sections 5, 6, 7, 8 current. Remove items from section 8 once they are built and move them into section 5.