using UnityEngine;
using UnityEngine.UI;

public class ViewItemFill : MonoBehaviour
{
    public float max = 100;
    public string itemName;
    public Image fill;
    void Start()
    {
        if (fill == null)
            fill = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        var c = MainStates.instance.GetItemsCount(itemName);
        fill.fillAmount = c / max;
    }
}
