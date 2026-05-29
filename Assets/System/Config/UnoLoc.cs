using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnoLoc : MonoBehaviour
{
    // Start is called before the first frame update
    public string loco = "";
    void Start()
    {
        var str = ConfigLoader.Instance.GetMeLocale(loco);
        if (str == "")
        {
            Invoke("Start", 0.1f);
        }
        else
        {
            GetComponent<TextMeshProUGUI>().text = ConfigLoader.Instance.GetMeLocale(loco, this);            
        }


    }

    // Update is called once per frame

}
