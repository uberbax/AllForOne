using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainCycleDung : MonoBehaviour
{
    public GameObject player;
    public Button endTurn;
    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);
        endTurn.onClick.AddListener(() => EndTurn());
    }

    private void B(ArgPass obj)
    {
        Debug.Log("haha");

        var main = new RObj("hero", 1, 1, true, Vector3.zero,true, ItemType.monster, "main_player");
        MainStates.instance.ApplyPlayerConfigParams(main);
        main.main = player;
        var oo = player.AddComponent<ObjHolder>();
        oo.obj = main;
        main.AddViz("coll");
        main.AddViz("combat");

        //var str = JsonConvert.SerializeObject(main);
        //Debug.Log(str);
        
        ModelStatistics.instance.UpdateAllTasks();
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var enm = UtilsControl.GetMeClosest(null, UtilsControl.FindPlayer().transform.position, out float dd, "Enemy");
            Debug.Log(enm);
        }
        
        
        if (Input.GetKeyDown("i"))
        {
            MainStates.instance.mainPlayer.ChangePar("registered_damage", 20);
        }
        
        /*
        if (Input.GetKeyDown("u"))
        {
            MainStates.instance.UI_skilChose.SetActive(true);
        }
        */
        
    }

    public void EndTurn()
    {
        StartCoroutine(DoEnemyTurn());
    }

    public IEnumerator DoEnemyTurn()
    {
        var gg= MainStates.instance.combats.FindAll(x => x.META_TAGS.Contains("CUR_BATTLE"));
        for (int i = 0; i < gg.Count; i++)
        {
            gg[i].main.GetComponentInChildren<XDcombat>().Iteration(true);
            yield return new WaitForSeconds(1f);
        }
               
    }
}
