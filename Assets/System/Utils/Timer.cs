using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public bool untilNextWave = false;
    public bool systemTimer = false;
    public long untilTime = 0;

    public WaveSpawner spawner;
    
    [SerializeField]
    TextMeshProUGUI text;

    void Start()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }
    
    void Update()
    {
        if (untilNextWave)
        {
            text.text = ((int)spawner.timeNext).ToString();
        }
    }
}
