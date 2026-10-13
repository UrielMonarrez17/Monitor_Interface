using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace QuestCheckout.Editor
{
    // Run once in the editor: the resulting Dropdown is saved as editable Canvas objects.
    public static class ReservationDaySetup
    {
        public static void ApplyAndBuild()
        {
            const string path = "Assets/Prefabs/QuestCheckoutCanvas.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var app = root.GetComponent<QuestCheckoutApp>();
                var form = app.reservation;
                var card = form.startDate.transform.parent;
                card.Find("Start Caption").GetComponent<Text>().text = "RESERVATION DAY";
                card.Find("Header Text").GetComponent<Text>().text = "Day and Duration";
                var hint = form.startDate.rectTransform;
                hint.anchoredPosition = new Vector2(18, -154);
                hint.sizeDelta = new Vector2(294, 24);
                form.startDate.fontSize = 14;
                form.startDate.text = "Starts when you confirm";
                if (form.reservationDay == null)
                {
                    var resources = new DefaultControls.Resources
                    {
                        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                        dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                        mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
                    };
                    var go = DefaultControls.CreateDropdown(resources);
                    go.name = "Reservation Day Dropdown";
                    var rect = go.GetComponent<RectTransform>();
                    rect.SetParent(card, false);
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(18, -112);
                    rect.sizeDelta = new Vector2(294, 40);
                    form.reservationDay = go.GetComponent<Dropdown>();
                    form.reservationDay.ClearOptions();
                    form.reservationDay.AddOptions(new System.Collections.Generic.List<string> { "Today", "Tomorrow", "In 2 days", "In 3 days", "In 4 days", "In 5 days", "In 6 days" });
                    foreach (var label in go.GetComponentsInChildren<Text>(true))
                    {
                        label.font = form.startDate.font;
                        label.fontSize = 19;
                        label.color = new Color32(52, 106, 127, 255);
                    }
                    UnityEventTools.AddPersistentListener(form.reservationDay.onValueChanged, app.SetReservationDay);
                }
                var dropdown = form.reservationDay;
                dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 252);
                var item = dropdown.template.GetComponentInChildren<Toggle>(true).GetComponent<RectTransform>();
                item.sizeDelta = new Vector2(item.sizeDelta.x, 34);
                var listContent = (RectTransform)item.parent;
                listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, 34);
                dropdown.itemText.fontSize = 17;
                dropdown.itemText.verticalOverflow = VerticalWrapMode.Overflow;
                app.review.transform.Find("Confirmation Hint").GetComponent<Text>().text = "Today starts on confirmation. Future pickup is at 9:00 AM.";
                app.review.details.fontSize = 20;
                app.receipt.caption.fontSize = 17;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            CheckoutBuild.Build();
        }
    }
}
