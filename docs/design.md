Design

Kiosk to borrow lab equipment (Meta Quest 3 kits, cameras, EEG headsets, electronics) at CIAC 2. Built in Unity from a Figma prototype.

Interaction decisions
1. Show availability and always give a next step
[desingImgs/Captura de pantalla 2026-10-13 102440.png](desingImgs/Captura de pantalla 2026-10-13 102440.png)
Problem: when no kit is available, "Unavailable" alone doesn't tell the user what to do.
Principle: visibility of system status (Nielsen).
Design: the screen shows "0 of 6 kits available", the next return time, and two buttons ("Check next availability" and "Other Options"). It refreshes every 15 seconds.
2. Verify the kit contents before continuing
[desingImgs/Captura de pantalla 2026-10-13 102659.png](desingImgs/Captura de pantalla 2026-10-13 102659.png)
Problem: kits come back with missing parts and nobody knows why.
Principle: error prevention.
Design: the user must check "1 Headset", "2 Controllers" and "1 Charger" before the "Review Reservation" button is enabled.
3. Review before confirming
[desingImgs/Captura de pantalla 2026-10-13 102712.png](desingImgs/Captura de pantalla 2026-10-13 102712.png)
Problem: a wrong tap on a touch screen can create a reservation by mistake.
Principle: user control and freedom (Nielsen).
Design: a review screen shows all the choices, with "Edit choices" to go back and "Confirm Reservation" to finish. The loan only starts when the user confirms.
Figma vs Unity
Figma had "Reserve Next" when all kits were on loan. Unity has "Check next availability" with auto-refresh. [Add the real reason]
Figma used fixed example text and times. Unity calculates availability and return times from the clock.
Unity added "Test controls" to switch between available and unavailable states while testing. [Confirm]
The flow and the dark style stayed the same.