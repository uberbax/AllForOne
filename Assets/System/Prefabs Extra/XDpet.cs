using UnityEngine;

public class XDpet : MonoBehaviour
{
    public string prevID = "";
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var bb = MainStates.instance.mainPlayer.inventory.FindAll(x => x.it == ItemType.monster && x.GetPar("used_slot") >= 0);
        if (bb.Count > 0)
        {
            if (bb[0].dbObj.ID != prevID)
            {
                MainStates.instance.ReplaceVisual(MainStates.instance.all["main_pet"], ResourceHolder.instance.monsters[bb[0].dbObj.ID]);
            }
            
            prevID = bb[0].dbObj.ID;
        }
        else if (prevID != "")
        {
            MainStates.instance.ReplaceVisual(MainStates.instance.all["main_pet"], ResourceHolder.instance.monsters["empty"]);
            
            prevID = "";
        }
        
        var mon = MainStates.instance.all["main_pet"];
        var h = MainStates.instance.all["main_player"].Position - MainStates.instance.all["main_pet"].Position;
        h.z = 0;
        if (h.magnitude > 1)
        {
            if (h.x > 0)
            {
                mon.SetScale(h.x > 0);
            }
            else if (h.x < 0)
            {
                mon.SetScale(h.x > 0);
            }

            mon.main.transform.position += h.normalized * Time.deltaTime;
        }
    }
}
