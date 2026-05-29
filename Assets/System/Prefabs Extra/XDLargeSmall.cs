using UnityEngine;

public class XDLargeSmall : ComponentBehavior
{
    void Start()
    {
        UtilsControl.Instance.LargeSmall(transform.parent, () =>
        {
            MiscParams.Removeso(transform.parent.gameObject, "largesmall");
        });
    }


}
