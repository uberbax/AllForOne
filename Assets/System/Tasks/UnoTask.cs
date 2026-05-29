using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UnoTask : MonoBehaviour
{
    public bool clickShowRewards = true;
    public bool clickReadyCollect = false;
    
    public string taskID = "";
    public ElTasko task = null;
    
    //not ready, ready, taken
    [Header("NotReady, Ready, Taken")]
    public List<GameObject> states = new List<GameObject>();

    public GameObject[] items;
    public Image[] fillItems;
    public TextMeshProUGUI[] textItemsProgr;
    public TextMeshProUGUI[] textItemsSingle;
    public TextMeshProUGUI[] taskNames;

    [Header("Collect buttons")] 
    public Button collectBtn;
    public Button goThereBtn;
    public List<Button> showRewards;

    private bool isCollected = false;

    private void Start()
    {
        if (showRewards != null)
        {
            foreach (var v in showRewards)
            {
                v.onClick.RemoveAllListeners();
                v.onClick.AddListener(() =>
                {
                    MainStates.instance.curSmalls = MainStates.instance.CreateItems(task.rewards);
                    MainStates.instance.posClick = v.transform;
                    MainStates.instance.UI_smalls.SetActive(true);
                    //PopupoManager.instance.ShowRewardsSmall(task.rewards, v.transform);
                });
            }
        }
        
        
        //popupomanager.instance.showrewards
        //mainstates.instance.inventary.additems

        if (collectBtn != null)
        {
            collectBtn.onClick.AddListener(() => ModelStatistics.instance.TakeTaskReward(task));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (task == null || task.id == null || task.id == "")
        {
            task = DatabaseAll.instance.allTasks[taskID];
            Debug.Log("Checking:" + task);
            if (task != null)
            {
                for (int i = 0 ; i < items.Length; i++)
                    if (items[i] != null) UISystem.instance.FillItem(task.rewards[0], items[i]); 

                for (int i = 0; i < taskNames.Length; i++)
                    if (taskNames[i] != null) taskNames[i].text = task.description;
            }
        }
        if (task == null) return;
        
        var pep = MainStates.instance.playerData.playerTasks.Find(x => x.id == taskID);
        //if (pep == null)
        //{
        //    pep = new TasksProg {id = taskID};
        //    MainStates.instance.playerData.playerTasks.Add(pep);
        //}

        float me = 0;
        float all = 0;
        ModelStatistics.instance.GetMeProgress(task, out me, out all);

        for (int i = 0; i < fillItems.Length; i++)
        {
            if (fillItems[i] != null) fillItems[i].fillAmount = me / all;
        }

        for (int i = 0; i < textItemsProgr.Length; i++)
        {
            if (textItemsProgr[i] != null) textItemsProgr[i].text = me + "/" + all;            
        }
        
        for (int i = 0; i < textItemsSingle.Length; i++)
        {
            if (textItemsSingle[i] != null) textItemsSingle[i].text = all.ToString();           
        }


        for (int i = 0; i < states.Count; i++)
            states[i].SetActive(false);

        if (pep.taken)
        {
            states[2].SetActive(true);
        }
        else if (all <= me)
        {
            states[1].SetActive(true);
        }
        else
        {
            states[0].SetActive(true);
        }
    }
}
