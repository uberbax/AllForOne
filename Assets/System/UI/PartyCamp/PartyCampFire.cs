using UnityEngine;
public sealed class PartyCampFire : MonoBehaviour
{
    [SerializeField] private Transform flame, glow;
    private Vector3 flameScale, glowScale;
    private void Awake() { flameScale = flame.localScale; glowScale = glow.localScale; }
    private void Update()
    {
        float wave = Mathf.Sin(Time.unscaledTime * 6.7f) * .06f + Mathf.Sin(Time.unscaledTime * 11.3f) * .025f;
        flame.localScale = Vector3.Scale(flameScale, new Vector3(1-wave,1+wave,1));
        glow.localScale = glowScale * (1 + wave * .5f);
    }
}
