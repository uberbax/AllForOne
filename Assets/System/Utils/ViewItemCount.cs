using System.Globalization;
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

    public int delimeter = 0;
    
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
        {
            if (delimeter > 0)
                amount.text = c.ToString("N0", CultureInfo.InvariantCulture/*CultureInfo.GetCultureInfo("de-DE")*/);
            else amount.text = c.ToString();
        }
    }
}
