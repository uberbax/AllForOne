using UnityEngine;

public class UnityLightRaysPlaying : MonoBehaviour
{
    public float minAlpha = 0.2f;
    public float maxAlpha = 1.0f;
    public float frequency = 1.0f;

    private SpriteRenderer spriteRenderer;
    private Color startColor;
    
    float clock;
    float alphaK;

    void Start()
    {
        clock = Random.Range(-10f, 10f);
        spriteRenderer = GetComponent<SpriteRenderer>();
        startColor = spriteRenderer.color;
    }

    void Update()
    {
        clock += Time.deltaTime;

        if (alphaK < 1)
            alphaK += Time.deltaTime;
        
        float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(clock * frequency) + 1f) / 2f);
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha * alphaK);
    }
}