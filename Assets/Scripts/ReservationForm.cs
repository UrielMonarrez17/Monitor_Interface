using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ReservationForm : MonoBehaviour
{
    public QuestCheckoutApp app;
    public Text title, kitName, startDate, estimatedReturn, controllerLabel;
    public Image kitImage, contentsImage;
    public Sprite questKit, questContents, genericKit;
    public Toggle[] contents;
    public Button[] durationButtons;
    public int[] durations = { 30, 60, 90, 120 };
    public Button reviewButton;
    public Color selectedColor = new Color32(105, 162, 184, 255);
    public bool[] Verified => contents.Select(t => t.isOn).ToArray();

    public void Refresh()
    {
        title.text = app.Generic ? $"Generic VR {app.SelectedKit}" : "Meta Quest 3 Reserve";
        kitName.text = app.KitName;
        kitImage.sprite = app.Generic ? genericKit : questKit;
        contentsImage.sprite = app.Generic ? genericKit : questContents;
        controllerLabel.text = app.Generic ? "1 Controller" : "2 Controllers";
        startDate.text = DateTime.Now.ToString("ddd, dd MMM yyyy");
        estimatedReturn.text = QuestCheckoutApp.Format(DateTime.UtcNow.AddMinutes(app.Duration));
        for (int i = 0; i < durationButtons.Length; i++)
        {
            bool selected = durations[i] == app.Duration;
            durationButtons[i].GetComponent<Image>().color = selected ? selectedColor : Color.white;
            durationButtons[i].GetComponentInChildren<Text>().color = selected ? Color.white : new Color32(52, 106, 127, 255);
        }
        ChecklistChanged(false);
    }

    public void ChecklistChanged(bool _) => reviewButton.interactable = contents.All(t => t.isOn);
    public void ClearChecklist() { foreach (var toggle in contents) toggle.SetIsOnWithoutNotify(false); ChecklistChanged(false); }
}
