using System;
using System.Collections;
using UnityEngine;
namespace QuestCheckout
{
    public sealed class PantherTransition : MonoBehaviour
    {
        public RectTransform panther;
        public float duration = 1.15f;
        public bool IsPlaying { get; private set; }
        public void Play(Action complete) { if (!IsPlaying) StartCoroutine(Animate(complete)); }
        IEnumerator Animate(Action complete)
        {
            IsPlaying = true;
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < duration)
            {
                float t = (Time.unscaledTime - start) / duration;
                panther.anchoredPosition = new Vector2(Mathf.Lerp(-300, 1200, t), -290 + Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6)) * 17);
                yield return null;
            }
            IsPlaying = false; complete();
        }
    }
}
