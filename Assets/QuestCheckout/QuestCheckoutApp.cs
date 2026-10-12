using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuestCheckout
{
    public sealed class QuestCheckoutApp : MonoBehaviour
    {
        VisualElement root, stage, body, overlay;
        LoanStore store;
        LoanStore genericStore;
        bool generic, transitioning;
        LoanStore CurrentStore => generic ? genericStore : store;
        string KitName => generic ? $"Generic VR {selected}" : $"Meta Quest 3 · Kit {selected}";
        string InventoryPage => generic ? "other" : "kits";
        int selected = 1, duration = 90;
        bool[] verified = new bool[3];
        string page = "home", notice = "";
        KitLoan receipt;
        float lastRefresh;
        bool smokeHadError;
        public static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "quest-loans.json");

        void Awake()
        {
            Application.targetFrameRate = 60;
            var args = Environment.GetCommandLineArgs();
            bool smoke = args.Contains("--checkout-smoke");
            if (smoke) Application.logMessageReceived += TrackSmokeError;
            store = new LoanStore(smoke ? System.IO.Path.Combine(Application.persistentDataPath, "smoke-loans.json") : SavePath);
            genericStore = new LoanStore(System.IO.Path.Combine(Application.persistentDataPath, smoke ? "smoke-generic-loans.json" : "generic-loans.json"), 3);
            if (!File.Exists(store.Path)) Try(() => store.Reset(false, DateTime.UtcNow));
            if (!File.Exists(genericStore.Path)) Try(() => genericStore.Reset(false, DateTime.UtcNow));
            var panel = Resources.Load<PanelSettings>("CheckoutPanel");
            if (panel == null) throw new InvalidOperationException("Missing CheckoutPanel. Run Quest Checkout > Create or open checkout scene.");
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            root = doc.rootVisualElement;
            root.style.backgroundColor = Color.white;
            root.style.flexGrow = 1;
            stage = new VisualElement();
            stage.style.width = 1200;
            stage.style.height = 854;
            stage.style.position = Position.Absolute;
            stage.style.transformOrigin = new TransformOrigin(0, 0, 0);
            stage.styleSheets.Add(Resources.Load<StyleSheet>("Checkout"));
            root.Add(stage);
            root.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            Fit();
            Show("home");
            if (smoke) StartCoroutine(Smoke());
        }
        void Fit()
        {
            float w = root.resolvedStyle.width, h = root.resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0) { w = Screen.width; h = Screen.height; }
            float scale = Mathf.Min(w / 1200f, h / 854f);
            stage.style.scale = new Scale(new Vector3(scale, scale, 1));
            stage.style.left = (w - 1200 * scale) / 2;
            stage.style.top = (h - 854 * scale) / 2;
        }
        void TrackSmokeError(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) smokeHadError = true;
        }
        void OnDestroy() => Application.logMessageReceived -= TrackSmokeError;
        void Update()
        {
            if (!transitioning && Time.unscaledTime - lastRefresh > 15 && overlay == null && (page == "kits" || page == "waiting" || page == "other"))
            { lastRefresh = Time.unscaledTime; Show(page); }
        }
        static void Place(VisualElement el, float x, float y, float w, float h)
        {
            el.style.position = Position.Absolute; el.style.left = x; el.style.top = y; el.style.width = w; el.style.height = h;
        }
        Label Text(VisualElement parent, string text, float x, float y, float w, float h, int size = 22, bool bold = false)
        {
            var label = new Label(text); label.AddToClassList("text");
            Place(label, x, y, w, h); label.style.fontSize = size;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(label); return label;
        }
        VisualElement Picture(VisualElement parent, string name, float x, float y, float w, float h)
        {
            var pic = new Image { image = Resources.Load<Texture2D>(name), scaleMode = ScaleMode.ScaleToFit };
            Place(pic, x, y, w, h); parent.Add(pic); return pic;
        }
        Button Button(VisualElement parent, string text, float x, float y, float w, float h, Action action, string style = "primary")
        {
            var b = new Button(action) { text = text }; b.AddToClassList(style); Place(b, x, y, w, h); parent.Add(b); return b;
        }
        void Try(Action action)
        {
            try { action(); }
            catch (Exception ex) { notice = ex.Message; if (body != null) Alert(notice); Debug.LogWarning(ex.Message); }
        }
        void Alert(string message)
        {
            body.Q<Label>("alert")?.RemoveFromHierarchy();
            var label = Text(body, message, 160, 767, 880, 38, 17); label.name = "alert"; label.AddToClassList("alert");
        }
        void Show(string target)
        {
            if (target == "home" || target == "kits" || target == "waiting") generic = false;
            if (target == "other") generic = true;
            page = target; stage.Clear(); overlay = null;
            body = new VisualElement(); Place(body, 0, 0, 1200, 854); stage.Add(body);
            if (target != "home")
            {
                var home = Button(body, "", 1095, 24, 64, 58, () => Navigate("home"), "icon-button");
                home.tooltip = "Home"; Picture(home, "home", 3, 3, 54, 48);
            }
            switch (target)
            {
                case "home": Home(); break;
                case "kits": Kits(); break;
                case "reserve": Reserve(); break;
                case "review": Review(); break;
                case "success": Success(); break;
                case "waiting": Waiting(); break;
                case "other": Other(); break;
            }
            Text(body, "QUEST CHECKOUT  /  CIAC 2", 30, 822, 400, 22, 12).AddToClassList("footer");
            Button(body, "Test controls", 1030, 812, 140, 34, TestControls, "quiet");
            if (!CurrentStore.CanWrite) Alert(CurrentStore.LoadWarning);
            else if (!string.IsNullOrEmpty(notice)) Alert(notice);
        }
        void Title(string text) => Text(body, text, 220, 90, 760, 65, 40, true);
        void Navigate(string target)
        {
            if (!transitioning) StartCoroutine(Transition(target));
        }
        IEnumerator Transition(string target)
        {
            transitioning = true;
            stage.Clear(); overlay = null;
            body = new VisualElement(); Place(body, 0, 0, 1200, 854); stage.Add(body);
            Title("Going!!");
            Text(body, "Loading…", 400, 165, 400, 40, 23).AddToClassList("muted");
            var panther = Picture(body, "waiting", -300, 290, 300, 340);
            panther.name = "loading-panther";
            float started = Time.unscaledTime;
            const float seconds = 1.15f;
            while (Time.unscaledTime - started < seconds)
            {
                float t = (Time.unscaledTime - started) / seconds;
                panther.style.left = Mathf.Lerp(-300, 1200, t);
                panther.style.top = 290 - Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6)) * 17;
                yield return null;
            }
            Show(target); transitioning = false;
        }
        void PantherHover(Button button, VisualElement artwork, bool available, bool side = false)
        {
            artwork.AddToClassList("hover-art"); artwork.pickingMode = PickingMode.Ignore;
            var panther = Picture(button, available ? (side ? "panther-point" : "panther-select") : "panther-stop",
                side && available ? -100 : available ? 65 : 60,
                available ? (side ? 50 : -85) : 42, 105, available ? 100 : 125);
            panther.name = "hover-panther"; panther.pickingMode = PickingMode.Ignore;
            panther.style.display = DisplayStyle.None;
            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (transitioning) return;
                button.AddToClassList("panther-hover");
                panther.style.display = DisplayStyle.Flex;
            });
            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                button.RemoveFromClassList("panther-hover");
                panther.style.display = DisplayStyle.None;
            });
        }
        void Home()
        {
            Title("What’s your choice today?");
            string[] icons = { "quest-icon", "camera-icon", "headset-icon", "electronics-icon" };
            string[] names = { "Meta Quest 3", "Cameras", "Headsets EEG", "Electronics" };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var b = Button(body, "", 68 + i * 278, 320, 245, 270, () => { if (index == 0) { notice = ""; Navigate("kits"); } else Alert("This category is unavailable at this kiosk. Please visit CIAC 2."); }, "category");
                b.name = "category-" + i;
                var artwork = Picture(b, icons[i], 20, 15, 205, 184);
                Text(b, names[i], 0, 217, 245, 40, 22, true);
                PantherHover(b, artwork, index == 0);
            }
        }
        void Kits()
        {
            Title("Meta Quest 3 Kits");
            int available = store.AvailableCount(DateTime.UtcNow);
            bool none = available == 0;
            if (none) Picture(body, "waiting", 205, 52, 140, 140);
            var availability = Text(body, store.CanWrite ? (none ? "0 of 6 kits available  •  See the next return below" : $"{available} of 6 kits available  •  Select a kit to continue") : "Inventory unavailable", none ? 310 : 30, none ? 163 : 780, none ? 580 : 420, 30, none ? 16 : 14);
            availability.AddToClassList("muted");
            for (int i = 1; i <= 6; i++)
            {
                int kit = i;
                float x = i <= 4 ? 60 + (i - 1) * 297 : 280 + (i - 5) * 443;
                float y = i <= 4 ? 218 : 496;
                float size = none ? 150 : 200;
                if (none) { x = i <= 4 ? 185 + (i - 1) * 226 : 352 + (i - 5) * 338; y = i <= 4 ? 216 : 420; }
                bool ok = store.Available(kit, DateTime.UtcNow);
                var b = Button(body, "", x, y, size, size + 57, () => SelectKit(kit), "kit");
                b.name = "quest-kit-" + kit;
                var artwork = Picture(b, ok ? "kit" : "kit-unavailable", 0, 0, size, size);
                Text(b, ok ? $"Kit {kit}" : "Unavailable", -10, size + 5, size + 20, 29, 22, true);
                if (!ok) Text(b, $"Kit {kit} • on loan", -15, size + 32, size + 30, 23, 13).AddToClassList("muted");
                PantherHover(b, artwork, ok, i == 5);
            }
            if (none && store.CanWrite)
            {
                Text(body, "All six kits are on loan. Next return: " + Format(store.NextAvailable(DateTime.UtcNow)), 125, 632, 950, 42, 24);
                Button(body, "Check next availability", 171, 703, 405, 69, () => { notice = ""; Navigate("waiting"); });
                Button(body, "Other Options", 602, 703, 355, 69, () => Navigate("other"));
            }
            else Button(body, "Other Options", 464, 766, 270, 39, () => Navigate("other"), "quiet");
        }
        void SelectKit(int kit, bool isGeneric = false)
        {
            if (!(isGeneric ? genericStore : store).Available(kit, DateTime.UtcNow)) { Alert("This kit is unavailable. Please choose an available kit."); return; }
            generic = isGeneric;
            selected = kit; verified = new bool[3]; notice = ""; Navigate("reserve");
        }
        void Reserve()
        {
            Title(generic ? $"Generic VR {selected}" : "Meta Quest 3 Reserve");
            Picture(body, "mascot", 190, 55, 145, 137);
            Picture(body, generic ? "generic-kit" : "kit", 75, 233, 280, 280);
            Text(body, KitName, 52, 526, 330, 40, 23, true);
            Button(body, "Change kit", 135, 583, 160, 43, () => { notice = ""; Navigate(InventoryPage); }, "secondary");
            var card = new VisualElement(); card.AddToClassList("card"); Place(card, 445, 194, 330, 443); body.Add(card);
            var header = Text(card, "Select Reservation Duration", 0, 0, 330, 62, 22, true); header.AddToClassList("card-header");
            Text(card, "STARTS TODAY", 20, 86, 290, 25, 13, true).AddToClassList("muted");
            Text(card, DateTime.Now.ToString("ddd, dd MMM yyyy"), 18, 115, 294, 40, 24, true);
            Text(card, "Choose your loan duration", 15, 176, 300, 28, 18);
            int[] minutes = { 30, 60, 90, 120 };
            for (int i = 0; i < minutes.Length; i++)
            {
                int m = minutes[i];
                var b = Button(card, m + " min", 22 + (i % 2) * 149, 222 + (i / 2) * 61, 137, 50, () => { duration = m; Show("reserve"); }, duration == m ? "selected" : "secondary");
                b.tooltip = "Select " + m + " minutes";
            }
            Text(card, "Estimated return", 15, 353, 300, 24, 15).AddToClassList("muted");
            Text(card, Format(DateTime.UtcNow.AddMinutes(duration)), 10, 382, 310, 36, 21, true);
            Picture(body, generic ? "generic-kit" : "contents", 843, 185, 295, 260);
            Text(body, "Verify kit contents", 836, 463, 310, 36, 22, true);
            string[] contents = { "1 Headset", generic ? "1 Controller" : "2 Controllers", "1 Charger" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var toggle = new Toggle { text = contents[i], value = verified[i] }; toggle.AddToClassList("check");
                Place(toggle, 871, 508 + i * 42, 260, 38); body.Add(toggle);
                toggle.RegisterValueChangedCallback(e => { verified[index] = e.newValue; body.Q<Button>("review").SetEnabled(verified.All(v => v)); });
            }
            Text(body, "Check all three items to continue.", 817, 644, 350, 32, 15).AddToClassList("muted");
            var next = Button(body, "Review Reservation", 432, 694, 355, 72, () => { notice = ""; Show("review"); });
            next.name = "review"; next.SetEnabled(verified.All(v => v));
        }
        void Review()
        {
            Title("Review your reservation");
            Picture(body, generic ? "generic-kit" : "kit", 140, 230, 300, 300);
            var card = new VisualElement(); card.AddToClassList("card"); Place(card, 500, 217, 510, 378); body.Add(card);
            Text(card, KitName, 25, 25, 460, 50, 30, true);
            Text(card, $"Duration: {duration} minutes\nPick up at: {LoanStore.Pickup}\nEstimated return: {Format(DateTime.UtcNow.AddMinutes(duration))}\n\nVerified: 1 headset, {(generic ? "1 controller" : "2 controllers")}, 1 charger", 30, 94, 450, 237, 22);
            Text(body, "Your loan starts when you confirm.", 410, 622, 640, 32, 19).AddToClassList("muted");
            Button(body, "Edit choices", 221, 700, 275, 68, () => Show("reserve"), "secondary");
            Button(body, "Confirm Reservation", 535, 700, 424, 68, Confirm);
        }
        void Confirm()
        {
            Try(() => { receipt = CurrentStore.Checkout(selected, duration, verified, DateTime.UtcNow); notice = ""; Show("success"); });
        }
        void Success()
        {
            Title("Reservation successful");
            Picture(body, "celebrate", 436, 202, 342, 337);
            Text(body, "Pick up at: " + LoanStore.Pickup, 180, 567, 840, 56, 38, true);
            Text(body, "Return: " + Format(new DateTime(receipt.dueUtc, DateTimeKind.Utc)), 120, 624, 960, 54, 32, true);
            Text(body, $"{KitName}  •  {duration} minutes  •  Reservation saved", 220, 678, 760, 31, 19).AddToClassList("muted");
            Button(body, "Back to kits", 445, 724, 310, 55, () => Navigate(InventoryPage));
        }
        void Waiting()
        {
            Title("Next available kit");
            Picture(body, "waiting", 440, 196, 310, 285);
            bool available = store.AvailableCount(DateTime.UtcNow) > 0;
            Text(body, available ? "A kit is now available" : "All six Quest 3 kits are currently on loan.", 150, 499, 900, 48, 30, true);
            Text(body, available ? "Return to the kit list to start your checkout." : "Earliest return: " + Format(store.NextAvailable(DateTime.UtcNow)) + "\nThis screen checks availability every 15 seconds.\nFor immediate help or other equipment, visit CIAC 2.", 130, 563, 940, 111, 23);
            Button(body, available ? "Choose an available kit" : "Refresh availability", 365, 706, 470, 65, () => { notice = ""; Show(store.AvailableCount(DateTime.UtcNow) > 0 ? "kits" : "waiting"); });
        }
        void Other()
        {
            Title("Other options");
            Picture(body, "waiting", 205, 52, 140, 140);
            for (int i = 1; i <= 3; i++)
            {
                int kit = i;
                bool available = genericStore.Available(kit, DateTime.UtcNow);
                var b = Button(body, "", 184 + (i - 1) * 316, 329, 210, 249, () => SelectKit(kit, true), "kit");
                b.name = "generic-kit-" + kit;
                var art = Picture(b, available ? "generic-kit" : "generic-unavailable", 0, 0, 210, 200);
                Text(b, available ? $"Generic VR {kit}" : "Unavailable", -12, 207, 234, 35, 22, true);
                PantherHover(b, art, available);
            }
            if (genericStore.CanWrite && genericStore.AvailableCount(DateTime.UtcNow) == 0)
                Text(body, "Next return: " + Format(genericStore.NextAvailable(DateTime.UtcNow)), 170, 631, 860, 45, 24);
            Button(body, "Back to Quest 3 kits", 415, 710, 370, 60, () => { notice = ""; Navigate("kits"); }, "secondary");
        }
        void TestControls()
        {
            if (overlay != null) return;
            overlay = new VisualElement(); overlay.AddToClassList("overlay"); Place(overlay, 0, 0, 1200, 854); stage.Add(overlay);
            var panel = new VisualElement(); panel.AddToClassList("card"); Place(panel, 260, 155, 680, 535); overlay.Add(panel);
            Text(panel, "Test controls", 30, 25, 620, 55, 32, true);
            Text(panel, "Reset replaces all saved loans on this device.\nA: four Quest kits available. B: all six on loan.\nBoth resets make Generic VR 1 available.", 40, 95, 600, 108, 21);
            Button(panel, "Reset to A · available kits", 65, 234, 550, 65, () => Reset(false));
            Button(panel, "Reset to B · no kits available", 65, 317, 550, 65, () => Reset(true));
            Button(panel, "Cancel", 220, 426, 240, 57, () => { overlay.RemoveFromHierarchy(); overlay = null; }, "secondary");
        }
        void Reset(bool busy)
        {
            Try(() => { store.Reset(busy, DateTime.UtcNow); genericStore.Reset(false, DateTime.UtcNow); selected = 1; duration = 90; verified = new bool[3]; receipt = null; notice = ""; Show("kits"); });
        }
        static string Format(DateTime utc) => utc.ToLocalTime().ToString("dd MMM yyyy 'at' h:mm tt");

        IEnumerator Smoke()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots"); Directory.CreateDirectory(dir);
            Reset(false); Show("home"); yield return Capture(dir, "01-home");
            Hover("category-0", true); yield return Capture(dir, "10-home-hover");
            Hover("category-0", false);
            if (body.Q<Button>("category-0").Q("hover-panther").style.display.value != DisplayStyle.None) throw new Exception("Hover did not clear");
            Hover("category-3", true); yield return Capture(dir, "11-blocked-hover"); Hover("category-3", false);
            Navigate("kits");
            var loading = body.Q("loading-panther");
            float startX = loading.style.left.value.value;
            yield return Capture(dir, "12-loading");
            if (!transitioning || loading.style.left.value.value <= startX) throw new Exception("Loading panther did not move right");
            while (transitioning) yield return null;
            if (page != "kits") throw new Exception("Menu transition failed");
            Show("kits"); yield return Capture(dir, "02-kits");
            Hover("quest-kit-3", true); yield return Capture(dir, "17-top-kit-hover"); Hover("quest-kit-3", false);
            Hover("quest-kit-5", true); yield return Capture(dir, "13-kit-hover"); Hover("quest-kit-5", false);
            SelectKit(1); while (transitioning) yield return null;
            yield return Capture(dir, "03-reserve");
            if (body.Q<Button>("review").enabledSelf) throw new Exception("Checklist did not block review");
            foreach (var toggle in body.Query<Toggle>().ToList()) toggle.value = true;
            if (!body.Q<Button>("review").enabledSelf) throw new Exception("Checklist did not enable review");
            Submit("Review Reservation");
            if (page != "review") throw new Exception("Review button failed");
            Submit("Edit choices");
            Submit("60 min");
            if (duration != 60 || !verified.All(v => v)) throw new Exception("Edit choices failed");
            Submit("90 min"); Submit("Review Reservation"); yield return Capture(dir, "04-review");
            Submit("Confirm Reservation"); yield return Capture(dir, "05-success");
            if (page != "success" || new LoanStore(store.Path).Available(1, DateTime.UtcNow)) throw new Exception("Smoke checkout/persistence failed");
            Reset(true); yield return Capture(dir, "06-unavailable");
            Show("waiting"); yield return Capture(dir, "07-next-availability");
            TestControls(); yield return Capture(dir, "08-test-controls");
            Show("other"); yield return Capture(dir, "09-other-options");
            Hover("generic-kit-1", true); yield return Capture(dir, "14-generic-hover"); Hover("generic-kit-1", false);
            SelectKit(2, true);
            if (page != "other" || transitioning) throw new Exception("Unavailable generic kit was selectable");
            SelectKit(1, true); while (transitioning) yield return null;
            yield return Capture(dir, "15-generic-reserve");
            if (!generic || !body.Query<Toggle>().ToList().Any(t => t.text == "1 Controller")) throw new Exception("Generic checklist incorrect");
            foreach (var toggle in body.Query<Toggle>().ToList()) toggle.value = true;
            Submit("Review Reservation"); Submit("Confirm Reservation");
            if (page != "success" || new LoanStore(genericStore.Path, 3).Available(1, DateTime.UtcNow)) throw new Exception("Generic checkout did not persist");
            if (store.AvailableCount(DateTime.UtcNow) != 0) throw new Exception("Generic checkout affected Quest inventory");
            yield return Capture(dir, "16-generic-success");
            if (smokeHadError) { Debug.LogError("QUEST_CHECKOUT_SMOKE_FAIL: runtime errors were logged."); Application.Quit(1); }
            else { Debug.Log("QUEST_CHECKOUT_SMOKE_PASS"); Application.Quit(0); }
        }
        void Submit(string text)
        {
            var button = body.Query<Button>().ToList().First(b => b.text == text);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        void Hover(string name, bool enter)
        {
            var button = body.Q<Button>(name);
            if (enter) { using (var evt = PointerEnterEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); } }
            else { using (var evt = PointerLeaveEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); } }
        }
        IEnumerator Capture(string dir, string name)
        {
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
