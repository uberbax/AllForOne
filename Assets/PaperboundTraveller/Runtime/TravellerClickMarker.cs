using UnityEngine;
namespace Paperbound.Traveller
{
    public sealed class TravellerClickMarker : MonoBehaviour
    {
        public SpriteRenderer sprite;
        [Min(.05f)] public float duration = .78f;
        float age;
        public void Show(Vector3 point, Vector3 normal)
        {
            gameObject.SetActive(true); age = 0;
            transform.position = point;
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, normal);
            Apply();
        }
        void Update() { age += Time.deltaTime; Apply(); if (age >= duration) gameObject.SetActive(false); }
        void Apply()
        {
            float t = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(.12f, .35f, t);
            if (sprite) sprite.color = new Color(1, 1, 1, .7f * (1 - t));
        }
    }
}
