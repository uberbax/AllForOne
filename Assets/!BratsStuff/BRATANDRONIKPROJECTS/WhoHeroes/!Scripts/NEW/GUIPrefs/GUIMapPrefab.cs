using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIMapPrefab : MonoBehaviour
{
    public GameObject plus;
    Button plusb;
    public GameObject minus;
    Button minusb;
    public Transform holder;

    public bool state = true;

    List<GameObject> allplace= new List<GameObject>();

    void Awake()
    {
        plusb = plus.GetComponentInChildren<Button>();
        minusb = minus.GetComponentInChildren<Button>();

        for(int i=1; i< holder.childCount; i++)
            allplace.Add(holder.GetChild(i).gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        plusb.onClick.AddListener(()=> SwitchState(true));
        minusb.onClick.AddListener(()=> SwitchState(false));

        SwitchState(true);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SwitchState(bool st)
    {
        state = st;
        plus.SetActive(!state);
        minus.SetActive(state);

        for(int i=0; i< allplace.Count; i++)
            allplace[i].SetActive(state);
        
    }

}
