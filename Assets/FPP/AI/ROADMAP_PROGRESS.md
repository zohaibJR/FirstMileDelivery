# ROADMAP_PROGRESS.md — Delivery Life Simulator

> Give this file together with `PROJECT_CONTEXT.md` to Claude in a new chat.
> Claude continues from the **Current Step** below, one task at a time.

## Status Legend
- `[ ]` not started
- `[~]` code written / in progress, **waiting for the user to confirm it works**
- `[x]` done and confirmed in Unity
- `[-]` skipped on purpose

## Working Rules
1. Only ONE task is active at a time (the **Current Step**).
2. A task is finished only when the user confirms it works in Unity. Then tick it here and move on.
3. Each task is delivered as: short goal → script files → numbered Unity setup → test checklist.
4. If the chat limit is reached, the user starts a new chat, pastes both `.md` files, and says: **"Continue from the current step."**
5. After finishing a task, update this file (ticks + session log) and `PROJECT_CONTEXT.md` (new scripts/conventions).

---

## CURRENT STEP

**Milestone 02, Tasks 2.1 to 2.8 — warehouse, counter, package types, daily assignment, board display, collect-all. TESTING PHASE (user tests everything at the end).**
Code written: `PackageDefinition.cs`, `Package.cs`, `HouseId.cs`, `DailyBatchManager.cs`, `PackageCounter.cs`, small edit to `PickupItem.cs`. **Waiting for the user to build the grey-box warehouse and confirm everything works in Unity.**
Next after testing: Milestone 03 (delivery point, validate destination, pay once, remaining counter). Packages are carried ONE AT A TIME for now (decision log). Open question for the user: keep one-at-a-time, or add a backpack/trolley?
(Tasks 1.1 and 1.2 were skipped by choice. Gameplay is tested in `FPP Scene` until MainTown is made.)

> Update this line every time a task is completed.

---

## Overall Progress

| Milestone | Time estimate | Done |
|---|---|---|
| 01 Project Foundation | 1–2 days | 0/7 |
| 02 Warehouse System | 3–5 days | 0/8 |
| 03 Package Carrying and Delivery | 4–6 days | 0/10 |
| 04 Economy and Work Rewards | 2–4 days | 0/8 |
| 05 Time and Day/Night Cycle | 3–5 days | 0/9 |
| 06 Energy and Sleep | 3–4 days | 0/10 |
| 07 Home and Weekly Bills | 3–5 days | 0/10 |
| 08 Rental Vehicle System | 5–8 days | 0/11 |
| 09 Smartphone and Navigation | 3–5 days | 0/9 |
| 10 Town and Environment | 2–4 weeks | 0/10 |
| 11 Gameplay Polish and Progression | 2–4 weeks | 0/10 |
| 12 Steam Release Preparation | 3–6 weeks | 0/13 |

Suggested play order: build gameplay systems (01 to 09) in a grey-box test scene first, then dress the town (10), polish (11), release prep (12).

---

## Milestone 01 — Project Foundation (1–2 days)
**Done when:** project is organized and under version control, `MainTown` scene exists, and the player can walk and interact with a test object.

> Notes: a first-person controller (`FppPlayerController`) and a full interaction system already exist (see PROJECT_CONTEXT.md section 5). Tasks 1.4 to 1.7 are mostly **verification** in the new MainTown scene.

- [-] 1.1 Organize Unity project folders *(skipped for now)*
- [-] 1.2 Set up Git version control and create a backup *(skipped for now)*
- [ ] 1.3 Create the MainTown scene
- [ ] 1.4 Verify first-person player movement
- [~] 1.5 Implement or verify player interaction system *(scripts written: PlayerInteractor, IInteractable, ItemCarrier, etc.; verify in MainTown)*
- [~] 1.6 Create reusable interaction prompts *(InteractionHud prompt + hold ring + toasts written; verify)*
- [ ] 1.7 Test player movement and interaction

## Milestone 02 — Warehouse System (3–5 days)
**Done when:** each morning a batch of packages is assigned, shown at the counter, and the player can collect the whole batch.

- [~] 2.1 Create warehouse interior *(grey-box guide given; user builds in Unity)*
- [~] 2.2 Create package pickup counter *(grey-box guide given; `PackageCounter` script written)*
- [~] 2.3 Create `PackageDefinition` ScriptableObject
- [~] 2.4 Create three package types: small, medium and large
- [~] 2.5 Create daily package assignment system *(`DailyBatchManager`)*
- [~] 2.6 Display assigned packages at counter *(3D TMP board in `PackageCounter`)*
- [~] 2.7 Implement collect-all-batch interaction *(`PackageCounter.Interact` spawns the batch)*
- [ ] 2.8 Test package spawning and collection

## Milestone 03 — Package Carrying and Delivery (4–6 days)
**Done when:** the player can take a batch from the warehouse to the correct houses, get paid once per package, and the remaining counter updates.

> Decision needed here: how does the player carry a batch (one package at a time, a box/trolley, or a hand-held stack)? Existing `ItemCarrier` holds ONE item.

- [ ] 3.1 Create package pickup interaction
- [ ] 3.2 Attach package to player carrying position
- [ ] 3.3 Implement package placement
- [~] 3.4 Create unique House IDs *(`HouseId` script written early because the daily assignment needs it)*
- [ ] 3.5 Create delivery point interaction
- [ ] 3.6 Validate package destination against House ID
- [ ] 3.7 Implement successful delivery
- [ ] 3.8 Prevent duplicate delivery payments
- [ ] 3.9 Update remaining delivery counter
- [ ] 3.10 Test complete warehouse-to-house delivery loop

## Milestone 04 — Economy and Work Rewards (2–4 days)
**Done when:** money is earned from packages, daily salary and completion bonus, and shown on the HUD with payment notifications.

- [ ] 4.1 Create `EconomyManager`
- [ ] 4.2 Implement player wallet
- [ ] 4.3 Implement fixed package rewards
- [ ] 4.4 Implement daily attendance salary
- [ ] 4.5 Implement daily completion bonus
- [ ] 4.6 Create money HUD
- [ ] 4.7 Create payment notifications
- [ ] 4.8 Test all income calculations

## Milestone 05 — Time and Day/Night Cycle (3–5 days)
**Done when:** the clock runs, the sun follows it, the warehouse has opening hours, and days advance correctly.

- [ ] 5.1 Create `TimeManager`
- [ ] 5.2 Implement in-game clock
- [ ] 5.3 Implement game-time multiplier
- [ ] 5.4 Connect clock to lighting
- [ ] 5.5 Implement sunrise and sunset
- [ ] 5.6 Create day counter
- [ ] 5.7 Set warehouse opening and closing hours
- [ ] 5.8 Implement next-day transition
- [ ] 5.9 Test day and night progression

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
| 2026-11-06 | Built interaction system, pickup/drop/inspect carrier, inventory, ItemData, HUD, head bob, footsteps. Created these two .md files. | Milestone 01, Task 1.1 |
| 2026-11-06 | Skipped Git and folder setup. Wrote `PackageDefinition`, `Package`, and `PickupItem.SetInfo`. | Confirm 2.3 + 2.4, then 2.5 |
| 2026-11-06 | Wrote `HouseId`, `DailyBatchManager`, `PackageCounter` (tasks 2.5 to 2.7, plus 3.4). Updated both .md files. | User tests all of Milestone 02, then Milestone 03 |

## Decisions Log (add answers here as they are made)

| Question | Decision |
|---|---|
| Carry a whole batch how? (one at a time / box / trolley / stack) | *undecided, needed at Milestone 03* |
| Weekly bill day (which in-game day of the week)? | *undecided, needed at Milestone 07* |
| Day length in real minutes (time multiplier)? | *undecided, needed at Milestone 05* |
| Unity version / render pipeline | *fill in* |

---

## Prompt to Paste in a New Chat

```
I am developing a first-person package delivery simulator in Unity.
Attached are PROJECT_CONTEXT.md and ROADMAP_PROGRESS.md.
Read both, then continue from the CURRENT STEP in ROADMAP_PROGRESS.md.
Work one task at a time: give me the scripts as files and a numbered Unity guide,
and wait until I confirm it works before moving to the next task.
```