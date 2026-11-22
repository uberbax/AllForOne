using UnityEngine;
using UnityEngine.UI;

public class UIturnOrder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform holder;
    public float scl = 2;
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
            if (obj.whats.Count <= l) break;
            Transform g0 = null;
            if (obj.whats[l].tags.Contains("player"))
            {
                g.GetComponent<Image>().color = Color.green;
                g0 = g.transform.Find("icon");
                if (g0 == null) g0 = g.transform.Find("Image/icon");
                if (g0) g0.localScale = new Vector3(scl, scl, scl);
            }
            else
            {
                g0 = g.transform.Find("icon");
                if (g0 == null) g0 = g.transform.Find("Image/icon");
                if (g0) g0.localScale = new Vector3(1, 1, 1);
            }

            g0.GetComponent<Image>().sprite = ResourceHolder.instance.avas[obj.whats[l].dbObj.ID];
            l++;
            if (l >= obj.whats.Count)
                l = 0;
        }
    }
}
