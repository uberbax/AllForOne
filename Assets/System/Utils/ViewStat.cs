using TMPro;
using UnityEngine;

public class ViewStat : MonoBehaviour
{
    public string statName;
    public TextMeshProUGUI statText;

    void Start()
    {
        statText = GetComponentInChildren<TextMeshProUGUI>();
    }
    // Update is called once per frame
    void Update()
    {
        var d = ModelStatistics.instance.GetStatValue(statName);
        statText.text = d.ToString();
    }
}
