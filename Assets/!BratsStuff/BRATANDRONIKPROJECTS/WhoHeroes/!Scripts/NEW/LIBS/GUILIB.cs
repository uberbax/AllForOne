using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class WhoHeroesEvents
{
    public const string Refresh = "whoheroes_refresh";
    public const string ViewBuilding = "whoheroes_view_building";
    public const string ObserveBuilding = "whoheroes_observe_building";
    public const string UnitInfo = "whoheroes_unit_info";
    public const string Dialogue = "whoheroes_dialogue";
    public const string ResetRequested = "whoheroes_reset_requested";
}

[Serializable]
public class WhoHeroesObjectRef
{
    public string id = "";
    public int level = 1;
    public string itembaseid = "";
    public bool own = true;
}

[Serializable]
public class WhoHeroesNamedObject
{
    public string id = "";
    public GameObject obj;
}

[Serializable]
public class WhoHeroesStatValue
{
    public string id = "";
    public float value;
    public string format = "int";
}

[Serializable]
public class WhoHeroesStatList
{
    public List<WhoHeroesStatValue> items = new List<WhoHeroesStatValue>();

    public WhoHeroesStatList Scaled(float multiplier)
    {
        var result = new WhoHeroesStatList();
        foreach (var item in items)
        {
            result.items.Add(new WhoHeroesStatValue
            {
                id = item.id,
                value = item.value * multiplier,
                format = item.format
            });
        }
        return result;
    }
}

public class GUILIB : MonoBehaviour
{
    public static GUILIB Instance;

    private GameObject actionContext;
    private ObjHolder actionHolder;
    private UnoAll action;

    private void Awake()
    {
        Instance = this;
        actionContext = new GameObject("WhoHeroes Minimus UI Context");
        actionContext.transform.SetParent(transform, false);
        actionContext.SetActive(false);
        actionHolder = actionContext.AddComponent<ObjHolder>();
        action = actionContext.AddComponent<UnoAll>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool CoreReady => ConfigLoader.parseEnded && MainStates.instance != null && DatabaseAll.instance != null;

    public static RObj Resolve(WhoHeroesObjectRef objectRef, GameObject context = null, bool create = false)
    {
        return Resolve(objectRef == null ? "" : objectRef.id, context, create,
            objectRef == null ? 1 : Mathf.Max(1, objectRef.level));
    }

    public static RObj Resolve(string id, GameObject context = null, bool create = false, int level = 1)
    {
        var holder = context == null ? null : context.GetComponentInParent<ObjHolder>(true);
        if (holder != null && holder.obj != null &&
            (string.IsNullOrEmpty(id) || IsId(holder.obj, id)))
            return holder.obj;

        if (MainStates.instance == null)
            return null;

        if (!string.IsNullOrEmpty(id) && MainStates.instance.all.TryGetValue(id, out var exact))
            return exact;

        var player = MainStates.instance.all.TryGetValue("main_player", out var mainPlayer) ? mainPlayer : null;
        var fromInventory = player?.inventory.Find(x => IsId(x, id));
        if (fromInventory != null)
            return fromInventory;

        if (!create || !CoreReady || string.IsNullOrEmpty(id) || context == null)
            return null;

        RObj result;
        var known = DatabaseAll.instance.heroes.ContainsKey(id) || DatabaseAll.instance.items.ContainsKey(id) ||
                    DatabaseAll.instance.buildings.ContainsKey(id) || DatabaseAll.instance.skills.ContainsKey(id);

        if (known)
        {
            result = DatabaseAll.instance.CreateAny(id, false, 1, context, id, null, false, false, level);
        }
        else
        {
            result = new RObj(id, ItemType.unknown)
            {
                RID = id,
                main = context,
                Position = context.transform.position
            };
            result.upgradePars["amount"] = 1;
            result.upgradePars["level"] = Mathf.Max(0, level - 1);
            result.upgradePars["registered_damage"] = 0;
            result.upgradePars["registered_mana"] = 0;
            result.upgradePars["used_slot"] = -1;
            result.upgradePars["exp"] = 0;
            result.RecalcPars();
            MainStates.instance.all.Add(result.RID, result);
        }

        holder = context.GetComponent<ObjHolder>();
        if (holder == null)
            holder = context.AddComponent<ObjHolder>();
        holder.obj = result;
        result.main = context;
        result.Position = context.transform.position;
        return result;
    }

    public static bool IsId(RObj value, string id)
    {
        if (value == null || string.IsNullOrEmpty(id))
            return false;
        return value.RID == id || (value.dbObj != null && value.dbObj.ID == id);
    }

    public static string Id(RObj value, string fallback = "")
    {
        if (value == null)
            return fallback;
        return value.dbObj == null ? value.RID : value.dbObj.ID;
    }

    public static int Level(RObj value, int fallback = 0)
    {
        return value == null ? fallback : Mathf.RoundToInt(value.GetPar("level"));
    }

    public static float Param(RObj value, string key, float fallback = 0)
    {
        if (value == null || string.IsNullOrEmpty(key))
            return fallback;
        return value.GetPar(key);
    }

    public static string StringParam(RObj value, string key, string fallback = "")
    {
        if (value?.dbObj == null || !value.dbObj.parsStr.TryGetValue(key, out var result))
            return fallback;
        return result;
    }

    public static Sprite Icon(RObj value)
    {
        if (value == null || ResourceHolder.instance == null)
            return null;

        var id = Id(value);
        if (value.it == ItemType.item && ResourceHolder.instance.items.ContainsKey(id))
            return ResourceHolder.instance.items[id];
        if (value.it == ItemType.monster && ResourceHolder.instance.avas.ContainsKey(id))
            return ResourceHolder.instance.avas[id];
        if (value.it == ItemType.projectile && ResourceHolder.instance.skills.ContainsKey(id))
            return ResourceHolder.instance.skills[id];
        if (value.it == ItemType.building && ResourceHolder.instance.buildings.ContainsKey(id))
            return ResourceHolder.instance.buildings[id];
        if (value.it == ItemType.task && ResourceHolder.instance.tasks.ContainsKey(value.RID))
            return ResourceHolder.instance.tasks[value.RID];
        return Icon(id);
    }

    public static Sprite Icon(string id)
    {
        if (ResourceHolder.instance == null || string.IsNullOrEmpty(id))
            return null;
        if (ResourceHolder.instance.buildings.ContainsKey(id))
            return ResourceHolder.instance.buildings[id];
        if (ResourceHolder.instance.items.ContainsKey(id))
            return ResourceHolder.instance.items[id];
        if (ResourceHolder.instance.avas.ContainsKey(id))
            return ResourceHolder.instance.avas[id];
        if (ResourceHolder.instance.skills.ContainsKey(id))
            return ResourceHolder.instance.skills[id];
        if (ResourceHolder.instance.misc.ContainsKey(id))
            return ResourceHolder.instance.misc[id];
        return null;
    }

    public static Color ColorFor(string id)
    {
        if (ResourceHolder.instance != null && ResourceHolder.instance.elemColors.ContainsKey(id))
            return ResourceHolder.instance.elemColors[id];

        return id switch
        {
            "butred" or "textred" => new Color(0.8f, 0.25f, 0.2f),
            "butgreen" => new Color(0.25f, 0.65f, 0.3f),
            "butgrey" or "textgrey" or "disabledtab" => new Color(0.45f, 0.45f, 0.45f),
            "activetab" => Color.white,
            "inactivetab" => new Color(0.7f, 0.7f, 0.7f),
            _ => Color.white
        };
    }

    public static List<RObj> PlayerInventory()
    {
        if (MainStates.instance == null || !MainStates.instance.all.TryGetValue("main_player", out var player))
            return new List<RObj>();
        return player.inventory;
    }

    public static List<Bon> AsResources(IEnumerable<RObj> values)
    {
        return values.Where(x => x != null && x.dbObj != null)
            .GroupBy(x => x.dbObj.ID)
            .Select(x => new Bon { Key = x.Key, Value = Mathf.RoundToInt(x.Sum(v => v.GetPar("amount"))) })
            .ToList();
    }

    public static List<Bon> Price(RObj value, string actionName = "upgrade")
    {
        if (value == null)
            return new List<Bon>();
        if (UpgradeSystem.instance != null)
            return UpgradeSystem.instance.GetPrice(value, actionName);
        return value.dbObj?.price ?? new List<Bon>();
    }

    public static bool CanAfford(List<Bon> price)
    {
        return MainStates.instance != null && MainStates.instance.HaveAmount(price ?? new List<Bon>());
    }

    public static void CoreAction(RObj value, string actionName, string actionParam2 = "")
    {
        if (value == null || MainStates.instance == null || Instance == null)
            return;

        Instance.actionHolder.obj = value;
        Instance.action.mon = value;
        Instance.action.param = actionName;
        Instance.action.param2 = actionParam2;
        MainStates.instance.ClickedSome(value, Instance.action, Instance.actionHolder, true);
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = value, what = actionName });
    }

    public static void Emit(string eventName, RObj value = null, string what = "", int number = 0)
    {
        EventManager.INV(eventName, new ArgPass { who = value, what = what, num = number });
    }

    public string TimeFormat(float time)
    {
        var seconds = Mathf.Max(0, Mathf.FloorToInt(time));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    public string FillNum(float number, string format = "int", string pref = "", string postf = "")
    {
        var value = format switch
        {
            "int" => ShortNumber(Mathf.RoundToInt(number)),
            "persent" => Mathf.RoundToInt(100 * number) + "%",
            "time" => TimeFormat(number),
            "cutfloat" => number.ToString("0.00"),
            _ => number.ToString()
        };
        return pref + value + postf;
    }

    public void ForseTranslate(TextMeshProUGUI text, string id, string language)
    {
        Translate(text, id);
    }

    public void Translate(TextMeshProUGUI text, string id)
    {
        if (text == null)
            return;
        text.text = ConfigLoader.Instance == null ? id : ConfigLoader.Instance.GetMeLocale(id);
    }

    public void ActDisObjects(List<bool> states, List<GameObject> objects)
    {
        var count = Mathf.Min(states.Count, objects.Count);
        for (var i = 0; i < count; i++)
            if (objects[i] != null)
                objects[i].SetActive(states[i]);
    }

    public void FillGUIList<T, M>(GameObject prefab, Transform holder, List<T> list) where M : Component, CanFill<T>
    {
        for (var i = holder.childCount - 1; i >= 0; i--)
            Destroy(holder.GetChild(i).gameObject);
        foreach (var value in list)
            Instantiate(prefab, holder).GetComponent<M>().Fill(value);
    }

    public void PlaceBottomLeftToSourceTopRight(Transform sourceTransform, RectTransform target, int corner = 2)
    {
        if (sourceTransform == null || target == null || sourceTransform is not RectTransform source ||
            target.parent is not RectTransform targetParent)
            return;

        var corners = new Vector3[4];
        source.GetWorldCorners(corners);
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, corners[Mathf.Clamp(corner, 0, 3)]);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPoint, camera, out var localPoint))
            target.localPosition = new Vector3(localPoint.x, localPoint.y, target.localPosition.z);
    }

    public Coroutine BarInfinite(Slider bar, float speed)
    {
        return StartCoroutine(BarInfiniteCor(bar, speed));
    }

    private IEnumerator BarInfiniteCor(Slider bar, float speed)
    {
        if (bar == null)
            yield break;
        bar.value = 0;
        while (bar.value < 0.999f)
        {
            if (MainStates.instance == null || !MainStates.instance.isPaused)
                bar.value += speed * Time.deltaTime;
            yield return null;
        }
        bar.value = 1;
    }

    public Coroutine TextPrintContinue(TextMeshProUGUI text, string target, float delay, GameObject next = null)
    {
        return StartCoroutine(TextPrintContinueCor(text, target, delay, next));
    }

    private IEnumerator TextPrintContinueCor(TextMeshProUGUI text, string target, float delay, GameObject next)
    {
        text.text = "";
        foreach (var character in target)
        {
            text.text += character;
            yield return new WaitForSeconds(delay);
        }
        if (next != null)
            next.SetActive(true);
    }

    public Coroutine IntInfiniteAdd(TextMeshProUGUI text, float delay, int target, int start = 0)
    {
        return StartCoroutine(IntInfiniteAddCor(text, delay, target, start));
    }

    private IEnumerator IntInfiniteAddCor(TextMeshProUGUI text, float delay, int target, int start)
    {
        for (var value = start; value <= target; value++)
        {
            text.text = value.ToString();
            yield return new WaitForSeconds(delay);
        }
    }

    public string ShortNumber(int number)
    {
        if (Mathf.Abs(number) < 1000)
            return number.ToString();
        if (Mathf.Abs(number) < 1000000)
            return (number / 1000f).ToString("0.#") + "K";
        return (number / 1000000f).ToString("0.#") + "M";
    }

    public void FastAppear(CanvasGroup group, bool active, GameObject target = null)
    {
        if (target != null)
            target.SetActive(active);
        if (group != null)
            group.alpha = active ? 1 : 0;
    }

    public Coroutine SlowAppear(GameObject target, CanvasGroup group, bool active, float speed,
        float delay = 0, float idle = 0, bool revert = false, string sound = "")
    {
        return StartCoroutine(Slow(target, group, active, speed, delay, idle, revert, sound));
    }

    private IEnumerator Slow(GameObject target, CanvasGroup group, bool active, float speed, float delay,
        float idle, bool revert, string sound)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            target.SetActive(true);
        if (!string.IsNullOrEmpty(sound) && active && SoundManager.instance != null)
            SoundManager.instance.PlayAny(sound);
        if (group == null)
            yield break;

        group.alpha = active ? 0 : 1;
        while (active ? group.alpha < 1 : group.alpha > 0)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, active ? 1 : 0, speed * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(idle);
        if (target != null)
            target.SetActive(active);
        if (revert && active)
            yield return Slow(target, group, false, speed, 0, 0, false, "");
    }
}

public interface CanFill<T>
{
    void Fill(T value);
}

[Serializable]
public class PopUp
{
    public string id = "";
    public GameObject obj;
    public bool status;
    public Button but;
    public GameObject[] choosen;
    public Button close;
    public GameObject disabled;
    public bool available = true;
    public bool pause;
    public bool keepDefault;
    public TextMeshProUGUI colortxt;
    public Action<string> OnChoosen;

    public void ManageAvailable(bool state)
    {
        if (disabled != null)
            disabled.SetActive(!state);
        available = state;
        if (but != null)
            but.interactable = state;
        if (colortxt != null && !state)
            colortxt.color = GUILIB.ColorFor("disabledtab");
    }

    public void Active()
    {
        if (!available)
            return;
        status = true;
        if (obj != null)
            obj.SetActive(true);
        if (choosen != null)
            foreach (var value in choosen)
                if (value != null) value.SetActive(true);
        OnChoosen?.Invoke(id);
        if (colortxt != null)
            colortxt.color = GUILIB.ColorFor("activetab");
    }

    public void Inactive()
    {
        status = false;
        if (obj != null)
            obj.SetActive(false);
        if (choosen != null)
            foreach (var value in choosen)
                if (value != null) value.SetActive(false);
        if (colortxt != null)
            colortxt.color = GUILIB.ColorFor("inactivetab");
    }
}

[Serializable]
public class PopUpList
{
    public List<PopUp> items = new List<PopUp>();
    public string choosen = "";
    public string defaulttab = "";
    public int Count => items.Count;
    public PopUp this[string id] => items.Find(x => x.id == id);

    public void SetUpNavigation()
    {
        foreach (var popup in items)
        {
            var current = popup;
            current.but?.onClick.AddListener(() => SwitchTab(current.id));
            current.close?.onClick.AddListener(() => ToDefault());
        }
    }

    public void SwitchTab(string id)
    {
        choosen = id;
        foreach (var popup in items)
        {
            if (popup.id == id)
                popup.Active();
            else
                popup.Inactive();
        }
        var selected = this[id];
        if (selected != null && selected.keepDefault && !string.IsNullOrEmpty(defaulttab))
            this[defaulttab]?.obj?.SetActive(true);
        if (MainStates.instance != null && selected != null && selected.pause)
            MainStates.instance.isPaused = true;
    }

    public void ToDefault(bool pause = false)
    {
        if (!pause && MainStates.instance != null)
            MainStates.instance.isPaused = false;
        choosen = defaulttab;
        foreach (var popup in items)
        {
            if (popup.id == choosen && !string.IsNullOrEmpty(choosen))
                popup.Active();
            else
                popup.Inactive();
        }
    }

    public void ChangeStatus(string id, bool state = false)
    {
        var popup = this[id];
        if (popup != null)
            popup.status = state;
    }
}

[Serializable]
public class ControllerManager
{
    public List<GameObject> onMouse;
    public List<GameObject> onJoystick;
    public bool joystickControl;
    public bool singleControl = true;

    public void ManageControl(bool joystick)
    {
        if (onMouse == null || onJoystick == null)
            return;
        joystickControl = joystick;
        foreach (var value in onMouse)
            value.SetActive(!singleControl || !joystickControl);
        foreach (var value in onJoystick)
            value.SetActive(joystickControl);
    }
}

[Serializable]
public class SpriteItem
{
    public string id = "";
    public Sprite icon;
}

[Serializable]
public class SpriteList
{
    public List<SpriteItem> icons = new List<SpriteItem>();
    public Sprite this[string id] => icons.Find(x => x.id == id)?.icon;
}

[Serializable]
public class RarityItem
{
    public string id = "";
    public Color color;
}

[Serializable]
public class RarityItemList
{
    public List<RarityItem> items = new List<RarityItem>();
}

[Serializable]
public class GUIResources
{
    [NonSerialized] private List<Bon> resources = new List<Bon>();
    public bool fillZero = true;
    public List<TextMeshProUGUI> values = new List<TextMeshProUGUI>();
    public List<GameObject> objs = new List<GameObject>();

    public void Fill(List<Bon> source = null, string color = "")
    {
        if (source != null)
            resources = source.Select(x => new Bon { Key = x.Key, Value = x.Value, Val2 = x.Val2, Val3 = x.Val3 }).ToList();

        var count = Mathf.Min(objs.Count, values.Count);
        for (var i = 0; i < count; i++)
        {
            var visible = i < resources.Count && (fillZero || resources[i].Value != 0);
            objs[i].SetActive(visible);
            if (!visible)
                continue;
            values[i].text = GUILIB.Instance.FillNum(resources[i].Value, "int");
            if (!string.IsNullOrEmpty(color))
                values[i].color = GUILIB.ColorFor(color);
        }
    }
}

[Serializable]
public class GUIStats
{
    public WhoHeroesStatList stats = new WhoHeroesStatList();
    public bool fillZero = true;
    public List<TextMeshProUGUI> values = new List<TextMeshProUGUI>();
    public List<GameObject> objs = new List<GameObject>();

    public void Fill(WhoHeroesStatList source = null, string color = "")
    {
        if (source != null)
            stats = source;
        var count = Mathf.Min(stats.items.Count, Mathf.Min(values.Count, objs.Count));
        for (var i = 0; i < count; i++)
        {
            var stat = stats.items[i];
            var visible = fillZero || !Mathf.Approximately(stat.value, 0);
            objs[i].SetActive(visible);
            if (!visible)
                continue;
            values[i].text = GUILIB.Instance.FillNum(stat.value, stat.format);
            if (!string.IsNullOrEmpty(color))
                values[i].color = GUILIB.ColorFor(color);
        }
    }

    public void Fill(RObj value, string color = "")
    {
        var current = new WhoHeroesStatList();
        foreach (var stat in stats.items)
            current.items.Add(new WhoHeroesStatValue { id = stat.id, value = GUILIB.Param(value, stat.id), format = stat.format });
        Fill(current, color);
    }
}

[Serializable]
public class GUIInfoItem
{
    public Image icon;
    public TextMeshProUGUI name;
    public TextMeshProUGUI description;
    public TextMeshProUGUI number;

    public void Fill(string id, int level, Sprite sprite = null, string descriptionId = "", string numberStat = "")
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
        if (name != null)
            GUILIB.Instance.Translate(name, id);
        if (description != null)
        {
            GUILIB.Instance.Translate(description, string.IsNullOrEmpty(descriptionId) ? id + "_descr" : descriptionId);
            description.text = description.text.Replace("%%n", numberStat);
        }
        if (number != null)
            number.text = level.ToString();
    }

    public void Fill(RObj value, string descriptionId = "")
    {
        Fill(GUILIB.Id(value), GUILIB.Level(value), GUILIB.Icon(value), descriptionId);
    }
}

[Serializable]
public class GUICostButtonItem
{
    public GUIResources cost;
    public Image buttonObj;
    public Button buy;
    public GameObject costList;
    public GameObject restriction;
    public GameObject max;
    public TextMeshProUGUI header;

    public void Fill(List<Bon> price, bool maxLevel = false, bool block = false, bool showRestriction = true,
        string headerText = "upgrade", string activeButtonColor = "butgreen", string disabledButtonColor = "butgrey",
        string activeTextColor = "textwhite", string disabledTextColor = "textred",
        string activeHeaderColor = "textwhite", string disabledHeaderColor = "textgrey")
    {
        if (header != null)
            GUILIB.Instance.Translate(header, headerText);
        var affordable = GUILIB.CanAfford(price);
        max?.SetActive(maxLevel);
        restriction?.SetActive(block && !maxLevel && showRestriction);
        costList?.SetActive(!maxLevel);
        var active = affordable && !maxLevel && !block;
        if (buy != null)
            buy.interactable = active;
        if (header != null)
            header.color = GUILIB.ColorFor(active ? activeHeaderColor : disabledHeaderColor);
        if (!maxLevel)
            cost?.Fill(price, affordable ? activeTextColor : disabledTextColor);
        if (buttonObj != null)
            buttonObj.color = GUILIB.ColorFor(active ? activeButtonColor : disabledButtonColor);
    }
}

[Serializable]
public class GUIStatsGrades
{
    public List<TextMeshProUGUI> levels = new List<TextMeshProUGUI>();
    public List<GUIStats> grades = new List<GUIStats>();

    public void Fill(int current, WhoHeroesStatList basic, float multiplier)
    {
        for (var i = 0; i < levels.Count; i++)
            levels[i].text = (current + i).ToString();
        for (var i = 0; i < grades.Count; i++)
            grades[i].Fill(basic.Scaled((current + i) * multiplier));
    }

    public void Fill(int current, RObj value, float multiplier)
    {
        var basic = new WhoHeroesStatList();
        if (grades.Count > 0)
            foreach (var stat in grades[0].stats.items)
                basic.items.Add(new WhoHeroesStatValue { id = stat.id, value = GUILIB.Param(value, stat.id), format = stat.format });
        Fill(current, basic, multiplier);
    }
}

[Serializable]
public class GUIValueGrades
{
    public List<TextMeshProUGUI> levels = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> grades = new List<TextMeshProUGUI>();
    public List<Image> icons = new List<Image>();

    public void Fill(int current, float basic, float multiplier, string format = "int", string pref = "", string postf = "", string icon = "")
    {
        for (var i = 0; i < levels.Count; i++)
            levels[i].text = (current + i).ToString();
        for (var i = 0; i < grades.Count; i++)
            grades[i].text = GUILIB.Instance.FillNum((current + i) * multiplier * basic, format, pref, postf);
        var sprite = GUILIB.Icon(icon);
        if (sprite != null)
            foreach (var image in icons)
                image.sprite = sprite;
    }
}

[Serializable]
public class GUIUnitFrame
{
    public string id = "";
    public Image ava;
    [NonSerialized] public RObj g;
    public Image rarity;
    public TextMeshProUGUI number;
    public Button infoBut;
    public Button actionBut;
    public TextMeshProUGUI buttonText;
    public Image buttonImage;

    public void Fill(RObj value = null)
    {
        if (value != null)
            g = value;
        if (g == null)
            return;
        var sprite = GUILIB.Icon(g);
        if (ava != null && sprite != null)
            ava.sprite = sprite;
        if (rarity != null && ResourceHolder.instance != null)
        {
            var rarityIndex = Mathf.RoundToInt(g.GetPar("rarity"));
            if (ResourceHolder.instance.rareColors.ContainsKey(rarityIndex))
                rarity.color = ResourceHolder.instance.rareColors[rarityIndex];
        }
        if (number != null)
            number.text = Mathf.RoundToInt(g.GetPar("amount")).ToString();
    }

    public void SetUpActions(bool hasInfo = true, bool hasAction = true, string actionType = "", string color = "")
    {
        infoBut?.gameObject.SetActive(hasInfo);
        actionBut?.gameObject.SetActive(hasAction);
        if (hasInfo && infoBut != null)
            infoBut.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.UnitInfo, g));
        if (hasAction && actionBut != null)
        {
            if (buttonText != null && !string.IsNullOrEmpty(actionType))
                GUILIB.Instance.Translate(buttonText, actionType);
            if (buttonImage != null && !string.IsNullOrEmpty(color))
                buttonImage.color = GUILIB.ColorFor(color);
            actionBut.onClick.AddListener(() => ExecuteAction(actionType));
        }
    }

    private void ExecuteAction(string actionType)
    {
        var coreAction = actionType switch
        {
            "hire" => "buy",
            "addexp" or "addtower" or "add" => "equip_exp",
            "removeexp" or "removetower" or "remove" => "unequip_exp",
            _ => actionType
        };
        GUILIB.CoreAction(g, coreAction);
    }

    public void ChangeAction(bool state, string color = "butgreen", string disabledColor = "butgrey")
    {
        if (actionBut != null)
            actionBut.interactable = state;
        if (buttonImage != null)
            buttonImage.color = GUILIB.ColorFor(state ? color : disabledColor);
    }
}

[Serializable]
public class GUIUnitShort
{
    public string id = "";
    [NonSerialized] public RObj unit;
    public GUIInfoItem general;
    public Image rarity;
    public TextMeshProUGUI number;

    public virtual void Fill(RObj value = null)
    {
        if (value != null)
            unit = value;
        if (unit == null)
            return;
        general?.Fill(unit, GUILIB.StringParam(unit, "skill"));
        if (number != null)
            number.text = Mathf.RoundToInt(unit.GetPar("amount")).ToString();
        if (rarity != null && ResourceHolder.instance != null)
        {
            var rarityIndex = Mathf.RoundToInt(unit.GetPar("rarity"));
            if (ResourceHolder.instance.rareColors.ContainsKey(rarityIndex))
                rarity.color = ResourceHolder.instance.rareColors[rarityIndex];
        }
    }
}

[Serializable]
public class ButtonActionItem
{
    public Button but;
    public TextMeshProUGUI buttonText;
    public Image buttonImage;

    public void Fill(bool active = true, string header = "add", string color = "butgreen")
    {
        if (but != null)
            but.interactable = active;
        if (buttonImage != null)
            buttonImage.color = GUILIB.ColorFor(color);
        if (buttonText != null && !string.IsNullOrEmpty(header))
            GUILIB.Instance.Translate(buttonText, header);
    }
}

[Serializable]
public class GUIUnit : GUIUnitShort
{
    public GUIStats stats;
    public GUIButtUpgrade hire;
    public TextMeshProUGUI rarityText;

    public override void Fill(RObj value = null)
    {
        base.Fill(value);
        if (unit == null)
            return;
        stats?.Fill(unit);
        hire?.Fill(GUILIB.Price(unit, "buy"), false, unit.GetPar("amount") <= 0, false, "hire");
        if (rarityText != null)
        {
            var rarityIndex = Mathf.RoundToInt(unit.GetPar("rarity"));
            rarityText.text = ResourceHolder.instance != null && ResourceHolder.instance.rareString.ContainsKey(rarityIndex)
                ? ResourceHolder.instance.rareString[rarityIndex]
                : rarityIndex.ToString();
            if (ResourceHolder.instance != null && ResourceHolder.instance.rareColors.ContainsKey(rarityIndex))
                rarityText.color = ResourceHolder.instance.rareColors[rarityIndex];
        }
    }
}

[Serializable]
public class GUIBuilding
{
    public string id = "";
    [NonSerialized] public RObj building;
    public GUIInfoItem general;
    public GUIResources cost;

    public virtual void Fill(RObj value = null)
    {
        if (value != null)
            building = value;
        if (building == null)
            return;
        general?.Fill(building);
        cost?.Fill(GUILIB.Price(building));
    }
}

[Serializable]
public class GUITask : CanFill<RObj>
{
    public string id = "";
    public Transform transform;
    [NonSerialized] public RObj task;
    public TextMeshProUGUI description;
    public GUIResources reward;
    public Button claimButt;
    public GameObject complete;

    public void Fill(RObj value)
    {
        task = value;
        if (task == null || DatabaseAll.instance == null || !DatabaseAll.instance.allTasks.ContainsKey(task.RID))
            return;

        var config = DatabaseAll.instance.allTasks[task.RID];
        var progress = MainStates.instance.playerData.playerTasks.Find(x => x.id == task.RID);
        if (description != null)
            description.text = ConfigLoader.Instance.GetMeLocale(config.description);
        reward?.Fill(config.rewards);

        var claimable = progress != null && progress.completed && !progress.taken;
        var taken = progress != null && progress.taken;
        if (claimButt != null)
        {
            claimButt.onClick.RemoveAllListeners();
            claimButt.interactable = claimable;
            claimButt.GetComponent<Image>().color = GUILIB.ColorFor(claimable ? "butgreen" : "butgrey");
            if (claimable)
                claimButt.onClick.AddListener(() =>
                {
                    ModelStatistics.instance.TakeTaskReward(config);
                    EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = task });
                });
        }
        complete?.SetActive(taken);
        Order();
    }

    public void Order()
    {
        if (task == null || transform == null || MainStates.instance == null)
            return;
        var progress = MainStates.instance.playerData.playerTasks.Find(x => x.id == task.RID);
        if (progress != null && progress.completed && !progress.taken)
            transform.SetAsFirstSibling();
        else if (progress != null && progress.taken)
            transform.SetAsLastSibling();
    }
}

[Serializable]
public class GUITaskList
{
    public GameObject taskPrefab;
    public Transform taskHolder;
    public Transform transformMove;
    public Transform showTarget;
    public Transform hideTarget;

    public void Fill(List<RObj> tasks)
    {
        for (var i = taskHolder.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(taskHolder.GetChild(i).gameObject);
        foreach (var task in tasks)
            UnityEngine.Object.Instantiate(taskPrefab, taskHolder).GetComponent<GUITaskPrefab>()?.taskgui.Fill(task);
        Order();
    }

    public void Order()
    {
        for (var i = 0; i < taskHolder.childCount; i++)
            taskHolder.GetChild(i).GetComponent<GUITaskPrefab>()?.taskgui.Order();
    }
}

[Serializable]
public class GUISettings
{
    public Slider music;
    public Slider effects;
    public Button apply;
    public PopUpList languageToggles;

    public void SetUp()
    {
        languageToggles?.SetUpNavigation();
        languageToggles?.ToDefault();
        music?.onValueChanged.AddListener(value => SetSetting("volume_music", value));
        effects?.onValueChanged.AddListener(value => SetSetting("volume_sound", value));
        apply?.onClick.AddListener(() =>
            EventManager.INV("language_changed", new ArgPass { what = languageToggles?.choosen ?? "" }));
    }

    private static void SetSetting(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        if (MainStates.instance != null && MainStates.instance.all.TryGetValue("settings", out var settings))
            settings.SetPar(key, value);
    }

    public void Fill()
    {
        var settings = MainStates.instance != null && MainStates.instance.all.TryGetValue("settings", out var value)
            ? value
            : null;
        if (music != null)
            music.SetValueWithoutNotify(settings == null ? PlayerPrefs.GetFloat("volume_music", 1) : settings.GetPar("volume_music"));
        if (effects != null)
            effects.SetValueWithoutNotify(settings == null ? PlayerPrefs.GetFloat("volume_sound", 1) : settings.GetPar("volume_sound"));
    }
}

[Serializable]
public class GUIActivationGroup
{
    public List<GameObject> onTrue = new List<GameObject>();
    public List<GameObject> onFalse = new List<GameObject>();

    public void Activate(bool state = true)
    {
        foreach (var value in onTrue)
            value.SetActive(state);
        foreach (var value in onFalse)
            value.SetActive(!state);
    }
}

[Serializable]
public class IdRectTransform
{
    public string id = "";
    public RectTransform rect;
}
