# Quest Checkout

**Project 02 · Human–Computer Interaction · Second Partial**

## About the project

Quest Checkout is our Unity implementation of the kiosk we designed in Figma for Project 01. The goal is to make borrowing a Meta Quest 3 kit easy to understand: choose an available kit, select a day and duration, check its contents, review the reservation, and confirm it.

The app manages six Meta Quest 3 kits and saves reservations locally so they remain after closing and reopening it. The confirmation shows the pickup location, **CIAC 2**, and the return deadline. When no Quest kits are available, the app explains the situation and offers a way to check the next availability or choose a generic VR kit.

We used separate Unity Canvas elements for the screens, buttons, images, dropdown, and checkboxes. The panther provides hover feedback and appears in the loading transition. This project focuses on applying the interaction design from our prototype in a working classroom demo.

## Project links and reports

| Resource | Link / status |
| --- | --- |
| Original Project 01 Figma | https://www.figma.com/proto/J9WmKUbRiZGFD2TlP7GAQk/Untitled?node-id=3-2&starting-point-node-id=3%3A2|
| GitHub Release| [GitHub Releases](https://github.com/UrielMonarrez17/Monitor_Interface/releases)|
| Design justification | [docs/design.md](docs/design.md)|
| Testing report | [docs/testing.md](docs/testing.md)|
| AI-use declaration | [docs/ai-use.md](docs/ai-use.md)|
| Video | [FinalVideo/TeamVideo.mp4](FinalVideo/TeamVideo.mp4)|
| Final buld | [Builds/QuestCheckout-Windows.zip](Builds/QuestCheckout-Windows.zip)|

## Team contributions
| Team member | Contribution |
| --- | --- |
| Uriel Monarrez Cervantes | Flow A implementation |
| David Patricio Avalos Molinar | Flow B implementation |
| Juan Pablo Villaseñor| App Design |

## Unity version and setup

The project uses **Unity 6000.5.7f1**.

1. Install Unity Hub and Unity **6000.5.7f1**, including Windows Build Support.
2. Clone or download this repository. Keep `Assets`, `Packages`, `ProjectSettings`, and the `.meta` files together.
3. In Unity Hub, add the repository folder as a project and open it.
4. Wait for Unity to finish importing the assets and packages.
5. Open `Assets/Scenes/QuestCheckout.unity` and press **Play**.

## Run the Windows build

1. Download the complete Windows build ZIP from the linked GitHub Release.
2. Extract the ZIP into a folder.
3. Open `QuestCheckout.exe` inside the extracted folder.

## Controls and reservation flow

Use the mouse to select buttons, choose a day from the dropdown, and check the kit contents. Hovering over an option enlarges its image and shows the panther. Available options use the pointing pose, while unavailable options use the stop pose.

1. Select **Meta Quest 3** and choose an available kit.
2. Choose **today or one of the next six days**. Today starts when you confirm; future reservations start at **9:00 AM local time**.
3. Select **30, 60, 90, or 120 minutes**. The default is 90 minutes.
4. Check **1 Headset**, **2 Controllers**, and **1 Charger**. All three are required to continue.
5. Select **Review Reservation** to check the kit, pickup time, duration, location, and return deadline.
6. Use **Edit choices** to change the reservation or **Confirm Reservation** to save it. Availability is checked again before confirmation.

**Change kit** returns to inventory and clears the contents checklist. The **Home** icon returns to the main screen. **Other Options** opens three generic VR kits, which use the same reservation flow but have one controller instead of two.

## Reset the data

Open **Test controls** at the bottom right of the screen and choose a scenario:

- **Reset to A · available kits:** makes Quest kits 1, 3, 4, and 5 available. Kits 2 and 6 start on loan.
- **Reset to B · no kits available:** puts all six Quest kits on loan.
- **Cancel:** closes the dialog without changing the saved data.

Both resets replace the saved Quest reservations, including future bookings. They also reset the generic inventory so Generic VR 1 is available and Generic VR 2 and 3 are on loan. The reset state is saved across restarts. A first launch without saved data starts with scenario A.

## Test scenario A: borrow a kit for 90 minutes

1. Open **Test controls** and select **Reset to A · available kits**.
2. Choose **Quest Kit 1**.
3. Keep the day as **Today** and the duration as **90 min**.
4. Verify all three contents and select **Review Reservation**.
5. Check the details, then select **Confirm Reservation**.
6. Confirm that the receipt shows **CIAC 2** and a return deadline 90 minutes after confirmation.
7. Return to the kit list. Kit 1 should now be unavailable.
8. Close and reopen the app before the deadline. Kit 1 should still be unavailable.

To check editing, use **Edit choices** before confirming, change the day or duration, and return to the review. The updated choices should appear there.

## Test scenario B: no Quest kits available

1. Open **Test controls** and select **Reset to B · no kits available**.
2. Confirm that all six Quest kits are unavailable and cannot be selected.
3. Select **Check next availability** to see the earliest return time. The screen refreshes every 15 seconds and also includes a manual refresh button.
4. Open **Other Options** from the Quest inventory and choose **Generic VR 1** to try the alternative checkout flow.
5. Reset to scenario A when you want to restore available Quest kits immediately.

Generic VR reservations are stored separately and do not change the six Quest kits. When a loan reaches its deadline, the kit becomes available again automatically.

These steps describe expected behavior. Our actual test results, participant observations, fixes, and evidence belong in [the testing report](docs/testing.md).

## Saved data and known limitations

Reservations are saved locally in `Application.persistentDataPath`. On Windows, this is normally:

```text
%USERPROFILE%/AppData/LocalLow/CIAC/Quest Checkout/
```

The files are `quest-loans.json` and `generic-loans.json`. Times are stored in UTC and displayed in the computer's local time. If saved data cannot be read, checkout is blocked and the test controls can reset it.

- This is a classroom prototype for one device. There is no server, user login, or shared inventory between computers.
- Each kit supports one outstanding reservation. A future booking holds the kit immediately until its return deadline; the app does not schedule multiple bookings for the same kit.
- Future pickup is fixed at 9:00 AM. There is no custom pickup time, opening-hours calendar, or booking beyond the next six days.
- Kits become available automatically at their deadline. The app does not verify that equipment has physically been returned.
- Cameras, EEG headsets, and Electronics are unavailable categories in this version.
- Panther hover feedback requires a pointer; it is not shown as a hover interaction on a touchscreen.
- Test controls remain visible so both required scenarios are easy to demonstrate.
- The runnable build targets Windows. Other platforms have not been verified.

## Developer checks

In Unity, select **Quest Checkout > Verify checkout logic** to run the checks for checkout, invalid inputs, saved data, reservation dates, and scenario resets. **Quest Checkout > Validate Canvas scene** checks the saved UI hierarchy and button connections.

Launching the Windows app with `--checkout-smoke` runs the automated UI checks using separate test save files, captures screenshots, and exits. These checks support debugging; they do not replace our functional report or testing with three people outside the team.
