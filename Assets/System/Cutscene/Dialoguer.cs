using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Dialoguer : MonoBehaviour
{
    public static Dialoguer instance;

    public GameObject bubble;
    public GameObject dialog;
    
    public Image ava1;
    public Image ava2;
    public TextMeshProUGUI avaT1;
    public TextMeshProUGUI avaT2;
    public TextMeshProUGUI message;

    public Button nextBtn;

    public GameObject av1;
    public GameObject av2;
    public GameObject fade1;
    public GameObject fade2;

    public List<Button> options = new List<Button>();
    private void OnDestroy()
    {
        instance = null;
    }
    
    private void Awake()
    {
        instance = this;
        nextBtn.onClick.AddListener(() =>
        {
            if (lastDialog[1].phrase == "next")
            {
                if (lastDialog[1].action == "end")
                {
                    Hide();
                }
                else if (lastDialog[1].action == "cut")
                {
                    Hide();
                    Cutscener.instance.ExecuteCutscene(lastDialog[1].ava1);
                }
                else
                    ShowDialogue(lastDialog[1].action);
            }

        });
    }

    public void Hide()
    {
        dialog.SetActive(false);
        UtilsControl.Instance.CheckCamBack();

        if (lastNpc != null)
        {
            //var yy = lastNpc.transform.GetComponentInChildren<Animator>();
            var zz = lastNpc.transform.GetComponentInChildren<DefaultAnim>();
            if (zz != null)
            {
                zz.Do();
            }
        }
    }

    private List<FormatDialogue> lastDialog;
    private GameObject lastNpc;
    private FormatDynamic lastDynamic;
    public void ShowDialogue(string id, GameObject npc = null)
    {
        lastNpc = npc;
        if (npc != null)
        {
            MainStates.instance.curLoot = npc.GetComponentInParent<ObjHolder>().obj;
            lastDynamic = npc.GetComponentInParent<Buyable>()?.curDynamic;
        }
        lastDialog = ConfigLoader.Instance.dictDialogues[id];
        
        dialog.SetActive(true);
        ava1.sprite = ResourceHolder.instance.GetDiaAva(lastDialog[0].ava1);
        ava2.sprite = ResourceHolder.instance.GetDiaAva(lastDialog[0].ava2);
        avaT1.text = lastDialog[0].avaT1;
        avaT2.text = lastDialog[0].avaT2;

        av2.SetActive(lastDialog[0].ava2 != "x" && lastDialog[0].ava2 != "");
        av1.SetActive(lastDialog[0].ava1 != "x" && lastDialog[0].ava1 != "");
        fade2.SetActive(lastDialog[0].ava2 != lastDialog[0].speaker);
        fade1.SetActive(lastDialog[0].ava1 != lastDialog[0].speaker);

        
        //play mumbling ?
        message.text = lastDialog[0].phrase;
        GetComponent<Typewritter>().SetText(lastDialog[0].phrase);

        for (int i = 0; i < options.Count; i++)
        {
            options[i].gameObject.SetActive(i < lastDialog.Count - 1);
        }

        var yy = lastDialog.Find(x => x.phrase == "next");
        nextBtn.gameObject.SetActive(yy != null);
        if (yy != null)
        {
            nextBtn.GetComponent<UnoOption>().fd = yy;
        }
        
        var ld = lastDialog.FindAll(x => x.phrase != "next");
        //after that we remove all inaccsessible by cost
        ld = ld.FindAll(x =>
        {
            if (x.cond == "x" || x.cond == string.Empty) return true;
            var a1 = ConfigLoader.Instance.allConditions[x.cond];
            var b1 = ModelStatistics.instance.CheckCondition(a1);
            return b1;
        });
        
        for (int i = 0; i < options.Count; i++) options[i].gameObject.SetActive(false);
        
        for (int i = 0; i < options.Count; i++)
        {
            if (i + 1 >= ld.Count) break;
            options[i].gameObject.SetActive(true);
            //if (!options[i].gameObject.activeSelf) continue;
            options[i].GetComponentInChildren<TextMeshProUGUI>().text = ld[i + 1].phrase;
            options[i].GetComponent<UnoOption>().fd = ld[i + 1];
            options[i].transform.Find("price").gameObject.SetActive(false);
            if (ld[i + 1].req_trigger != "x" && ld[i + 1].req_trigger.Length > 2)
            {
                Debug.Log("x " + ld[i + 1].req_trigger);
                var ds = ld[i + 1].req_trigger.Split(",");
                options[i].transform.Find("price").gameObject.SetActive(true);
                options[i].transform.Find("price").GetComponentInChildren<TextMeshProUGUI>().text = ds[1];
            }
        }
    }

    public void ClickOption(UnoOption uo)
    {
        Debug.Log(uo.fd.action);
        if (uo.fd.phrase == "next")
        {
            ShowDialogue(uo.fd.action);
        }
        else if (uo.fd.action == "end")
            {
                Hide();
            }
            else if (uo.fd.action == "cut")
            {
                Hide();
                Cutscener.instance.ExecuteCutscene(uo.fd.ava1);
            }
            else if (uo.fd.action == "buy")
            {
                Hide();
                //Show chests
                Debug.Log("here1");
                //thwbbb
                ModelStatistics.instance.SetStatValue("buy_mode", 1);
                MainStates.instance.UI_secondBuy.SetActive(true);
                MainStates.instance.UI_inventorySell.SetActive(true);
                
                //ShopKeeper.instance.Set(lastNpc.GetComponent<Inventary>(), MainStates.instance.inventory);
            }
            else if (uo.fd.action == "dyn")
            {
                Hide();
                
                ModelStatistics.instance.TakeDynamic(uo.fd.ava1);
                MainStates.instance.ExecuteDone(uo.fd.ava1);
            }
            else if (uo.fd.action == "hire")
            {
                var ds = uo.fd.req_trigger.Split(",");
                var mm = MainStates.instance.HaveAmount(new List<Bon>{ new Bon{Key = ds[0], Value = int.Parse(ds[1])} });
                if (mm)
                {
                    Hide();
                    //Show chests
                    Debug.Log("here2 hire");
                    EventManager.INV("hire_member",new ArgPass {go = lastNpc, what = uo.fd.ava1});
                    MainStates.instance.DelItems(new List<Bon>{ new Bon{Key = ds[0], Value = int.Parse(ds[1])} });

                    //MainStates.instance.inventory.AddItems(new List<Bon> { new Bon{Key = uo.fd.ava1} });
                    Destroy(lastNpc);
                }
            }
            else if (uo.fd.action == "enchant")
            {
                Hide();
                EventManager.INV("enchant_call",new ArgPass {go = lastNpc});
            }
            else
                ShowDialogue(uo.fd.action);

        if (uo.fd.cStart != "x" && uo.fd.cStart != string.Empty)
        {
            if (lastDynamic != null)
                lastDynamic.dialog =  uo.fd.cStart;
        }
        //}
    }
}
