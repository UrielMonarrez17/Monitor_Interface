# AI use declaration

## Scope

We used ChatGpt and Gemini to support the development of Quest Checkout in Unity. This included mascot feedback behavior, debugging support, the mascot transition, and test controls for flows A and B.

The mascot artwork used in this implementation came from Gemini Nano Banana Model. AI assistance created and adjusted the mascot design.

## Usage record

Tool: Web ChatGpt and Gemini. The entries summarize the development conversation.

| Purpose | Prompt summary | Response used and adjustments | Verification and related work | Teammate |
| --- | --- | --- | --- | --- |
| Create mascot for feedback in the interface | Create a mascot for making more easy and personal connection in the interface for the feedback | A image in jpg of a mascot similar to a panter | Panter image and design | Juan Pablo |
| Debugging support | Help diagnose and fix Unity UI, availability, and persistence problems. | Assisted with code corrections, serialized button wiring, validation, and save-error handling. | Editor logic checks and runtime smoke checks | Uriel and David |
| Mascot transition | Move the panther from left to right while showing loading feedback when entering a menu. | Implemented the transition and connected it to navigation. The moving image is an editable Canvas object. | Runtime check confirms that the panther moves right and navigation completes. | Uriel Monarrez |
| Test controls for flows A and B | Provide an easy way to view and test available-kit checkout and the no-kits-available flow. | Added a test-controls dialog with scenario A/B resets and cancellation.| Logic and runtime checks exercise both flows and reset behavior. | David Patricio |


