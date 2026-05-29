using UnityEngine;

public class AimTargeter : Targeter
{
    public LineRenderer line;
    public override void MouseMoved(Vector3 pos)
    {
        base.MouseMoved(pos);
        line.enabled = true;
        line.SetPositions(new []{ transform.position, pos });
        
    }

    public override void Deactivate()
    {
        base.Deactivate();
        line.enabled = false;
    }
}
