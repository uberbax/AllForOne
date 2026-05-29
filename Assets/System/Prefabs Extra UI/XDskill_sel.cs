using TMPro;
using UnityEngine;

public class XDskill_sel : ComponentUIBehavior
{
    public GameObject inactive;
    public GameObject selected;
    public TextMeshProUGUI skNum;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Fill()
    {
        base.Fill();
        var itm = GetComponentInParent<ObjHolder>().obj;

        var h0 = itm.dynamic;
        var h1 = UtilsControl.GetLowest(h0.id);
        var hb = UtilsControl.GetNum(h0.id);

        RectTransform rt = GetComponent<RectTransform>();
        float width = rt.rect.width;
        float height = rt.rect.height;
        skNum.GetComponent<RectTransform>().sizeDelta = new Vector2(width/5, height/5);
        
        skNum.text = hb + "/3";
        
        
        inactive.SetActive(h0.id == h1);
        selected.SetActive(itm.GetPar("used_slot") >= 0);

    }

    void Update()
    {
        Fill();
    }


}
