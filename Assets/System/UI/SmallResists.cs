using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmallResists : MonoBehaviour
{
    private ObjHolder other;
    private RObj mon;

    public Transform resistsRoot;
    public Transform immunitiesRoot;
    public Transform weaknessRoot;
    
    private void OnEnable()
    {
        //if (other == null)
        //{
            other = GetComponentInParent<ObjHolder>();
            mon = other.obj;
        //}
        
        Fill();
    }

    public void Fill()
    {
        
        
        

    }


}
