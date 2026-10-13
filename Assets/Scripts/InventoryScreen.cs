using System;
using UnityEngine;
using UnityEngine.UI;
namespace QuestCheckout
{
    public sealed class InventoryScreen : MonoBehaviour
    {
        public QuestCheckoutApp app;
        public bool generic;
        public KitCardView[] cards;
        public Text availability, nextReturn;
        public void Refresh()
        {
            var store = generic ? app.GenericStore : app.QuestStore;
            int count = store.AvailableCount(DateTime.UtcNow);
            foreach (var card in cards) card.Refresh(store.Available(card.kit, DateTime.UtcNow), generic);
            if (availability != null) availability.text = !store.CanWrite ? "Inventory unavailable" : count == 0 ? "0 of 6 kits available • See the next return below" : $"{count} of 6 kits available • Select a kit to continue";
            if (nextReturn != null)
            {
                nextReturn.gameObject.SetActive(count == 0 && store.CanWrite);
                nextReturn.text = (generic ? "Next return: " : "All six kits are on loan. Next return: ") + QuestCheckoutApp.Format(store.NextAvailable(DateTime.UtcNow));
            }
        }
    }
}
