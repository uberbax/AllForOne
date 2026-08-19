using UnityEngine;

public class GUIUnitPrefab : MonoBehaviour
{
    public GUIUnit unit;

    public void Fill(RObj value)
    {
        unit?.Fill(value);
    }
}
