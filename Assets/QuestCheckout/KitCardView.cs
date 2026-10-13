using UnityEngine;
using UnityEngine.UI;
namespace QuestCheckout
{
    public sealed class KitCardView : MonoBehaviour
    {
        public int kit = 1;
        public Image artwork;
        public Sprite availableImage, unavailableImage;
        public Text title, status;
        public PantherHover hover;
        public void Refresh(bool available, bool generic)
        {
            artwork.sprite = available ? availableImage : unavailableImage;
            title.text = available ? (generic ? $"Generic VR {kit}" : $"Kit {kit}") : "Unavailable";
            if (status != null) status.text = available ? "" : $"Kit {kit} • on loan";
            hover.SetAvailable(available);
        }
    }
}
