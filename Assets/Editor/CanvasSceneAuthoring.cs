using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace QuestCheckout.Editor
{
    // One-time editor authoring tool. No UI is constructed at runtime or during normal builds.
    public static class CanvasSceneAuthoring
    {
        public const string ScenePath = "Assets/Scenes/QuestCheckout.unity";
        const string PrefabPath = "Assets/QuestCheckout/Prefabs/QuestCheckoutCanvas.prefab";
        static readonly Color Blue = new Color32(105, 162, 184, 255);
        static readonly Color Muted = new Color32(92, 100, 105, 255);
        static Font font;
        static Sprite rounded;
        static QuestCheckoutApp app;
        static RectTransform content;
        static readonly List<CanvasScreen> screens = new List<CanvasScreen>();

        public static void EnsureScene()
        {
            if (EditorSceneManager.GetActiveScene().path == ScenePath && UnityEngine.Object.FindAnyObjectByType<Canvas>() != null)
            {
                if (EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                ValidateScene(); return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException("Scene switch cancelled.");
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                if (UnityEngine.Object.FindAnyObjectByType<Canvas>() != null) { ValidateScene(); return; }
            }
            Author();
        }
        static void PrepareArt()
        {
            Directory.CreateDirectory("Assets/QuestCheckout/Prefabs");
            Directory.CreateDirectory("Assets/QuestCheckout/Art");
            // Small nine-sliced rounded rectangle for native uGUI Images.
            string path = "Assets/QuestCheckout/Art/RoundedPanel.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 16, 0), dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 16, 0);
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(16 - Mathf.Sqrt(dx * dx + dy * dy))));
                }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles("Assets/QuestCheckout", "*.png", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(normalized) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                if (normalized == path) importer.spriteBorder = new Vector4(20, 20, 20, 20);
                importer.SaveAndReimport();
            }
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        static void Author()
        {
            PrepareArt(); screens.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Quest Checkout Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200, 854); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            app = root.AddComponent<QuestCheckoutApp>();
            var backdrop = Box(root.transform, "White Background", 0, 0, 1200, 854, Color.white);
            var br = backdrop.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            content = Rect(root.transform, "Screens - 1200 x 854", 0, 0, 1200, 854);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f); content.anchoredPosition = Vector2.zero;
            Home(); app.questInventory = Inventory("QuestKits", false, false); app.unavailableInventory = Inventory("QuestUnavailable", false, true);
            app.genericInventory = Inventory("GenericKits", true, false);
            Reservation(); Review(); Receipt(); Waiting(); Loading(); Shared();
            app.screens = screens.ToArray();
            foreach (var screen in app.screens) screen.gameObject.SetActive(screen.key == "Home");
            app.homeButton.SetActive(false); app.alert.gameObject.SetActive(false); app.resetDialog.SetActive(false);
            root.AddComponent<CanvasSmokeTest>().app = app;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidateScene();
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        static Image Box(Transform p, string name, float x, float y, float w, float h, Color color, bool round = false)
        {
            var image = Rect(p, name, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color;
            if (round) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            return image;
        }
        static Text Label(Transform p, string name, string value, float x, float y, float w, float h, int size = 22, bool bold = false, Color? color = null)
        {
            var text = Rect(p, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color ?? Color.black; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuestCheckout/Resources/" + name + ".png");
        static Image Picture(Transform p, string name, string art, float x, float y, float w, float h)
        {
            var image = Box(p, name, x, y, w, h, Color.white); image.sprite = Art(art); image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        static Button Button(Transform p, string name, string label, float x, float y, float w, float h, UnityAction action = null, bool secondary = false, bool card = false)
        {
            var background = Box(p, name, x, y, w, h, card || secondary ? Color.white : Blue, !card);
            var button = background.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var colors = button.colors; colors.highlightedColor = new Color(.92f,.96f,.98f); colors.pressedColor = new Color(.8f,.88f,.92f); colors.disabledColor = new Color(1,1,1,.4f); button.colors = colors;
            if (secondary) { var outline = background.gameObject.AddComponent<Outline>(); outline.effectColor = Blue; outline.effectDistance = new Vector2(1,-1); }
            if (!string.IsNullOrEmpty(label)) Label(button.transform, "Label", label, 8, 2, w - 16, h - 4, secondary ? 22 : 27, true, secondary ? new Color32(52,106,127,255) : Color.white);
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
        static void Route(Button b, string page) => UnityEventTools.AddStringPersistentListener(b.onClick, app.Navigate, page);
        static RectTransform Screen(string key, string title)
        {
            var rect = Rect(content, key, 0, 0, 1200, 854); rect.gameObject.AddComponent<CanvasGroup>(); var screen = rect.gameObject.AddComponent<CanvasScreen>(); screen.key = key; screens.Add(screen);
            Label(rect, "Heading", title, 220, 90, 760, 65, 40, true); return rect;
        }
        static PantherHover Hover(Button b, Image art, bool available, bool side = false)
        {
            // Art scales around its centre without enlarging the pointer hit area.
            var r = art.rectTransform; r.anchoredPosition += new Vector2(r.sizeDelta.x / 2, -r.sizeDelta.y / 2); r.pivot = new Vector2(.5f,.5f);
            var hover = b.gameObject.AddComponent<PantherHover>(); hover.artwork = r; hover.available = available;
            hover.selectingPanther = Picture(b.transform, "Panther - Selecting (hover only)", side ? "panther-point" : "panther-select", side ? -100 : 65, side ? 50 : -85, 105, 100).gameObject;
            hover.blockingPanther = Picture(b.transform, "Panther - Stop (hover only)", "panther-stop", 60, 42, 105, 125).gameObject;
            hover.selectingPanther.SetActive(false); hover.blockingPanther.SetActive(false); return hover;
        }
        static void Home()
        {
            var p = Screen("Home", "What’s your choice today?");
            string[] names = { "Meta Quest 3", "Cameras", "Headsets EEG", "Electronics" }, art = { "quest-icon", "camera-icon", "headset-icon", "electronics-icon" };
            for (int i = 0; i < 4; i++)
            {
                var b = Button(p, names[i] + " Button", "", 68 + i * 278, 320, 245, 270, card: true);
                if (i == 0) Route(b, "QuestKits"); else UnityEventTools.AddPersistentListener(b.onClick, app.UnavailableCategory);
                var image = Picture(b.transform, "Category Icon", art[i], 20, 15, 205, 184);
                Label(b.transform, "Category Name", names[i], 0, 217, 245, 40, 22, true); Hover(b, image, i == 0);
            }
        }
        static InventoryScreen Inventory(string key, bool generic, bool none)
        {
            var p = Screen(key, generic ? "Other options" : "Meta Quest 3 Kits");
            var view = p.gameObject.AddComponent<InventoryScreen>(); view.app = app; view.generic = generic;
            if (generic || none) Picture(p, "Waiting Panther", "waiting", 205, 52, 140, 140);
            if (!generic) view.availability = Label(p, "Availability Count", none ? "0 of 6 kits available • See the next return below" : "4 of 6 kits available • Select a kit to continue", none ? 310 : 30, none ? 163 : 780, none ? 580 : 420, 30, none ? 16 : 14, color: Muted);
            var cards = new List<KitCardView>();
            for (int i = 1; i <= (generic ? 3 : 6); i++)
            {
                float x = generic ? 184 + (i - 1) * 316 : i <= 4 ? 60 + (i - 1) * 297 : 280 + (i - 5) * 443;
                float y = generic ? 329 : i <= 4 ? 218 : 496, size = generic ? 210 : none ? 150 : 200;
                if (none) { x = i <= 4 ? 185 + (i - 1) * 226 : 352 + (i - 5) * 338; y = i <= 4 ? 216 : 420; }
                bool available = !none && i != 2 && i != (generic ? 3 : 6);
                var b = Button(p, (generic ? "Generic VR " : "Quest Kit ") + i + " Button", "", x, y, size, size + 57, card: true);
                if (generic) UnityEventTools.AddIntPersistentListener(b.onClick, app.SelectGenericKit, i); else UnityEventTools.AddIntPersistentListener(b.onClick, app.SelectQuestKit, i);
                var card = b.gameObject.AddComponent<KitCardView>(); card.kit = i;
                card.availableImage = Art(generic ? "generic-kit" : "kit"); card.unavailableImage = Art(generic ? "generic-unavailable" : "kit-unavailable");
                card.artwork = Picture(b.transform, "Kit Photograph", generic ? "generic-kit" : "kit", 0, 0, size, generic ? 200 : size);
                card.title = Label(b.transform, "Kit Name", "", -10, generic ? 207 : size + 5, size + 20, 32, 22, true);
                if (!generic) card.status = Label(b.transform, "Loan Status", "", -15, size + 32, size + 30, 23, 13, color: Muted);
                card.hover = Hover(b, card.artwork, available, !generic && i == 5); card.Refresh(available, generic); cards.Add(card);
            }
            view.cards = cards.ToArray();
            if (generic || none) view.nextReturn = Label(p, "Next Return", "Next return appears here", 125, 632, 950, 42, 24);
            if (generic) Route(Button(p, "Back to Quest Button", "Back to Quest 3 kits", 415, 710, 370, 60, secondary: true), "QuestKits");
            else if (none)
            {
                Route(Button(p, "Next Availability Button", "Check next availability", 171, 703, 405, 69), "Waiting");
                Route(Button(p, "Other Options Button", "Other Options", 602, 703, 355, 69), "GenericKits");
            }
            else { var b = Button(p, "Other Options Button", "Other Options", 464, 766, 270, 39, secondary: true); b.GetComponentInChildren<Text>().fontSize = 16; Route(b, "GenericKits"); }
            return view;
        }
        static void Reservation()
        {
            var p = Screen("Reservation", "Meta Quest 3 Reserve"); var form = p.gameObject.AddComponent<ReservationForm>(); app.reservation = form; form.app = app;
            form.title = p.Find("Heading").GetComponent<Text>();
            Picture(p, "Reading Panther", "mascot", 190, 55, 145, 137);
            form.questKit = Art("kit"); form.questContents = Art("contents"); form.genericKit = Art("generic-kit");
            form.kitImage = Picture(p, "Selected Kit Photograph", "kit", 75, 233, 280, 280);
            form.kitName = Label(p, "Selected Kit Name", "Meta Quest 3 · Kit 1", 52, 526, 330, 40, 23, true);
            Button(p, "Change Kit Button", "Change kit", 135, 583, 160, 43, app.ChangeKit, true);
            var card = Box(p, "Duration Card", 445, 194, 330, 443, new Color32(248,248,248,255), true).transform;
            Box(card, "Blue Header", 0, 0, 330, 62, Blue, true);
            Label(card, "Header Text", "Select Reservation Duration", 5, 0, 320, 62, 22, true, Color.white);
            Label(card, "Start Caption", "STARTS TODAY", 20, 86, 290, 25, 13, true, Muted);
            form.startDate = Label(card, "Start Date", "Today", 18, 115, 294, 40, 24, true);
            Label(card, "Duration Prompt", "Choose your loan duration", 15, 176, 300, 28, 18);
            form.durationButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int minutes = form.durations[i]; var b = Button(card, minutes + " Minutes Button", minutes + " min", 22 + (i % 2) * 149, 222 + (i / 2) * 61, 137, 50, secondary: true);
                UnityEventTools.AddIntPersistentListener(b.onClick, app.SetDuration, minutes); form.durationButtons[i] = b;
            }
            Label(card, "Return Caption", "Estimated return", 15, 353, 300, 24, 15, color: Muted);
            form.estimatedReturn = Label(card, "Estimated Return", "Calculated at checkout", 10, 382, 310, 36, 20, true);
            form.contentsImage = Picture(p, "Contents Photograph", "contents", 843, 185, 295, 260);
            Label(p, "Checklist Heading", "Verify kit contents", 836, 463, 310, 36, 22, true);
            string[] items = { "1 Headset", "2 Controllers", "1 Charger" }; form.contents = new Toggle[3];
            for (int i = 0; i < 3; i++)
            {
                var tr = Rect(p, items[i] + " Toggle", 871, 508 + i * 42, 260, 38);
                var t = tr.gameObject.AddComponent<Toggle>();
                var hit = tr.gameObject.AddComponent<Image>(); hit.color = new Color(1,1,1,0);
                var box = Box(tr, "Checkbox Background", 0, 4, 29, 29, Color.white, true);
                var outline = box.gameObject.AddComponent<Outline>(); outline.effectColor = Blue; outline.effectDistance = new Vector2(1,-1);
                var check = Label(tr, "Checkmark", "✓", 0, 1, 29, 32, 26, true, Blue);
                t.targetGraphic = box; t.graphic = check; t.isOn = false;
                var label = Label(tr, "Item Label", items[i], 42, 0, 213, 38, 22); label.alignment = TextAnchor.MiddleLeft;
                if (i == 1) form.controllerLabel = label;
                UnityEventTools.AddPersistentListener(t.onValueChanged, form.ChecklistChanged); form.contents[i] = t;
            }
            Label(p, "Checklist Hint", "Check all three items to continue.", 817, 644, 350, 32, 15, color: Muted);
            form.reviewButton = Button(p, "Review Reservation Button", "Review Reservation", 432, 694, 355, 72, app.ReviewChoices); form.reviewButton.interactable = false;
        }
        static void Review()
        {
            var p = Screen("Review", "Review your reservation"); var v = p.gameObject.AddComponent<CheckoutDetailsView>(); app.review = v; v.app = app;
            v.questKit = Art("kit"); v.genericKit = Art("generic-kit"); v.artwork = Picture(p, "Selected Kit Photograph", "kit", 140, 230, 300, 300);
            var card = Box(p, "Review Card", 500, 217, 510, 378, new Color32(248,248,248,255), true).transform;
            v.heading = Label(card, "Kit Name", "Meta Quest 3 · Kit 1", 25, 25, 460, 50, 30, true);
            v.details = Label(card, "Reservation Details", "Duration, pickup and return", 30, 94, 450, 237, 22);
            Label(p, "Confirmation Hint", "Your loan starts when you confirm.", 410, 622, 640, 32, 19, color: Muted);
            Button(p, "Edit Choices Button", "Edit choices", 221, 700, 275, 68, app.EditChoices, true);
            Button(p, "Confirm Reservation Button", "Confirm Reservation", 535, 700, 424, 68, app.Confirm);
        }
        static void Receipt()
        {
            var p = Screen("Success", "Reservation successful"); var v = p.gameObject.AddComponent<CheckoutDetailsView>(); app.receipt = v; v.app = app; v.view = CheckoutDetailsView.View.Receipt;
            Picture(p, "Celebrating Panther", "celebrate", 436, 202, 342, 337);
            v.heading = Label(p, "Pickup Location", "Pick up at: CIAC 2", 180, 567, 840, 56, 38, true);
            v.details = Label(p, "Return Deadline", "Return deadline", 120, 624, 960, 54, 32, true);
            v.caption = Label(p, "Saved Reservation", "Reservation saved", 220, 678, 760, 31, 19, color: Muted);
            Button(p, "Back to Kits Button", "Back to kits", 445, 724, 310, 55, app.BackToInventory);
        }
        static void Waiting()
        {
            var p = Screen("Waiting", "Next available kit"); var v = p.gameObject.AddComponent<CheckoutDetailsView>(); app.waiting = v; v.app = app; v.view = CheckoutDetailsView.View.Waiting;
            Picture(p, "Waiting Panther", "waiting", 440, 196, 310, 285);
            v.heading = Label(p, "Availability Message", "All six Quest 3 kits are currently on loan.", 150, 499, 900, 48, 30, true);
            v.details = Label(p, "Next Return Details", "Next return and assistance", 130, 563, 940, 111, 23);
            v.actionLabel = Button(p, "Refresh Availability Button", "Refresh availability", 365, 706, 470, 65, app.RefreshAvailability).GetComponentInChildren<Text>();
        }
        static void Loading()
        {
            var p = Screen("Loading", "Going!!"); Label(p, "Loading Caption", "Loading…", 400, 165, 400, 40, 23, color: Muted);
            app.transition = app.gameObject.AddComponent<PantherTransition>();
            app.transition.panther = Picture(p, "Moving Panther", "waiting", -300, 290, 300, 340).rectTransform;
        }
        static void Shared()
        {
            var home = Button(content, "Home Button", "", 1095, 24, 64, 58, card: true); Route(home, "Home"); Picture(home.transform, "Home Icon", "home", 3, 3, 54, 48); app.homeButton = home.gameObject;
            var footer = Rect(content, "Footer", 0, 0, 1200, 854); app.footer = footer.gameObject;
            var label = Label(footer, "Kiosk Label", "QUEST CHECKOUT  /  CIAC 2", 30, 822, 400, 22, 12, color: Muted); label.alignment = TextAnchor.MiddleLeft;
            var tests = Button(footer, "Test Controls Button", "Test controls", 1030, 812, 140, 34, app.OpenTestControls, true); tests.GetComponentInChildren<Text>().fontSize = 14;
            var overlay = Box(content, "Test Controls Dialog", 0, 0, 1200, 854, new Color(.1f,.17f,.2f,.35f)); app.resetDialog = overlay.gameObject;
            var card = Box(overlay.transform, "Dialog Card", 260, 155, 680, 535, new Color32(248,248,248,255), true).transform;
            Label(card, "Dialog Heading", "Test controls", 30, 25, 620, 55, 32, true);
            Label(card, "Reset Explanation", "Reset replaces all saved loans on this device.\nA: four Quest kits available. B: all six on loan.\nBoth resets make Generic VR 1 available.", 40, 95, 600, 108, 21);
            Button(card, "Reset Scenario A Button", "Reset to A · available kits", 65, 234, 550, 65, app.ResetScenarioA);
            Button(card, "Reset Scenario B Button", "Reset to B · no kits available", 65, 317, 550, 65, app.ResetScenarioB);
            Button(card, "Cancel Button", "Cancel", 220, 426, 240, 57, app.CloseTestControls, true);
            app.alert = Label(content, "Validation Message", "", 160, 767, 880, 40, 17, color: new Color32(150,44,30,255));
            app.homeButton.AddComponent<CanvasGroup>(); app.footer.AddComponent<CanvasGroup>();
        }
        // Final migration pass; normal builds only validate and use the saved prefab.
        public static void FinalizeMigrationAndBuild()
        {
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            var controller = prefab.GetComponent<QuestCheckoutApp>();
            foreach (var screen in controller.screens) if (screen.GetComponent<CanvasGroup>() == null) screen.gameObject.AddComponent<CanvasGroup>();
            if (controller.homeButton.GetComponent<CanvasGroup>() == null) controller.homeButton.AddComponent<CanvasGroup>();
            if (controller.footer.GetComponent<CanvasGroup>() == null) controller.footer.AddComponent<CanvasGroup>();
            foreach (var hover in prefab.GetComponentsInChildren<PantherHover>(true))
            {
                var b = hover.GetComponent<Button>(); var colors = b.colors;
                colors.highlightedColor = colors.pressedColor = colors.selectedColor = Color.white; b.colors = colors;
            }
            var selected = controller.reservation.durationButtons[2]; selected.GetComponent<Image>().color = Blue; selected.GetComponentInChildren<Text>().color = Color.white;
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath); PrefabUtility.UnloadPrefabContents(prefab);
            foreach (string file in new[] { "Checkout.uss", "CheckoutTheme.tss", "CheckoutPanel.asset" }) AssetDatabase.DeleteAsset("Assets/QuestCheckout/Resources/" + file);
            AssetDatabase.SaveAssets();
            CheckoutBuild.Build();
        }
        [MenuItem("Quest Checkout/Validate Canvas scene")]
        public static void ValidateScene()
        {
            var root = UnityEngine.Object.FindAnyObjectByType<QuestCheckoutApp>();
            if (root == null || root.GetComponent<Canvas>() == null) throw new Exception("Missing Canvas root.");
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
                if (button.onClick.GetPersistentEventCount() == 0 || button.onClick.GetPersistentTarget(0) == null) throw new Exception("Unwired button: " + button.name);
            if (root.screens.Length != 9 || root.reservation.contents.Length != 3) throw new Exception("Missing Canvas screen or checklist.");
            if (root.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "UIDocument")) throw new Exception("Legacy UI Toolkit component remains.");
            Debug.Log($"QUEST_CANVAS_SCENE_PASS: {root.screens.Length} screens, {buttons.Length} serialized buttons, {root.GetComponentsInChildren<Image>(true).Length} Images, 3 Toggles.");
        }
    }
}
