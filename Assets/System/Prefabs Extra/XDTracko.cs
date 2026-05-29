using UnityEngine;

public class XDTracko : ComponentBehavior
{
    private RObj mon;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private int prevHp = -1;

    private int prevAtk = -1;

    void Update()
    {
        var atk = mon.GetPar("attack");
        if (prevAtk >= 0 && prevAtk != atk)
        {
            //UtilsControl.Instance.FlyText3D(transform.position, (atk - prevAtk).ToString(), Color.green, new Vector3(0, 1, 0));
            UtilsControl.Instance.FlyTextUI(transform, (atk - prevAtk).ToString(), Color.green, ResourceHolder.instance.misc["attack"], doCam:true, dltY:100);
            
        }
        
        var hp = mon.GetPar("health");
        if (prevHp >= 0 && prevHp != hp)
        {
            //UtilsControl.Instance.FlyText3D(transform.position, (hp - prevHp).ToString(), Color.green, new Vector3(0, 1, 0));
            UtilsControl.Instance.FlyTextUI(transform, (hp - prevHp).ToString(), Color.green, ResourceHolder.instance.misc["health"], doCam:true, dltY:100);
            
        }

        prevHp = (int)hp;
        prevAtk = (int)atk;

    }
}
