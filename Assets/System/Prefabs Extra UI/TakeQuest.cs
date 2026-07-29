using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TakeQuest : MonoBehaviour
{
    private RObj mon;
    AbsHolder holder;
    private void Start()
    {
        holder = GetComponentInParent<AbsHolder>();

        var b = GetComponent<Button>();
        //b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() =>
            {
                var a = MainStates.instance.playerData.playerTasks.Find(x => x.id == holder.id);
                a.started = 1;
            }
        );
    }


}
