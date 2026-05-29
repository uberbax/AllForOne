using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheEx : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Excel4Unity.Read();
        
        //string path = Application.dataPath + "/Test/Test3.xlsx";
        //Excel xls =  ExcelHelper.LoadExcel(path);
        //xls.ShowLog();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
