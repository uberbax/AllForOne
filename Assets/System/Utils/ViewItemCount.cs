using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewItemCount : MonoBehaviour
{
    public float max = 100;
    public string itemName;
    public Image fill;
    public Image icon;
    public TextMeshProUGUI amount;
    
    void Start()
    {
        if (fill == null)
            fill = GetComponent<Image>();

        if (icon)
            icon.sprite = ResourceHolder.instance.items[itemName];
    }

    // Update is called once per frame
    void Update()
    {
        var c = MainStates.instance.GetItemsCount(itemName);
        if (fill) fill.fillAmount = c / max;
        if (amount)
            amount.text = c.ToString();
    }
}
