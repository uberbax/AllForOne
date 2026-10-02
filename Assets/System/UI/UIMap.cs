using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class UIMap : MonoBehaviour
{
    public RectTransform mainPlayer;
    public RectTransform map;
    public RectTransform view;

    public static Vector2 delta = new Vector2(0.23f, 0.12f);
    
    public static UIMap instance;

    public Button travel;
    public GameObject lastPortal;

    private void Awake()
    {
        instance = this;
        travel.onClick.AddListener(RealTravel);
    }

    private void OnEnable()
    {
        Calculate();
        ToCenter();
    }

    public static void CenterMapOnPlayer(
        RectTransform map,
        RectTransform mainPlayer,
        RectTransform view)
    {
        RectTransform mapParent = map.parent as RectTransform;

        // Player position in Map's parent coordinate space
        Vector2 playerPos = mapParent.InverseTransformPoint(
            mainPlayer.TransformPoint(mainPlayer.rect.center)
        );

        // Center of View in the same coordinate space
        Vector2 viewCenter = mapParent.InverseTransformPoint(
            view.TransformPoint(view.rect.center)
        );

        // Move Map so player ends up exactly at View center
        Vector2 delta = viewCenter - playerPos;

        map.anchoredPosition += delta;
    }
    
    public void ToCenter()
    {
        travel.gameObject.SetActive(false);
        Calculate();
        CenterMapOnPlayer(map, mainPlayer, view);
    }

    public void Travel(GameObject go)
    {
        lastPortal = go;
        travel.gameObject.SetActive(true);
    }

    public void RealTravel()
    {
        var pp = MainStates.instance.mainPlayer.main.GetComponent<NavMeshAgent>();
        if (pp != null) pp.enabled = false;
        
        MainStates.instance.mainPlayer.main.transform.position = lastPortal.transform.position + new Vector3(0, -1, 0);
        MainStates.instance.mainPlayer.Position = lastPortal.transform.position + new Vector3(0, -1, 0);
        if (pp != null) pp.enabled = true;
        
        gameObject.SetActive(false);
    }

    public void Calculate()
    {
        float lenx = PositionSetter.instance.high.position.x - PositionSetter.instance.lo.position.x;
        float leny = PositionSetter.instance.high.position.y - PositionSetter.instance.lo.position.y;

        var dx = MainStates.instance.mainPlayer.Position.x/ lenx;
        var dy = MainStates.instance.mainPlayer.Position.y / leny;
        
        mainPlayer.anchorMin = new Vector2(dx, dy) + delta;
        mainPlayer.anchorMax = new Vector2(dx, dy) + delta;        
    }
    
    public Vector2 GetCalc(GameObject go)
    {
        float lenx = PositionSetter.instance.high.position.x - PositionSetter.instance.lo.position.x;
        float leny = PositionSetter.instance.high.position.y - PositionSetter.instance.lo.position.y;

        var dx = go.transform.position.x / lenx;
        var dy = go.transform.position.y / leny;
        
        return (new Vector2(dx, dy) + delta);
    }
    
    void Update()
    {
        Calculate();
    }
}
