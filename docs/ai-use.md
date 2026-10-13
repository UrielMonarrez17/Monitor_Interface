# AI use declaration

## Scope

We used OpenAI Codex to support the development of Quest Checkout in Unity. This included mascot feedback behavior, debugging support, the mascot transition, and test controls for flows A and B. AI also assisted with the Unity implementation, the migration to editable Canvas UI elements, local loan persistence, the reservation-day dropdown, automated checks, and README preparation. This declaration does not claim that AI was used only for the four areas listed below.

The mascot artwork used in this implementation came from the team's supplied Figma PDF. AI assistance created and adjusted the **mascot-based feedback behavior and its integration into Unity**; this session does not establish that AI generated the original artwork.

## Usage record

Tool: **OpenAI Codex**. Record prepared: **2026-10-13**. The entries summarize the development conversation; prompt summaries are paraphrases, not verbatim transcripts. Team member responsible for requesting and reviewing each entry: **team to complete**. Add the actual commit or PR links before submission.

| Purpose | Prompt summary | Response used and adjustments | Verification and related work |
| --- | --- | --- | --- |
| Create mascot feedback in the interface | Show the panther when hovering over an option; enlarge the icon; use the stop pose for unavailable options. | Implemented pointer-enter/exit behavior using the supplied artwork. Adjusted the poses and placement to match the PDF, including generic VR options. | Automated hover-enter/leave checks. See [PantherHover.cs](../Assets/Scripts/PantherHover.cs) and [CanvasSmokeTest.cs](../Assets/Scripts/CanvasSmokeTest.cs). |
| Debugging support | Help diagnose and fix Unity UI, navigation, availability, and persistence problems. | Assisted with code corrections, serialized button wiring, validation, and save-error handling. | Editor logic checks and runtime smoke checks; these are developer checks, not participant usability results. See [CheckoutBuild.cs](../Assets/Editor/CheckoutBuild.cs) and [LoanStore.cs](../Assets/Scripts/LoanStore.cs). |
| Mascot transition | Move the panther from left to right while showing loading feedback when entering a menu. | Implemented the transition and connected it to navigation. The moving image is an editable Canvas object. | Runtime check confirms that the panther moves right and navigation completes. See [PantherTransition.cs](../Assets/Scripts/PantherTransition.cs). |
| Test controls for flows A and B | Provide an easy way to view and test available-kit checkout and the no-kits-available flow. | Added a test-controls dialog with scenario A/B resets and cancellation. Resets replace demo inventory and save the new state. | Logic and runtime checks exercise both flows and reset behavior. See [QuestCheckoutApp.cs](../Assets/Scripts/QuestCheckoutApp.cs) and [CanvasSmokeTest.cs](../Assets/Scripts/CanvasSmokeTest.cs). |
| Unity Canvas implementation and reservation day | Recreate the Figma kiosk with separate editable Canvas elements; add a simple day selector. | Assisted with the saved UI hierarchy, separate behavior scripts, reservation validation, and a native Dropdown for today and the next six days. | Checks cover saved reservations, future pickup time, blocked overwrites, invalid dates, and editing choices. See [Canvas prefab](../Assets/Prefabs/QuestCheckoutCanvas.prefab), [ReservationForm.cs](../Assets/Scripts/ReservationForm.cs), and [ReservationDaySetup.cs](../Assets/Editor/ReservationDaySetup.cs). |
| Project instructions | Prepare the README with setup, controls, reset instructions, and limitations. | Drafted and updated the [README](../README.md); team-specific submission links remain for the team to complete. | Team must verify its release link, Unity setup, contributions, and documentation links before submission. |

## Team review before submission

Each teammate should understand the code they use and document their actual review and changes. Complete the responsible teammate names, relevant work links, and the team's verification dates/results. No teammate reviews, external-user sessions, usability findings, or video participation are claimed here. The team will produce its own testing report and video using real evidence.
