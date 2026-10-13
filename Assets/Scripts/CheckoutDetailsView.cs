using System;
using UnityEngine;
using UnityEngine.UI;

public class CheckoutDetailsView : MonoBehaviour
{
    public enum View { Review, Receipt, Waiting }
    public View view;
    public QuestCheckoutApp app;
    public Text heading, details, caption, actionLabel;
    public Image artwork;
    public Sprite questKit, genericKit;

    public void Refresh()
    {
        if (view == View.Review)
        {
            artwork.sprite = app.Generic ? genericKit : questKit;
            heading.text = app.KitName;
            details.text = $"Duration: {app.Duration} minutes\nPick up at: {LoanStore.Pickup}\nEstimated return: {QuestCheckoutApp.Format(DateTime.UtcNow.AddMinutes(app.Duration))}\n\nVerified: 1 headset, {(app.Generic ? "1 controller" : "2 controllers")}, 1 charger";
        }
        else if (view == View.Receipt)
        {
            heading.text = "Pick up at: " + LoanStore.Pickup;
            details.text = "Return: " + QuestCheckoutApp.Format(new DateTime(app.LastReceipt.dueUtc, DateTimeKind.Utc));
            caption.text = $"{app.KitName} • {app.Duration} minutes • Reservation saved";
        }
        else
        {
            bool available = app.QuestStore.AvailableCount(DateTime.UtcNow) > 0;
            heading.text = available ? "A kit is now available" : "All six Quest 3 kits are currently on loan.";
            details.text = available ? "Return to the kit list to start your checkout." : "Earliest return: " + QuestCheckoutApp.Format(app.QuestStore.NextAvailable(DateTime.UtcNow)) + "\nFor immediate help or other equipment, visit CIAC 2.";
            actionLabel.text = available ? "Choose an available kit" : "Refresh availability";
        }
    }
}
