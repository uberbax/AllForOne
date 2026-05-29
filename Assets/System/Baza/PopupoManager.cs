using System;
using System.Collections.Generic;
using UnityEngine;

public class PopupoManager : MonoBehaviour
{
    public static PopupoManager instance;
    
    public GameObject battleWin;
    public GameObject battleLose;
    
    public UIfiller rewards;

    public StringObjectDictionary insiders;

    public GameObject tooltip;
    private void Awake()
    {
        instance = this;
        EventManager.SUB("battle_ended", ShowBattleResult);
    }

    public void ShowBattleResult(ArgPass argPass)
    {
        if (argPass.num == 0)
        {
            battleWin.SetActive(true);
            var a =battleWin.GetComponent<GBind>();
        }
        else
        {
            battleLose.SetActive(true);
        }
    }

    public void ShowRewards(List<Bon> rew)
    {
        rewards.selfReward = rew;
        rewards.gameObject.SetActive(true);
    }

    public void ShowRewardsInside(List<Bon> rew, string what)
    {
        var f0 = insiders[what].GetComponent<UIfiller>();
        f0.selfReward = rew;
        f0.OnEnable();

        if (insiders.ContainsKey(what + "1"))
        {
            f0 = insiders[what + "1"].GetComponent<UIfiller>();
            f0.selfReward = rew;
            f0.OnEnable();
        }
    }

    public void ShowTooltip(Transform b)
    {
        var ff = b.GetComponentInParent<ObjHolder>();
        if (ff == null || ff.obj == null)
        {
            MainStates.instance.lastTooltip = null;
            return;
        }
        MainStates.instance.lastTooltip = null;
        if (ff) MainStates.instance.lastTooltip = ff.obj;
        if (MainStates.instance.lastTooltip == null)
        {
            var c = b.GetComponentInParent<Buyable>().curDynamic;
            var itm = DatabaseAll.instance.CreateItem("empty", 1);
            itm.dynamic = c;
            MainStates.instance.lastTooltip = itm;
        }

        tooltip.SetActive(true);
        PlaceBottomLeftToSourceTopRight(b, tooltip.GetComponent<RectTransform>());
    }

    public void HideTooltip()
    {
        tooltip.SetActive(false);
    }

    private float dx = 20;
    public void PlaceBottomLeftToSourceTopRight(Transform sourceTransform, RectTransform target, int ind = 2)
    {
        if (sourceTransform == null || target == null)
            return;

        RectTransform source = sourceTransform.GetComponent<RectTransform>();
        if (source == null)
            return;

        RectTransform targetParent = target.parent as RectTransform;
        if (targetParent == null)
            return;

        Vector3[] sourceCorners = new Vector3[4];
        source.GetWorldCorners(sourceCorners);

        Canvas canvas0 = sourceTransform.GetComponentInParent<Canvas>();
        
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Vector3 sourceTopRightWorld = sourceCorners[ind];
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvas0 != null && canvas0.renderMode == RenderMode.WorldSpace)
            cam = Camera.main;
        
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, sourceTopRightWorld);
        cam = null;//huynya
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPoint, cam, out Vector2 localPoint))
        {
            Vector2 pivotOffset = new Vector2(
                target.rect.width * target.pivot.x,
                target.rect.height * target.pivot.y
            );

            target.localPosition = new Vector3(
                localPoint.x + (ind == 2 ? 1 : -1) * (pivotOffset.x + dx),
                localPoint.y + pivotOffset.y,
                target.localPosition.z
            );
        }
        
        Vector3[] endCorners = new Vector3[4];
        target.GetWorldCorners(endCorners);
        //Debug.Log("hh");
        //for (int i = 0; i < endCorners.Length; i++) Debug.Log(endCorners[i]);
        
        if (endCorners[1].y > Screen.height)
        {
            target.localPosition -= new Vector3(0, endCorners[1].y - Screen.height, 0);
        }
        
        if (endCorners[0].y < 0)
        {
            target.localPosition -= new Vector3(0, -endCorners[0].y, 0);
        }

        
        if (endCorners[3].x > Screen.width)
        {
            PlaceBottomLeftToSourceTopRight(sourceTransform, target, 1);
        }
        
        
    }

    public bool IsAnyActive()
    {
        return false;
    }
}
