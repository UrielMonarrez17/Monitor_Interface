using UnityEngine;
using UnityEngine.EventSystems;
namespace QuestCheckout
{
    public sealed class PantherHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform artwork;
        public GameObject selectingPanther, blockingPanther;
        public bool available = true;
        [Range(1, 1.5f)] public float zoom = 1.15f;
        public float speed = 12;
        bool hovering;
        public void OnPointerEnter(PointerEventData data) { hovering = true; UpdatePose(); }
        public void OnPointerExit(PointerEventData data) { hovering = false; UpdatePose(); }
        void Update() => artwork.localScale = Vector3.Lerp(artwork.localScale, Vector3.one * (hovering ? zoom : 1), Time.unscaledDeltaTime * speed);
        public void SetAvailable(bool value) { available = value; UpdatePose(); }
        void UpdatePose() { selectingPanther.SetActive(hovering && available); blockingPanther.SetActive(hovering && !available); }
        void OnDisable() { hovering = false; if (artwork != null) artwork.localScale = Vector3.one; if (selectingPanther != null && blockingPanther != null) UpdatePose(); }
    }
}
