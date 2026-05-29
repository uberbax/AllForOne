using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnityGameScreen : MonoBehaviour
{
    public UnityCharacter player;
    public CanvasGroup gameover;
    public Image deathByDarkness;
    public List<UnityCampfire> campfires = new List<UnityCampfire>();

    public List<GameObject> enableOnStart;

    public CanvasGroup finalScreen;
    
    public int debugStage;

    public static bool isgamewin;
    
    static bool startedAtLeastOnce;
    
    void Start()
    {
        foreach(var e in enableOnStart)
            e.SetActive(true);
        
        #if UNITY_EDITOR
        if (debugStage >= 0 && !startedAtLeastOnce)
        {
            startedAtLeastOnce = true;
            UnityShell.main.data.campfire = debugStage;
        }
        #endif
        
        for (var index = 0; index < campfires.Count; index++)
        {
            var cf = campfires[index];
            cf.index = index;
            if (cf.nextStage != null)
                cf.nextStage.SetActive(false);
        }

        var dataCampfire = UnityShell.main.data.campfire;

        if (dataCampfire > 0)
            campfires[dataCampfire - 1].Light();

        if (dataCampfire > 1)
            campfires[dataCampfire - 2].Light();
        
        var unityCampfire = campfires[dataCampfire];
        unityCampfire.Light(!UnityShell.main.data.isDefended);
        
        player.transform.position = unityCampfire.playerTarget.position;

        if (dataCampfire > 0)
        {
            if (player.shotArrow != null)
                player.shotArrow.Pickup(quiet: true);
        }
    }

    void Update()
    {
        if (isgamewin)
        {
            if (finalScreen.alpha < 1)
                finalScreen.alpha += Time.deltaTime;
        }
        
        if (player.isDead)
        {
            if (gameover.alpha == 0)
            {
                // show gameover lol
                gameover.alpha = 1f;
                UnityUIAnnouncement.i.Hide();
            }
        }

        deathByDarkness.color = new Color(0f, 0f, 0f, player.GetDeathByDarknessK());
    }
}