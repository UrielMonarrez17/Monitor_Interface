# Quest Checkout

A Unity kiosk for borrowing six Meta Quest 3 kits at CIAC 2. The interface recreates the team's Project 01 Figma PDF: white screens, bold headings, blue controls, kit photographs and the robot mascot. It includes kit selection, 30/60/90/120-minute loans, a contents checklist, an editable review and a saved confirmation with the pickup location and return deadline.

## Run the app

**Windows:** open `Builds/Windows/QuestCheckout.exe` after building or extracting the complete Windows release. Keep the executable, `QuestCheckout_Data`, `UnityPlayer.dll`, and all other build files together. No Unity installation is needed to run the build. The window can be resized; the original design proportions are preserved.

**Unity:** install **Unity 6000.5.7f1** through Unity Hub, including Windows Build Support. Add this repository as a project, allow package import to finish, open `Assets/Scenes/QuestCheckout.unity`, and press Play. If the scene needs to be recreated, choose **Quest Checkout > Create or open checkout scene**.

To create a Windows build, choose **Quest Checkout > Build Windows app**. It runs the logic checks and writes the runnable app to `Builds/Windows/`. Generated builds are excluded from Git; upload the complete build folder as a ZIP in a GitHub Release.

## Edit the Unity Canvas

The UI uses **Unity Canvas / uGUI**, with each element saved separately in the scene and in `Assets/QuestCheckout/Prefabs/QuestCheckoutCanvas.prefab`. It does not generate the interface when Play starts.

Open `Assets/Scenes/QuestCheckout.unity` and expand **Quest Checkout Canvas > Screens - 1200 x 854**. The hierarchy contains Home, QuestKits, QuestUnavailable, GenericKits, Reservation, Review, Success, Waiting and Loading panels, plus the shared Home button, footer and test dialog. Each panel contains individually named text objects, photographs, buttons, checkbox objects and panther images.

To edit a screen without entering Play mode, deactivate Home and activate the desired panel in the Inspector. Select any child to adjust its RectTransform, Image, Text, Button or Toggle. Return Home to active before saving. Runtime navigation chooses which saved panel to display. The Canvas Scaler uses a 1200 × 854 reference resolution and preserves the design proportions.

Select a Button and inspect **Button > On Click ()** to see its saved target and action. The Toggle **On Value Changed** events are also saved in the Inspector. `PantherHover` exposes its artwork, selecting/stop images, zoom and speed. `PantherTransition` exposes its moving image and duration. You can edit the shared prefab in Prefab Mode or keep scene overrides.

The editor-only `CanvasSceneAuthoring` utility performed the initial conversion to Canvas objects. Normal Play and builds use the saved scene/prefab and do not regenerate them, so layout edits are preserved. **Quest Checkout > Validate Canvas scene** checks the screen hierarchy and button wiring.

## Controls and checkout

- Use a mouse or touchscreen. Tab and Shift+Tab move between controls; Enter/Space activate focused buttons and checkboxes.
- Select **Meta Quest 3**, then an available kit. Unavailable kits cannot be selected.
- Select a duration (90 minutes by default). Verify **1 Headset**, **2 Controllers**, and **1 Charger** to enable **Review Reservation**.
- Review the kit, duration, pickup point and estimated deadline. **Edit choices** returns to the form; **Change kit** returns to inventory and clears the contents verification.
- **Confirm Reservation** validates availability again, saves the loan, and shows its exact local return date/time. The home icon returns to the start.
- Hover over a category or kit to enlarge its artwork and show the panther. Available options show the pointing panther; blocked options show the stop pose. The mascot disappears when the pointer leaves.
- Selecting a menu or available kit plays the panther crossing from left to right with Going!! / Loading text.
- Cameras, EEG headsets and Electronics are blocked categories and show the stop panther only on hover.
- **Other Options** shows three generic VR kits. Generic VR 1 starts available; Generic VR 2 and 3 start on loan. Generic checkout uses the same duration/review flow, with 1 headset, 1 controller and 1 charger.

## Reset data and test the two scenarios

Open **Test controls** at the bottom right of any screen. The dialog explains that resetting replaces all saved loans. Cancel leaves inventory unchanged.

**Scenario A:** choose **Reset to A · available kits**. Kits 1, 3, 4 and 5 are available; kits 2 and 6 are on loan, as in the prototype. Choose Kit 1, keep 90 minutes, check all contents, review and confirm. The receipt shows CIAC 2 and a deadline 90 minutes after confirmation. Return to the kits: Kit 1 is now unavailable. Close and reopen the app: it remains unavailable until its deadline.

**Scenario B:** choose **Reset to B · no kits available**. All six kits are unavailable. The screen explains that they are on loan and shows the earliest return from the saved data. **Check next availability** opens a screen that refreshes every 15 seconds and also has a manual refresh button. When a loan expires, it offers **Choose an available kit**. **Other Options** opens the generic VR inventory, where Generic VR 1 can be borrowed as an alternative. Reset to A to restore available kits immediately.

Both scenario resets also restore the generic inventory to one available kit and two on loan. Generic loans are saved separately in `generic-loans.json` beside `quest-loans.json`, without changing the six Quest kits.

Scenario A is initialized on the first launch only. Reset is explicit and is also saved across restarts.

## Saved inventory

Loans are stored locally in `Application.persistentDataPath/quest-loans.json`, normally `%USERPROFILE%/AppData/LocalLow/CIAC/Quest Checkout/quest-loans.json` on Windows. UTC timestamps are saved and displayed in the computer's local time. Writes use a temporary file and replacement; a failed write does not confirm a loan. A `.bak` copy of the previous save is retained. Unreadable saved data blocks checkout and can be replaced through the reset controls.

This is a single-device classroom kiosk. It has no server, account authentication or physical return tracking. A kit becomes available automatically at its deadline. Test controls intentionally remain visible for the demonstration. The pickup label CIAC 2 comes from the prototype and can be changed in `LoanStore.Pickup`. The PDF's multi-day date picker is adapted to immediate, minute-based loans to meet the 90-minute scenario. Advance reservations are not implemented; the unavailable flow uses live availability and alternative generic VR checkout.

## Code and verification

- `Assets/Scenes/QuestCheckout.unity`: runnable scene with the Canvas prefab instance and EventSystem.
- `Assets/QuestCheckout/Prefabs/QuestCheckoutCanvas.prefab`: all editable UI elements and Inspector event connections.
- `Assets/QuestCheckout/QuestCheckoutApp.cs`: navigation and coordination of saved loan data; no runtime UI construction.
- `InventoryScreen.cs` and `KitCardView.cs`: availability labels and kit photographs.
- `ReservationForm.cs`: duration selection, contents checklist and form bindings.
- `CheckoutDetailsView.cs`: review, receipt and next-return text bindings.
- `PantherHover.cs` and `PantherTransition.cs`: independent hover and loading effects.
- `Assets/QuestCheckout/LoanStore.cs`: six-kit inventory, validation and local persistence.
- `Assets/QuestCheckout/Resources/`: artwork extracted from the supplied Figma PDF.
- `Assets/QuestCheckout/Editor/CheckoutBuild.cs`: scene creation, Windows build and automated logic verification.
- `Assets/QuestCheckout/CanvasSmokeTest.cs`: optional runtime verification, enabled only by the `--checkout-smoke` launch argument.

Run **Quest Checkout > Verify checkout logic** to check 90-minute checkout, invalid/unavailable kits, unsupported duration, incomplete contents, edited choices, saved data after restart, expiry, both resets, corrupt saves and write failures. This is developer verification; it does not replace the team's functional testing report or three-participant usability study.

For a separate automated runtime smoke check, launch the built executable with `--checkout-smoke`. It uses `smoke-loans.json` and `smoke-generic-loans.json` (not normal loan data), captures screens into `Builds/Windows/Screenshots/`, exercises Quest and generic checkout/persistence, hover enter/leave states, the moving transition, and the unavailable scenario, then exits. Keep its preview window visible for screenshot capture. The player log records `QUEST_CHECKOUT_SMOKE_PASS` on success.

## Team submission links — complete before submitting

| Item | Team action |
| --- | --- |
| Figma | Add the shared Project 01 Figma link. Visual reference used here: the team's `Untitled.pdf`. |
| Design justification | Add `docs/design.md` and link it here. |
| Testing report | Add `docs/testing.md`, the tested build ID and real evidence, then link it here. |
| AI-use record | Add `docs/ai-use.md` with this implementation assistance and the team's verification. |
| Windows Release | Publish the complete build ZIP to [GitHub Releases](https://github.com/UrielMonarrez17/Monitor_Interface/releases), then add the specific release link. |
| Demo video | Add a link to the video (maximum five minutes; everyone speaks). |

| Team member | Actual contribution | Branch / commits / reviewed PR |
| --- | --- | --- |
| Add name | Add work performed | Add links |
| Add name | Add work performed | Add links |

Each member must use their own account, meaningful commits, their own branch and at least one PR reviewed by a teammate. Populate the table with actual work; no contributions, reviews, user tests or submission artifacts are claimed by this implementation.

Commit `Assets/`, `Packages/`, `ProjectSettings/` and Unity `.meta` files. The Unity `.gitignore` excludes generated caches and build output.
