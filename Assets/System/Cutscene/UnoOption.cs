using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnoOption : MonoBehaviour
{
    public FormatDialogue fd;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener( () => Dialoguer.instance.ClickOption(this));
    }
    
}
