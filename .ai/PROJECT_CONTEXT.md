# PROJECT_CONTEXT.md — First-Person Delivery Life Simulator

> **Purpose of this file:** Give this file (plus `ROADMAP_PROGRESS.md`) to Claude at the start of any new chat.
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
3. Collect the assigned batch of packages from the warehouse counter (opening hours 7:00 AM – 6:00 PM). Earn **daily attendance salary ($20)** upon collecting.
4. Deliver each package to its correct house (matching House ID). Fixed pay per package (Small $5, Medium $8, Large $12).
5. When all packages of the day are delivered, earn **completion bonus ($15)**.
6. Energy drops gradually while working (more when carrying, more when sprinting). Sleeping in the bed restores energy and advances to the next morning.
7. Pay **weekly** house rent, electricity bill and vehicle rental fee. Unpaid amounts become **debt**.
8. Eventually rent a car or van from the rental office to deliver faster.

## 3. Design Pillars and Scope Rules

- **Small but detailed town**, satisfying first-person interactions, believable daily routines, polished core gameplay.
- **NOT** a huge open world. **NOT** a complicated business-management sim.
- **Physical hand-carrying over menus:** packages and items are carried, inspected and placed with hands (`ItemCarrier`). No RPG backpack grids; modern information (deliveries, map, bills) lives on the upcoming Smartphone.
- Must stay achievable for a **solo developer**: prefer simple, reusable systems over complex ones.
- Final goal: complete, optimized, enjoyable, worth buying on Steam.

## 4. Technical Environment

| Item | Value |
|---|---|
| Engine | Unity 6 (`6000.3.23f1`) |
| Render pipeline | Universal Render Pipeline (URP) |
| Input | **New Input System** (`UnityEngine.InputSystem`), keyboard + mouse + gamepad |
| UI | **TextMeshPro**, authored in the Unity Editor under `Main Canvas` with Inspector drag-and-drop references |
| Font | Lilita One (TMP Font Asset) |
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
├─ Manager                  (DailyBatchManager, EconomyManager, TimeManager, DayNightCycle, DailyRewards, BillsManager)
├─ PackageCounter           (+ Spawn Point 1..3 children, Board TMP text)
├─ House_01..House_06       (HouseId + DeliveryPoint, optional PlacePoint child on porch)
├─ Home                     (grey-box room: Floor, 5 wall cubes with a 2-unit doorway, Roof)
│   ├─ Bed                  (grey-box cube, layer Default, Box Collider + BedInteractable)
│   └─ BillsDesk            (cube, Box Collider + BillsDesk)
├─ RentalOffice             (grey-box room, Counter with RentalOffice, ParkingSpot trigger area)
├─ Main Canvas              (Screen Space - Overlay, CanvasScaler 2560x1440 Match 0.5)
│   ├─ Status_Panel         (MoneyText, MoneyChangeText, DayText, DeliveriesText, BillsText, EnergyBar_Fill, EnergyText)
│   ├─ Interaction_Panel    (Crosshair, RingBack, RingFill, PromptText, ToastText, HintsText, Inspect_SubPanel)
│   └─ DayTransition_Panel  (BlackScreen, TextContainer: DayTitleText, DaySubtitleText)
└─ EventSystem
```

The player and View Pivot are on the **Ignore Raycast** layer so interaction rays never hit the player. Everything interactable must be on **Default**.

---

## 5. Script Folder Structure (ALWAYS follow this)

All scripts live in `Assets/FPP/Scripts/<SubFolder>/`. **Every time a script is delivered it must state its target folder.**
Move files by **dragging inside the Unity Project window** (never in Explorer/Finder) so `.meta` links and Inspector references stay intact. To replace a script, overwrite the file with the same name in the same folder.

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
| `Energy/` | `PlayerEnergy.cs`, `BedInteractable.cs` |
| `Bills/` | `BillsManager.cs`, `BillsDesk.cs` |
| `Vehicles/` | `RentalOffice.cs`, `DeliveryVehicle.cs`, `VehicleCargo.cs` |

**Planned folders (create when the system is built):**

| Folder (under `Assets/FPP/Scripts/`) | Future scripts |
|---|---|
| `Phone/` | smartphone UI, map, markers |
| `Save/` | save/load system |
| `Menus/` | main menu, pause menu, settings |

**Data / prefab folders:** `Assets/FPP/Data/Items` (ItemData), `Assets/FPP/Data/Packages` (Package_Small / Medium / Large), `Assets/FPP/Prefabs/` (Package, Houses), `Scenes`, `Materials`, `Fonts/Lilita_One`.

---

## 6. Existing Code

### 6.1 Player (`Player/`)
- **`FppPlayerController`** — user's original script. Sealed, `[RequireComponent(CharacterController)]`. Walk / sprint / jump / crouch, mouse + gamepad look, cursor lock (Esc unlocks, click re-locks). Public: `IsGrounded`, `IsCrouched`, `CurrentSpeed`, `ViewPivot`, `EnableSprinting` (setter used by `PlayerEnergy`). Walk 5, sprint 8, crouch 2.5.
- **`PlayerInputLock`** — lets several systems freeze the player safely. `Lock(owner)` / `Unlock(owner)`, `IsLocked`. Refuses to lock while airborne. Auto-added at runtime.
- **`HeadBob`** — bobs the camera. Landing dip. Events `Stepped`, `Landed`. Execution order -100.
- **`FootstepPlayer`** — step/landing sounds from `HeadBob` events.

### 6.2 Interaction (`Interaction/`)
| Script | Role |
|---|---|
| `IInteractable` (+ `IHoldInteractable`) | `InteractionPrompt`, `CanInteract(interactor)`, `Interact(interactor)`. Hold variant adds `HoldDuration`. `CanInteract == false` hides the prompt; to show a "closed" style prompt return true and refuse inside `Interact`. |
| `PlayerInteractor` | On player. Spherecast from camera centre (distance 3). Tap + hold interaction. Interact = **E** / gamepad West. Stays active while carrying. |
| `InteractableObject` | Generic tap interactable with `UnityEvent`, optional single use + cooldown. |
| `HoldInteractable` | Hold-to-interact with progress ring. |
| `InteractableDoor` | Swinging door, optional lock + `ItemData requiredKey`. |
| `InteractableHighlight` | Emission glow on the looked-at object. |
| `PickupItem` | On any pickup (Collider + Rigidbody). `SetInfo`, `SetPickupEnabled`, `OnPickedUp`, `OnReleased`. |

### 6.3 Items (`Items/`)
- **`ItemData`** (ScriptableObject): `Id`, `DisplayName`, `Description`, `Icon`, `Weight`, `Mode` (Carry only), `WorldPrefab`.
- **`ItemCarrier`**: on player, holds ONE `PickupItem`. Inspect (F / right mouse / gamepad Y), Drop Q, Throw left mouse. Public: `Pick`, `DetachHeld`, `IsHolding`, `IsInspecting`, `HeldItem`.

### 6.4 Delivery (`Delivery/`)
| Script | Role |
|---|---|
| `PackageDefinition` (SO) | Per package TYPE: `DisplayName`, `Size`, `Description`, `Weight`, `Reward`, `Dimensions`, `Tint`, `Prefab`. Small 1.5 kg / $5, Medium 4 kg / $8, Large 9 kg / $12. |
| `Package` | On the package prefab. Destination (`DestinationId`, `DestinationLabel`), `Reward`, `IsDelivered`, `MarkDelivered()`, `Initialize(def, houseId, address)`. |
| `HouseId` | On every house. Unique `Id` + `Address`. Static registry `HouseId.All`, `TryGet`. |
| `DailyBatchManager` | Singleton on `Manager`. Per-day deterministic batch. Events `BatchChanged`, `BatchCollected` (salary), `BatchCompleted` (bonus). Subscribes to `TimeManager.DayStarted`. |
| `PackageCounter` | Warehouse counter. Opening hours (7 AM – 6 PM), board, collect-all spawn, returns leftovers on new day. |
| `DeliveryPoint` | On houses. Validates destination, pays once, updates batch, places package on porch (destroyed after 120 s). |

### 6.5 Economy (`Economy/`)
- **`EconomyManager`**: singleton on `Manager`. `Balance`, `Earn`, `TrySpend`, `CanAfford`, `SetBalance`. Events `BalanceChanged`, `TransactionMade`. Debt NOT implemented yet (Milestone 07).
- **`DailyRewards`**: `attendanceSalary` $20 on `BatchCollected`; `completionBonus` $15 on `BatchCompleted`.

### 6.6 Time (`Time/`)
- **`TimeManager`**: singleton (order -50). `TimeOfDay`, `Hour`, `Minute`, `Day`, `IsPastMidnight`, `Paused`. 1 game minute per real second, wake at 7:00 AM, 12-hour clock. Events `HourChanged`, `DayEnded`, `DayStarted`. `RequestNextDay(reason)` → fade via `DayTransition` → `CompleteNextDay()`. Pass-out at 2:00 AM. `IsBetween(open, close)`, `Format(hours)`, `FormatTime()`. Debug context menus (+1 hour, jump to 5:55 PM, jump to 1:55 AM, next morning).
- **`DayNightCycle`**: rotates the Directional Light by time, tints, scales sun intensity, ambient and skybox exposure. URP compatible.

### 6.7 UI (`UI/`)
All UI is authored under **`Main Canvas`** with serialized drag-and-drop references (fallback to `UIFactory` if unassigned).
- **`UIFactory`**: static helpers; `DefaultFont`.
- **`InteractionHud`**: crosshair, prompt, hold ring, hints, queued toasts, inspect card.
- **`StatusHud`**: money counter, floating `+$`/`-$`, `Day N   7:00 AM`, `Deliveries left`, energy bar (`energyBarFill`, `energyText`, `energyGradient`), bills line (`billsText`: next bill day, amount, red debt line).
- **`DayTransition`**: black fade + "Day N" + reason, freezes the player and pauses the clock. Timing 1 s / 2 s / 1.2 s.

### 6.8 Core (`Core/`)
- **`Notifier.Show("text")`** for ALL player messages. **`InputGlyphs`** for key badges. **`GameAudio`** + **`ProceduralSfx`** placeholder sounds.

### 6.9 Energy (`Energy/`) — CONFIRMED WORKING
- **`PlayerEnergy`**: singleton on `FPP Player`. `Energy` (0–100), `EnergyNormalized`. Drain per game-hour (`energyDrainPerHour` 8) + carry drain by held weight (`carryDrainPerKgPerHour`) + sprint multiplier (`sprintDrainMultiplier`). At ≤ 30%: warning toast and sprinting disabled. At 0: collapse, `TimeManager.RequestNextDay("You collapsed from exhaustion")`, wake with `passOutWakeEnergy` 60. Full restore on a planned sleep via `MarkPlannedSleep()`. Debug: Set Energy to 5% / 0%.
- **`BedInteractable`**: on the `Bed`. Prompt shown always; sleeping allowed after `earliestSleepHour` (18) or when energy ≤ 30% (`allowSleepIfLowEnergy`). Interact → `MarkPlannedSleep()` → `TimeManager.RequestNextDay("Good night...")`. Before that it refuses with "too early to sleep".

### 6.9b Bills (`Bills/`) — CONFIRMED WORKING
- **`BillsManager`**: singleton on `Manager`. `firstBillDay` 8, `billingIntervalDays` 7, `weeklyRent` 70, `weeklyElectricity` 15. Subscribes to `TimeManager.DayStarted`; on a billing day it first tries to pay old debt, then pays Rent and Electricity through `EconomyManager.TrySpend`. Anything unaffordable is added to `Debt` (red toast). Public: `Debt`, `WeeklyRent`, `WeeklyElectricity`, `WeeklyTotal`, `NextBillDay`, `DaysUntilNextBill`, `ChargeBills()`, `PayDebt()` (pays what the player can afford, returns amount), `SetDebt()` (for saves). Events `DebtChanged(int)`, `BillsProcessed`. Debug: Charge Bills Now, Add $50 Debt. **Debt lives here, not in `EconomyManager`.** Vehicle rental (Milestone 08) must add its weekly fee here.
- **`BillsDesk`**: `IInteractable` on the desk at home. Prompt shows debt or the next bill day; E pays as much debt as the player can afford.

### 6.9c Vehicles (`Vehicles/`) — CONFIRMED WORKING
- **`RentalOffice`**: `IInteractable` on the rental `Counter`. `vehicleName`, `deposit` (100), `weeklyFee` (40), `parkingSpot`, `vehiclePrefab`. Renting needs no debt and enough money: deposit via `EconomyManager.TrySpend`, `BillsManager.SetVehicleRental(weeklyFee)`, vehicle spawns on the parking spot. Returning: the vehicle must be inside the `ParkingSpot` trigger area and its cargo bed must be empty; deposit is refunded and the weekly fee stops. `IsRented`.
- **`DeliveryVehicle`**: on the root of the truck prefab (Rigidbody, forward = +Z; children `DriverCamera` and `ExitPoint` found by name). `IInteractable`: E to get in (hands must be empty). Arcade driving (no wheel colliders): W/S throttle and brake/reverse, A/D steer, Space handbrake, E to get out. Enter: `PlayerInputLock.Lock`, player's `CharacterController` disabled, player parented to the truck. Exit: unparent, move to `ExitPoint`, re-enable controller, unlock. Removes friction from the truck's own colliders (physics material) so it never sticks to the ground. The truck's wheel meshes must have NO colliders.
- **`VehicleCargo`**: on the `CargoBed` child of the truck (Box Collider, not trigger, covering the open bed). Grid of spots (`columns` x `rows`, measured in the truck root's own axes, so the object's own scale/rotation does not matter). Holding a package + E: loads it into the next free spot, fixed in place (kinematic, collisions with the truck ignored), visible while driving. Hands empty, look at a package + E: takes that package out (prompt shows its address). `Count`, `Capacity`. Packages undelivered at the new morning are destroyed by `PackageCounter` as usual.

### 6.10 Components on FPP Player
`CharacterController`, `FppPlayerController`, `PlayerInteractor`, `ItemCarrier`, `InteractionHud`, `HeadBob`, `FootstepPlayer`, `StatusHud`, `DayTransition`, `PlayerEnergy` (+ `PlayerInputLock` auto-added).

### 6.11 Components on Manager
`EconomyManager`, `DailyBatchManager`, `TimeManager`, `DailyRewards`, `DayNightCycle`, `BillsManager`.

---

## 7. Conventions to Keep Using

- `sealed` classes where inheritance isn't needed; `[SerializeField] private` fields; public read-only properties.
- **ScriptableObjects for data** (items, packages, bills, vehicles). Compare by reference. Never modify SO values at runtime.
- Player messages through `Notifier.Show(...)`. All money changes through `EconomyManager`.
- Freeze the player only through `PlayerInputLock`.
- Managers are scene singletons with static `Instance` and C# events. Subscribe in `Start`, unsubscribe in `OnDestroy`.
- Daily things listen to `TimeManager.DayStarted` / `DayEnded`; hourly things to `HourChanged`. New morning only through `TimeManager.RequestNextDay(reason)`.
- Gameplay uses scaled `Time.deltaTime`; UI and transitions use `Time.unscaledDeltaTime`. Freeze game time with `TimeManager.Paused`.
- UI is authored in the Editor inside `Main Canvas` and linked by drag-and-drop.
- Placeholder audio via `ProceduralSfx` until real clips exist.
- **New Input System only** (`Keyboard.current`, `Gamepad.current`).
- New scripts go in the correct `Assets/FPP/Scripts/<SubFolder>/`.
- **Write code in chat only** — do NOT use write_file / edit_file for scripts (user's request to save tokens). The `.md` files are still rewritten as files and sent.

---

## 8. Known Limitations / Notes

- Inspect and day transition freeze the player by disabling `FppPlayerController` (only while grounded).
- `ItemCarrier` holds ONE item; packages are carried one at a time.
- Two ways to end a day: sleeping in the bed (full energy) or collapsing (0% energy, wake with 60) / passing out at 2:00 AM. No money penalty yet.
- `DailyBatchManager` needs `HouseId` objects in the scene before `Start`.
- Delivered packages stay on the porch 120 s; undelivered ones are destroyed at the next morning.
- **Confirmed working in Unity (user tests through 2026-11-08):** movement, interaction, carry/inspect/drop, warehouse counter + opening hours, delivery loop, wallet, salary, bonus, clock, day/night, day transition, authored UI panels, energy drain, carry/sprint drain, low-energy sprint lock, bed (too early refusal, sleep after 6 PM, wake at 7:00 AM with full energy), collapse (wake with 60%).
- Weekly bills (Day 8, 15, 22...), rent/electricity auto-payment, debt, HUD bills line and the BillsDesk were tested and confirmed (2026-11-08).
- Vehicle rental, deposit, weekly fee on the bills line, driving, enter/exit, parking check, loading and unloading packages in the truck bed, and the full vehicle delivery route were tested and confirmed (2026-11-09).
- The home, rental office and truck bed are grey-box / placeholder; real art comes with Milestone 10.
- The truck has no mouse look while driving and no engine sound yet (Milestone 11 polish).
- Debt has no extra penalty yet (no interest, no eviction). Possible later polish in Milestone 11.

## 9. Proposed Architecture for Upcoming Systems

- **Milestone 11 (planned, see roadmap):** package fragility and damage, customer doorbell interactions, delivery instructions, failed deliveries, home upgrades (beds). These will extend `Package`, `PackageDefinition`, `DeliveryPoint` and `DailyBatchManager`.
- **`Smartphone` (Milestone 09):** deliveries list, map, wallet, bills view.
- **Save system (Milestone 12):** managers expose state through serializable data classes.

---

## 10. How to Resume

1. Read this file and `ROADMAP_PROGRESS.md` fully.
2. Find the **Current Step** in `ROADMAP_PROGRESS.md`.
3. Guide the Unity setup ONE small step at a time (wait for user confirmation before moving on).
4. For every script delivered, state its target folder.
5. After each major step is done, **REWRITE BOTH `.md` FILES IN FULL**.
6. **Write scripts in chat only.**