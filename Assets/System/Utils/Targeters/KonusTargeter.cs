using UnityEngine;

public class KonusTargeter : Targeter
{

    public GameObject konus;
    public override void MouseMoved(Vector3 pos)
    {
        base.MouseMoved(pos);
        var da = SkillExecutor.instance.lastSkl.GetPar("angle") * Mathf.Deg2Rad;
        
        var vec = pos + new Vector3(0, MainStates.instance.LIFT_PROJ, 0) - MainStates.instance.mainPlayer.Position;
        Vector3 rotatedVector1 = Quaternion.Euler(0, 0, da) * vec;
        Vector3 rotatedVector2 = Quaternion.Euler(0, 0, -da) * vec;

        UtilsControl.Instance.BuildAngleFillMesh(konus, MainStates.instance.mainPlayer.Position, rotatedVector2, rotatedVector1);
        
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }
}
