using UnityEngine;

public class PositionHolder : MonoBehaviour
{
    public Transform who;
    void Start()
    {
        EventManager.SUB("battle_start", (x) =>
        {
            who = null;
        });        
    }


}
