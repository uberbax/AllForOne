using UnityEngine;
using UnityEngine.UI;

public class UIturnOrder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform holder;
    void Awake()
    {
        EventManager.SUB("turn_order", TurnOrder);
    }

    private void TurnOrder(ArgPass obj)
    {
        int l = 0;
        for (int i = holder.childCount - 1; i >= 0; i--)
        {
            var g = holder.GetChild(i);
            if (obj.whats[l].tags.Contains("player"))
                g.GetComponent<Image>().color = Color.green;
            else
            {
                g.GetComponent<Image>().color = Color.red;
            }

            g.Find("icon").GetComponent<Image>().sprite = ResourceHolder.instance.avas[obj.whats[l].dbObj.ID];
            l++;
            if (l >= obj.whats.Count)
                l = 0;
        }
    }
}
