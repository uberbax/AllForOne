using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonUI : MonoBehaviour, IReceive
{
    private List<string> dungeonDescriptions = new List<string>
    {
        "Once a prosperous crystal mine, its tunnels now glow with an unnatural golden light. Abandoned carts still overflow with ore, but something beneath the mountain has awakened—and it does not appreciate intruders.",
        "Moss-covered stones conceal the entrance to a forgotten underground sanctuary. Green flames have burned here for centuries, guiding treasure hunters deeper into halls where the dead refuse to remain silent.",
        "The old miners followed a rich vein far deeper than they were supposed to. Their lanterns are still burning, the rails are still intact, but no miner has returned from the lower shafts.",
        "A brutal stronghold built to guard the road into the underworld. Beyond its iron gate lie barracks, torture chambers, and halls occupied by creatures that now serve a new master.",
        "Beneath the frozen cliffs lies a maze of blue ice and luminous crystals. The deeper chambers have remained sealed for generations, preserving ancient beasts—and the treasures of those who tried to hunt them."
    };

    private List<string> dungeonNames = new List<string>
    {
        "Emberstone Mine",
        "Ruins of the Hollow King",
        "Deepdelve Mine",
        "Dreadfang Fortress",
        "Frostcrystal Cavern"
    };

    public List<int> difficulties = new List<int> { 1, 2, 3, 4, 5 };

    [Header("Dungeon Fill")] public Transform starHolder;
    public Image iconDungeon;
    public TextMeshProUGUI dungeonName;
    public TextMeshProUGUI dungeonDescr;
    public Button enterDungeon;

    private int curNum = 0;
    public void Receive(ArgPass arg)
        
    {
        curNum = arg.num;
        iconDungeon.sprite = ResourceHolder.instance.buildingIcons[curNum];
        dungeonName.text = dungeonNames[curNum];
        dungeonDescr.text = dungeonDescriptions[curNum];
        starHolder.GetChild(0).GetComponent<TextMeshProUGUI>().text = difficulties[curNum].ToString();
        enterDungeon.onClick.RemoveAllListeners();
        enterDungeon.onClick.AddListener(() =>
            {
                EventManager.INV("start_dungeon", new ArgPass{num = curNum});
                gameObject.SetActive(false);
            }
        );
        //время до повторного захода
        
        
    }
}