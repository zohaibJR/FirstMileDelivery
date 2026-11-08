# ROADMAP_PROGRESS.md — Delivery Life Simulator

> Give this file together with `PROJECT_CONTEXT.md` to Claude / Antigravity in a new chat.
> Continues from the **Current Step** below.

## Status Legend
- `[ ]` not started
- `[~]` code written / in progress, waiting for user to confirm it works
- `[x]` done and confirmed in Unity
- `[-]` skipped on purpose

## Working Rules
1. Work happens in **major steps** (one task, or a batch of related tasks tested together).
2. A task is finished only when the user confirms it works in Unity. Then it is ticked here and the next step starts.
3. Each step is delivered as: short goal → script files (with their target folder) → Unity setup guided **one small step at a time** (the user replies "done", then gets the next step) → test checklist.
4. **Every script delivery states the target folder** (`Assets/FPP/Scripts/<SubFolder>/`, see PROJECT_CONTEXT.md section 5). Move files by dragging inside the Unity Project window.
5. **After every major step, Claude rewrites BOTH `.md` files in full** and sends them.
6. If the chat limit is reached, the user starts a new chat, attaches both `.md` files, and says: **"Continue from the current step."**

---

## CURRENT STEP

**Milestone 06 — Energy and Sleep.**

Milestones 01 (partly), 02, 03, 04, and 05 were **tested by the user on 2026-11-08 and all passed.**
In-editor Canvas Panels (`Status_Panel`, `Interaction_Panel`, `DayTransition_Panel`) have been created, referenced via Inspector drag-and-drop, and verified.
Inventory system has been removed in favor of pure physical hand-carrying (`ItemCarrier`) and the upcoming Smartphone (Milestone 09).

Tasks to be tackled in Milestone 06:
- 6.1 Create `PlayerEnergy` system (`Assets/FPP/Scripts/Energy/PlayerEnergy.cs`)
- 6.2 Implement gradual energy consumption over the day
- 6.3 Add additional energy consumption when carrying packages
- 6.4 Implement energy HUD (energy bar / icon inside `Status_Panel`)
- 6.5 Add low-energy movement effects (slower walk/sprint, heavy breathing/vignette)
- 6.6 Create home bedroom and bed interaction (`Assets/FPP/Scripts/Energy/BedInteractable.cs`)
- 6.7 Implement sleep interaction
- 6.8 Restore energy after sleeping
- 6.9 Advance time to next morning (calls `TimeManager.RequestNextDay`)
- 6.10 Test complete daily routine with energy & sleep

---

## Overall Progress

| Milestone | Time estimate | Done |
|---|---|---|
| 01 Project Foundation | 1–2 days | 4/7 *(2 skipped, only 1.3 MainTown scene left)* |
| 02 Warehouse System | 3–5 days | 6/8 *(2.1 interior and 2.6 board need your confirmation)* |
| 03 Package Carrying and Delivery | 4–6 days | 10/10 |
| 04 Economy and Work Rewards | 2–4 days | 8/8 |
| 05 Time and Day/Night Cycle | 3–5 days | 9/9 |
| 06 Energy and Sleep | 3–4 days | 0/10 *(Current step)* |
| 07 Home and Weekly Bills | 3–5 days | 0/10 |
| 08 Rental Vehicle System | 5–8 days | 0/11 |
| 09 Smartphone and Navigation | 3–5 days | 0/9 |
| 10 Town and Environment | 2–4 weeks | 0/10 |
| 11 Gameplay Polish and Progression | 2–4 weeks | 0/10 |
| 12 Steam Release Preparation | 3–6 weeks | 0/13 |

---

## Milestone 01 — Project Foundation (1–2 days)
**Done when:** `MainTown` scene exists and the player can walk and interact with a test object.

- [-] 1.1 Organize Unity project folders *(skipped for now; only the script folder structure in PROJECT_CONTEXT.md section 5 is used)*
- [-] 1.2 Set up Git version control and create a backup *(skipped for now)*
- [ ] 1.3 Create the MainTown scene
- [x] 1.4 Verify first-person player movement
- [x] 1.5 Implement or verify player interaction system
- [x] 1.6 Create reusable interaction prompts
- [x] 1.7 Test player movement and interaction

## Milestone 02 — Warehouse System (3–5 days)
**Done when:** each morning a batch of packages is assigned, shown at the counter, and the player can collect the whole batch.

- [~] 2.1 Create warehouse interior *(grey-box counter built; real interior comes with Milestone 10)*
- [x] 2.2 Create package pickup counter
- [x] 2.3 Create `PackageDefinition` ScriptableObject
- [x] 2.4 Create three package types: small, medium and large
- [x] 2.5 Create daily package assignment system
- [~] 2.6 Display assigned packages at counter *(TMP board exists; confirm if tested)*
- [x] 2.7 Implement collect-all-batch interaction
- [x] 2.8 Test package spawning and collection

## Milestone 03 — Package Carrying and Delivery (4–6 days)
**Done when:** the player can take a batch from the warehouse to the correct houses, get paid once per package, and the remaining counter updates.

- [x] 3.1 Create package pickup interaction
- [x] 3.2 Attach package to player carrying position
- [x] 3.3 Implement package placement
- [x] 3.4 Create unique House IDs
- [x] 3.5 Create delivery point interaction
- [x] 3.6 Validate package destination against House ID
- [x] 3.7 Implement successful delivery
- [x] 3.8 Prevent duplicate delivery payments
- [x] 3.9 Update remaining delivery counter
- [x] 3.10 Test complete warehouse-to-house delivery loop

## Milestone 04 — Economy and Work Rewards (2–4 days)
**Done when:** money is earned from packages, daily salary and completion bonus, and shown on the HUD with payment notifications.

- [x] 4.1 Create `EconomyManager`
- [x] 4.2 Implement player wallet
- [x] 4.3 Implement fixed package rewards
- [x] 4.4 Implement daily attendance salary *(`DailyRewards`, $20 when batch is collected)*
- [x] 4.5 Implement daily completion bonus *(`DailyRewards`, $15 when all packages are delivered)*
- [x] 4.6 Create money HUD
- [x] 4.7 Create payment notifications
- [x] 4.8 Test all income calculations

## Milestone 05 — Time and Day/Night Cycle (3–5 days)
**Done when:** the clock runs, the sun follows it, the warehouse has opening hours, and days advance correctly.

- [x] 5.1 Create `TimeManager`
- [x] 5.2 Implement in-game clock *(12-hour clock on `StatusHud`)*
- [x] 5.3 Implement game-time multiplier *(`gameMinutesPerRealSecond`, default 1)*
- [x] 5.4 Connect clock to lighting *(`DayNightCycle`)*
- [x] 5.5 Implement sunrise and sunset
- [x] 5.6 Create day counter *(`TimeManager.Day`, shown on `StatusHud`)*
- [x] 5.7 Set warehouse opening and closing hours *(`PackageCounter`, 7 AM to 6 PM)*
- [x] 5.8 Implement next-day transition *(`DayTransition`; leftover package cleanup and pass-out at 2 AM)*
- [x] 5.9 Test day and night progression

## Milestone 06 — Energy and Sleep (3–4 days)
**Done when:** energy drains while working (faster when carrying), the player sleeps in bed, energy refills and the next morning begins.

- [ ] 6.1 Create `PlayerEnergy` system
- [ ] 6.2 Implement gradual energy consumption
- [ ] 6.3 Add additional energy consumption when carrying packages
- [ ] 6.4 Implement energy HUD
- [ ] 6.5 Add low-energy movement effects
- [ ] 6.6 Create home bedroom and bed interaction
- [ ] 6.7 Implement sleep interaction
- [ ] 6.8 Restore energy after sleeping
- [ ] 6.9 Advance time to next morning
- [ ] 6.10 Test complete daily routine

## Milestone 07 — Home and Weekly Bills (3–5 days)
**Done when:** weekly rent and electricity are charged automatically, unpaid amounts become debt, and the player is notified.

- [ ] 7.1 Create player's rented house
- [ ] 7.2 Create weekly billing calendar
- [ ] 7.3 Implement house rent
- [ ] 7.4 Implement electricity bills
- [ ] 7.5 Create `BillsManager`
- [ ] 7.6 Implement weekly automatic payment
- [ ] 7.7 Implement outstanding debt
- [ ] 7.8 Create bill notification UI
- [ ] 7.9 Create debt display
- [ ] 7.10 Test successful and unsuccessful bill payments

## Milestone 08 — Rental Vehicle System (5–8 days)
**Done when:** the player can rent a vehicle, drive it, park it, load/unload packages, and pays weekly rental through the bills system.

- [ ] 8.1 Create vehicle rental office
- [ ] 8.2 Create rental interaction
- [ ] 8.3 Implement rental agreement
- [ ] 8.4 Implement rental deposit
- [ ] 8.5 Implement weekly vehicle rental charge
- [ ] 8.6 Add one drivable vehicle
- [ ] 8.7 Implement enter and exit vehicle
- [ ] 8.8 Implement parking
- [ ] 8.9 Implement package loading and unloading
- [ ] 8.10 Connect vehicle rental to weekly bills
- [ ] 8.11 Test complete vehicle delivery route

## Milestone 09 — Smartphone and Navigation (3–5 days)
**Done when:** the phone shows today's deliveries with addresses, a simple map with markers, and money/energy/debt/bills info.

- [ ] 9.1 Create smartphone UI
- [ ] 9.2 Create daily delivery list
- [ ] 9.3 Display package addresses
- [ ] 9.4 Create simple town map
- [ ] 9.5 Add house location markers
- [ ] 9.6 Add player location marker
- [ ] 9.7 Display money and energy
- [ ] 9.8 Display debt and upcoming bills
- [ ] 9.9 Test navigation and delivery information

## Milestone 10 — Town and Environment (2–4 weeks)
**Done when:** a compact, good-looking and optimized town exists with reusable house prefabs, roads, props, sounds and light pedestrian life.

- [ ] 10.1 Create residential street layout
- [ ] 10.2 Create reusable house prefabs
- [ ] 10.3 Add unique house numbers
- [ ] 10.4 Create warehouse exterior
- [ ] 10.5 Create rental office exterior
- [ ] 10.6 Add roads and sidewalks
- [ ] 10.7 Add streetlights and environmental props
- [ ] 10.8 Add ambient sounds
- [ ] 10.9 Add basic pedestrian activity
- [ ] 10.10 Optimize town performance

## Milestone 11 — Gameplay Polish and Progression (2–4 weeks)
**Done when:** the full progression loop is fun and balanced across many in-game days.

- [ ] 11.1 Add different delivery package sizes
- [ ] 11.2 Add customer doorbell interactions
- [ ] 11.3 Add delivery success and failure feedback
- [ ] 11.4 Add weather variations
- [ ] 11.5 Add home furniture upgrades
- [ ] 11.6 Add vehicle rental options if feasible
- [ ] 11.7 Add delivery achievements
- [ ] 11.8 Balance daily earnings and expenses
- [ ] 11.9 Improve UI animations and audio feedback
- [ ] 11.10 Playtest the complete progression loop

## Milestone 12 — Steam Release Preparation (3–6 weeks)
**Done when:** a stable, optimized release candidate with saves, menus, settings, achievements and store assets is built and tested.

- [ ] 12.1 Implement reliable save and load
- [ ] 12.2 Test saving during active delivery batches
- [ ] 12.3 Add main menu and pause menu
- [ ] 12.4 Add graphics settings
- [ ] 12.5 Add audio settings
- [ ] 12.6 Add key rebinding
- [ ] 12.7 Add controller support if feasible
- [ ] 12.8 Profile and optimize standalone build
- [ ] 12.9 Fix progression-breaking bugs
- [ ] 12.10 Conduct external playtesting
- [ ] 12.11 Prepare Steam achievements and integration
- [ ] 12.12 Create trailer and store screenshots
- [ ] 12.13 Build and test release candidate

---

## Session Log

| Date | What was done | Next step |
|---|---|---|
| 2026-11-06 | Built interaction system, pickup/drop/inspect carrier, inventory, ItemData, HUD, head bob, footsteps. Created the two .md files. | Milestone 01 |
| 2026-11-06 | Skipped Git and folder setup. Wrote `PackageDefinition`, `Package`, `PickupItem.SetInfo`, `HouseId`, `DailyBatchManager`, `PackageCounter`. | User tests Milestone 02 |
| 2026-11-06 | Added script folder structure and the "rewrite both .md files after each major step" rule. Wrote `DeliveryPoint`, `EconomyManager`, `StatusHud`; changed `ItemCarrier`, `PickupItem`, `Package`, `DailyBatchManager`. | User tests the delivery loop |
| 2026-11-08 | Fixed `TryMarkDelivered` compile error. User built full setup (prefab, 3 definitions, 6 houses, counter) and all tests passed. Wrote `TimeManager`, `DayNightCycle`, `DayTransition`, `DailyRewards`; changed `DailyBatchManager`, `PackageCounter`, `DeliveryPoint`, `StatusHud`, `InteractionHud`. | Unity setup for Milestones 04+05 |
| 2026-11-08 | Organized scripts into `Time/`, `Economy/`, `UI/` folders. Added `TimeManager`, `DailyRewards`, and `DayNightCycle` (with Directional Light) to `Manager`. Added `DayTransition` to `FPP Player`. Verified opening hours and full loop. Milestones 04 & 05 completed and confirmed. Rewrote both .md files in full. | UI Authoring & Milestone 06 |
| 2026-11-08 | Converted UI architecture from procedural code to in-editor `Main Canvas` with Panels (`Status_Panel`, `Interaction_Panel`, `DayTransition_Panel`) and Inspector drag-and-drop references. Removed traditional RPG `Inventory` and `InventoryPanel` components and scripts; streamlined physical hand-carrying (`ItemCarrier`) and key unlock mechanics. Zero compile errors. Rewrote both .md files in full. | Milestone 06 (Energy and Sleep) |

## Decisions Log

| Question | Decision |
|---|---|
| Carry a whole batch how? | **One at a time.** User may change later. |
| Interaction while carrying a package | **Allowed** (doors, counter, delivery points). Only picking up a second carry item is blocked. |
| Script organisation | `Assets/FPP/Scripts/<SubFolder>/` (see PROJECT_CONTEXT.md section 5) |
| Package visuals | One `Package` prefab; size/colour/weight come from `PackageDefinition` |
| When is a delivery paid? | Instantly at the correct house, fixed reward per size (Small $5, Medium $8, Large $12) |
| Starting money | $50 / $100 |
| Day length | **1 game minute per real second** (24 real minutes = 24 game hours). Adjustable (`gameMinutesPerRealSecond`). |
| Day start / wake-up time | 7:00 AM |
| Warehouse opening hours | 7:00 AM to 6:00 PM (adjustable on `PackageCounter`) |
| Clock format | 12-hour ("7:30 AM"), switchable to 24-hour |
| Daily salary | **$20, paid when the day's batch is collected** at the counter (attendance) |
| Completion bonus | **$15, paid when every package of the day is delivered** |
| Late night rule (temporary) | Passing out at 2:00 AM, next morning starts, undelivered packages are returned. Replaced by energy and sleep in Milestone 06. |
| UI Architecture | Authored in the Unity Editor under `Main Canvas` and Panels with Inspector drag-and-drop references (replaces procedural runtime UI generation). |
| Traditional RPG Inventory | **Removed.** Delivery simulator uses physical hand-carrying (`ItemCarrier`) for packages and items. Modern device functions (deliveries, map, bills) are handled by the upcoming in-game Smartphone (Milestone 09). |
| Weekly bill day (which in-game day of the week)? | *undecided, needed at Milestone 07* |
| Unity version / render pipeline | Unity `6000.3.23f1` / Universal Render Pipeline (URP) |

---

## Prompt to Paste in a New Chat

```
I am developing a first-person package delivery simulator in Unity.
Attached are PROJECT_CONTEXT.md and ROADMAP_PROGRESS.md.
Read both, then continue from the CURRENT STEP in ROADMAP_PROGRESS.md.
Tell me which folder each script goes into, and rewrite both .md files in full after each major step.
Give me the scripts as files, then guide the Unity setup ONE small step at a time (I will say "done" after each step), in simple words, with a test checklist.
```
