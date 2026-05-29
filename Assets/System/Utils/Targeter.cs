using UnityEngine;

public class Targeter : MonoBehaviour
{
    //aim
    public  GameObject target;
    
    public RObj find = null;

    public bool handleExec = false;
    public bool reqFind = false;
    
    public virtual void MouseMoved(Vector3 pos)
    {
        target.transform.position = pos;
    }

    public virtual void Deactivate()
    {
        gameObject.SetActive(false);
        target.transform.position = new Vector3(1000, 1000, 1000);
    }

    public virtual void HandleExec()
    {
        
    }
    
}
