using UnityEngine;

public class UnoObject : MonoBehaviour
{
    private RObj obj;
    private string prevId = "";

    public float scl = 1;
    // Update is called once per frame
    void Update()
    {
        var g = GetComponentInParent<ObjHolder>();
        if (g == null || g.obj == null) return;
        obj = g.obj;
        if (prevId != obj.dbObj.ID)
        {
            prevId = obj.dbObj.ID;
            //var b = GetComponent<SpriteRigToUI>();
            UtilsControl.Instance.PlaceItemInRender(prevId);
            //b.sourceRoot = ResourceHolder.instance.monsters[prevId].transform;
        }
    }
}
