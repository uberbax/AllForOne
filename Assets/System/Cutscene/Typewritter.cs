using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Typewritter : MonoBehaviour
{
    // Start is called before the first frame update
    public TextMeshProUGUI txt;
    public TextMeshPro txt1;

    private string toComplete = string.Empty;
    int cnt = 0;
    private float tm = 0;

    private float spd = 0.1f;
    
    public void SetText(string txt)
    {
        toComplete = txt;
        cnt = 0;
        tm = 0;
    }
    
    
    void Update()
    {
        tm += Time.deltaTime;
        cnt = (int)(tm / spd);

        if (cnt > toComplete.Length) cnt = toComplete.Length;
        
        if (txt != null)
            txt.text = toComplete.Substring(0, cnt);
        else
            txt1.text = toComplete.Substring(0, cnt);

    }
}
