using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using TMPro;

public class ConfigLoader : MonoBehaviour
{
    public enum AdminTypes
    {
        Heroes,
        Skills,
        Artefacts,
        OriginNClassBonuses,
        Battles,
        MetaConfig,
        Levels,
        Localization,
        ExtraEffects,
        Dialogues,
        Cutscenes,
        LootSet,
        Drops,
        Tasks,
        Mail,
        Player,
        Skilltree,
        Shop,
        Buildings,
        Stages,
        Conditions,
        ConditionsRel,
        Dynamic
    }

    private int downloadCount = 0;
    private int specialCount = 0;
    
    public bool done = false;

    public List<TextAsset> localConfigs = new List<TextAsset>();

    public List<FormatStages> stages = new List<FormatStages>();
    public static Action<List<FormatStages>> onStagesParsed = null;
    
    public List<FormatHero> heroes = new List<FormatHero>();
    public static Action<List<FormatHero>> onHeroesParsed = null;
    
    private List<FormatArtefact> artefacts = new List<FormatArtefact>();
    public static Action<List<FormatArtefact>> onArtefactsParsed = null;

    private List<FormatBonusOrigins> bonuses = new List<FormatBonusOrigins>();
    public static Action<List<FormatBonusOrigins>> onBonusParsed = null;
    
    public List<FormatBattles> battles = new List<FormatBattles>();
    public static Action<List<FormatBattles>> onBattlesParsed = null;

    public List<FormatSkill> skills = new List<FormatSkill>();
    public static Action<List<FormatSkill>> onSkillsParsed = null;
    
    public List<FormatMeta> metaConf = new List<FormatMeta>();
    public static Action<List<FormatMeta>> onMetaParsed = null;
    
    public List<FormatLevel1> levelsOld = new List<FormatLevel1>();
    public static Action<List<FormatLevel1>> onLevelsParsed = null;
    
    public Dictionary<string, FormatLevel> levels = new Dictionary<string, FormatLevel>();
    
    public List<FormatExtraEffect> extraEffects = new List<FormatExtraEffect>();

    public Dictionary<string, FormatLocalization> doctLoc = new Dictionary<string, FormatLocalization>();
    public Dictionary<string, List<FormatDialogue>> dictDialogues = new Dictionary<string, List<FormatDialogue>>();
    public Dictionary<string, FormatSkilltree> dictSkilltrees = new Dictionary<string, FormatSkilltree>();
    
    public Dictionary<string, List<FormatCutscene>> dictCutscenes = new Dictionary<string, List<FormatCutscene>>();
    
    public Dictionary<string, List<Seto>> dictSets = new Dictionary<string, List<Seto>>();
    public Dictionary<string, List<OneLoot>> dictDrops = new Dictionary<string, List<OneLoot>>();
    
    public Dictionary<string, ElTasko> allTasks = new Dictionary<string, ElTasko>();
    public Dictionary<string, ElTasko> allShop = new Dictionary<string, ElTasko>();
    
    public Dictionary<string, UnoCond> allConditions = new Dictionary<string, UnoCond>();
    public Dictionary<string, string> allRelConditions = new Dictionary<string, string>();
    
    public Dictionary<string, FormatDynamic> allDynamic = new Dictionary<string, FormatDynamic>();
    
    public Dictionary<string, FormatBuilding> allBuildings = new Dictionary<string, FormatBuilding>();
    public static Action<Dictionary<string, FormatBuilding>> onBuildingsParsed = null;
    
    public static Action onParseEnded = null;
    public static bool parseEnded;
    
    public static ConfigLoader Instance;

    public bool useLocalExcel = true;

    public List<FormatMail> allMails = new List<FormatMail>();
    public List<FormatPlayer> allPlayer = new List<FormatPlayer>();

    public List<ElTasko> tass = new List<ElTasko>();

    public List<string> stagesSuf = new List<string> { "dragonlair","cave","ruins" };

    public DatabaseAll db;
    [ContextMenu("Watch")]
    public void Watch()
    {
        tass.Clear();
        foreach (var v in allTasks)
        {
            tass.Add(v.Value);
        }
    }
    
    private void OnDestroy()
    {
        Instance = null;
        parseEnded = false;
        onParseEnded = null;
        onLevelsParsed = null;
        onMetaParsed = null;
        onSkillsParsed = null;
        onBattlesParsed = null;
        onBonusParsed = null;
        onArtefactsParsed = null;
        onHeroesParsed = null;
        onBuildingsParsed = null;
    }
    
    public static void SetMetaParamValue(string nm, float val)
    {
        //if (nm == "move_end") return 5;
        var cc = Instance.metaConf.Find(x => x.parName == nm);
        if (cc != null)
        {
            cc.val = val;
        } 

    }
    
    public static float GetMetaParamValue(string nm)
    {
        //if (nm == "move_end") return 5;
        var cc = Instance.metaConf.Find(x => x.parName == nm);
        if (cc != null) return cc.val;
        else return 0;
    }

    public static string GetMetaParamValueString(string nm)
    {
        //if (nm == "move_end") return 5;
        var cc = Instance.metaConf.Find(x => x.parName == nm);
        if (cc != null) return cc.stringVal;
        else return "";
    }
    public void Clear()
    {
        onHeroesParsed = null;
        onArtefactsParsed = null;
        onBonusParsed = null;
        onBattlesParsed = null;
        onSkillsParsed = null;
        onParseEnded = null;
    }
    void Awake()
    {
        CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        ci.NumberFormat.CurrencyDecimalSeparator = ".";
        
        Instance = this;
        db.Init();
        
        if (useLocalExcel)
        {
            StartCoroutine(Do());
            StartCoroutine(DoLoc());
        }
        else
        {
            StartCoroutine(ParseAdmin(
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?output=tsv",
                AdminTypes.Heroes, false, localConfigs.Count > 0 ? localConfigs[0] : null));
            StartCoroutine(ParseAdmin(
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?gid=1714659238&single=true&output=tsv",
                AdminTypes.Artefacts, false, localConfigs.Count > 1 ? localConfigs[1] : null));
            StartCoroutine(ParseAdmin(
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?gid=1769548109&single=true&output=tsv",
                AdminTypes.OriginNClassBonuses, false, localConfigs.Count > 2 ? localConfigs[2] : null));
            StartCoroutine(ParseAdmin(
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?gid=970389661&single=true&output=tsv",
                AdminTypes.Skills, false, localConfigs.Count > 3 ? localConfigs[3] : null));
            //StartCoroutine(ParseAdmin("https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?gid=302540148&single=true&output=tsv", AdminTypes.Skills, false, localConfigs.Count > 3 ? localConfigs[3] : null));

            StartCoroutine(ParseAdmin(
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTn3SdI-oV3t1S3CKh-fpekDnTwLb6ccacqx4pVlKAOYzmfTKiwg-Q8xsulLKA2S5B28CqWW8Hp31WJ/pub?gid=1477423765&single=true&output=tsv",
                AdminTypes.Battles, false, localConfigs.Count > 4 ? localConfigs[4] : null));
        }
    }

    public string CONFIG_NAME = "Config.xlsx";
    
    public IEnumerator Do()
    {
        var loadingRequest = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, CONFIG_NAME));
        loadingRequest.SendWebRequest();
        while (!loadingRequest.isDone) {
            if (loadingRequest.isNetworkError || loadingRequest.isHttpError)
            {
                //ans.text = "ERROR";
                break;
            }

            yield return null;
        }

        //ans.text = loadingRequest.result.ToString();
        //txt.text += loadingRequest.downloadedBytes.ToString();
        
        MemoryStream stream = new MemoryStream(loadingRequest.downloadHandler.data);
        var tt = ExcelHelper.LoadExcel(stream);

        //txt.text += "|" + tt.ToString();
        downloadCount = 1;
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Heroes, false, null, tt.ToTSV("HEROES")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Artefacts, false, null, tt.ToTSV("ITEMS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.OriginNClassBonuses, false, null, tt.ToTSV("ORIGIN BONUS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Skills, false, null, tt.ToTSV("SKILLS2")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Battles, false, null, tt.ToTSV("BATTLES")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Levels, false, null, tt.ToTSV("LEVELS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.ExtraEffects, false, null, tt.ToTSV("EXTRA_EFFECTS")));

        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Dialogues, false, null, tt.ToTSV("DIALOGUES")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Cutscenes, false, null, tt.ToTSV("CUTSCENES")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.MetaConfig, false, null, tt.ToTSV("METACONF")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.LootSet, false, null, tt.ToTSV("LOOTSET")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Drops, false, null, tt.ToTSV("DROPS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Tasks, false, null, tt.ToTSV("TASKS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Mail, false, null, tt.ToTSV("MAIL")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Player, false, null, tt.ToTSV("PLAYER")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Skilltree, false, null, tt.ToTSV("SKILLTREE")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Shop, false, null, tt.ToTSV("SHOP")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Buildings, false, null, tt.ToTSV("BUILDINGS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Stages, false, null, tt.ToTSV("STAGES")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Conditions, false, null, tt.ToTSV("CONDITIONS")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.ConditionsRel, false, null, tt.ToTSV("COND_REL")));
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Dynamic, false, null, tt.ToTSV("DYNAMIC_ID")));

        DownloadingFinished();
    }
    
    public IEnumerator DoLoc()
    {
        var loadingRequest = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "loc.xlsx"));
        loadingRequest.SendWebRequest();
        while (!loadingRequest.isDone) {
            if (loadingRequest.isNetworkError || loadingRequest.isHttpError)
            {
                //ans.text = "ERROR";
                break;
            }

            yield return null;
        }

        //ans.text = loadingRequest.result.ToString();
        //txt.text += loadingRequest.downloadedBytes.ToString();
        
        MemoryStream stream = new MemoryStream(loadingRequest.downloadHandler.data);
        var tt = ExcelHelper.LoadExcel(stream);
        
        
        StartCoroutine(ParseAdmin(
            "",
            AdminTypes.Localization, false, null, tt.ToTSV("UI")));

    }
    public void Trymo(string[] toTrim)
    {
        for (int i = 0; i < toTrim.Length; i++)
            toTrim[i] = toTrim[i].Trim();
    }

    public void DownloadingFinished()
    {
        //null restart ? 
        if (allPlayer.Count == 0)
        {
            Debug.Log("ALL PLAYER: " + allPlayer.Count);
            Invoke("DownloadingFinished", 0.2f);
            return;
        }

        Debug.Log("FINISHED LOADING");
        

        if (onSkillsParsed != null)
        {
            Debug.Log("FILL SKILLS");
            onSkillsParsed(skills);
        }

        if (onHeroesParsed != null)
        {
            Debug.Log("FILL HEROES");
            onHeroesParsed(heroes);
        }

        if (onArtefactsParsed != null)
        {
            Debug.Log("FILL ARTEFACTS");
            onArtefactsParsed(artefacts);
        }

        if (onBuildingsParsed != null)
        {
            onBuildingsParsed(allBuildings);
        }

        if (onBonusParsed != null)
            onBonusParsed(bonuses);
        
        if (onParseEnded != null)
            onParseEnded();        
        
        if (onBattlesParsed != null)
            onBattlesParsed(battles);

        if (onLevelsParsed != null)
            onLevelsParsed(levelsOld);
        
        Debug.Log("FINISHED");
        parseEnded = true;   
        EventManager.INV("PARSE_ENDED", new ArgPass());

    }
    public IEnumerator ParseAdmin(string uri, AdminTypes aType, bool waitZero = false, TextAsset localConfig = null, string localE = null)
    {
        if (waitZero)
        {
            specialCount++;
            while (downloadCount > 0)
            {
                yield return null;
            }
        }
        downloadCount++;
        Debug.Log("Trying : " + aType);

        if (localE != null)
        {
            
            if (waitZero)
                specialCount--;
            
            Debug.Log("Received LOCAL EEE: " + aType + " " + localE.Length);
            Parse(aType, localE);
        }
        else if (localConfig != null)
        {
            yield return null;
            
            if (waitZero)
                specialCount--;
            
            Debug.Log("Received LOCAL: " + aType + " " + localConfig.text.Length);
            Parse(aType, localConfig.text);
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();

                if (waitZero)
                    specialCount--;

                Debug.Log(webRequest.downloadHandler.text);

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("Received: " + aType + " " + webRequest.downloadHandler.text.Length);
                        Parse(aType, webRequest.downloadHandler.text);
                        break;
                }
            }
        }

    }

    public void Parse(AdminTypes aType, string val)
    {
        if (aType == AdminTypes.Heroes) ParseHeroes(val);
        else if (aType == AdminTypes.Skills) ParseSkills(val);
        else if (aType == AdminTypes.Artefacts) ParseArtefacts(val);
        else if (aType == AdminTypes.Battles) ParseBattles(val);
        else if (aType == AdminTypes.OriginNClassBonuses) ParseOriginNClassBonuses(val);
        else if (aType == AdminTypes.MetaConfig) ParseMeta(val);
        else if (aType == AdminTypes.Levels) ParseLevels(val);
        else if (aType == AdminTypes.Localization) ParseLocalization(val);
        else if (aType == AdminTypes.ExtraEffects) ParseExtraEffects(val);
        else if (aType == AdminTypes.Dialogues) ParseDialogues(val);
        else if (aType == AdminTypes.Cutscenes) ParseCutscenes(val);
        else if (aType == AdminTypes.LootSet) ParseLootsets(val);
        else if (aType == AdminTypes.Drops) ParseDrops(val);
        else if (aType == AdminTypes.Tasks) ParseTasks(val);
        else if (aType == AdminTypes.Mail) ParseMail(val);
        else if (aType == AdminTypes.Player) ParsePlayer(val);
        else if (aType == AdminTypes.Skilltree) ParseSkilltree(val);
        else if (aType == AdminTypes.Shop) ParseShop(val);
        else if (aType == AdminTypes.Buildings) ParseBuildings(val);
        else if (aType == AdminTypes.Stages) ParseStages(val);
        else if (aType == AdminTypes.Conditions) ParseConditions(val);
        else if (aType == AdminTypes.ConditionsRel) ParseConditionsRel(val);
        else if (aType == AdminTypes.Dynamic) ParseDynamic(val);
        
        
        downloadCount--;

        if (downloadCount == 0 && specialCount == 0)
            DownloadingFinished();
    }

    public void ParseConditions(string val)
    {
        //TASKID	CATEGORY	REWARDS	REQSTART	REQFINISH	REQITEMS	EXPIRE	DESCRIPTION	ICON	MARKETID	MARKETPRICE	LIMIT	FREE_EVERY	STAT_NEW

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty;
        var mm = new UnoCond();
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }

                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm = new UnoCond();
                        mm.id = tt[j];
                        lastId = mm.id;
                        allConditions.Add(mm.id, mm);
                    }

                    //mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "REQS")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqs.Add(ur);
                }

            }

            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
    }
    
    public void ParseConditionsRel(string val)
    {
        //TASKID	CATEGORY	REWARDS	REQSTART	REQFINISH	REQITEMS	EXPIRE	DESCRIPTION	ICON	MARKETID	MARKETPRICE	LIMIT	FREE_EVERY	STAT_NEW

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty;
        var mm = new UnoCond();
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }

                if (columns[j].ToUpper() == "OBJ_ID")
                {
                    if (tt[j] != String.Empty)
                    {
                        allRelConditions.Add(tt[j],"");
                        lastId = tt[j];
                    }

                    //mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "COND_ID")
                {
                    allRelConditions[lastId] = tt[j];
                }

            }

            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
    }

    public void ParseDynamic(string val)
    {
        //TASKID	CATEGORY	REWARDS	REQSTART	REQFINISH	REQITEMS	EXPIRE	DESCRIPTION	ICON	MARKETID	MARKETPRICE	LIMIT	FREE_EVERY	STAT_NEW

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty;
        FormatDynamic mm = new FormatDynamic();
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }

                if (columns[j].ToUpper() == "ID")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm = new FormatDynamic();
                        mm.id = tt[j];
                        allDynamic.Add(tt[j],mm);
                        lastId = tt[j];
                    }

                    //mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "PRICE")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.price.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1]), Val2 = yp.Length > 2 ? yp[2] : ""});                        
                    }
                }
                else if (columns[j].ToUpper() == "CONDS")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.conds1.Add(new Bon1{Key = yp[0], Value = float.Parse(yp[1], CultureInfo.InvariantCulture), Val2 = yp.Length > 2 ? yp[2] : ""});                        
                    }
                }
                else if (columns[j].ToUpper() == "STATS_INC")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.statsInc.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "STATS_EXACT")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.statsExact.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "ITEMS_GET")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.itemsGet.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1]), Val2 = yp.Length > 2 ? yp[2] : ""});                     
                    }
                }
                else if (columns[j].ToUpper() == "PAR_UPGRADE")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.parUpgrade.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1]), Val2 = yp.Length > 2 ? yp[2] : ""});                     
                    }
                }
                else if (columns[j].ToUpper() == "PARENT_UPGRADE")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.parentUpgrade.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1]), Val2 = yp.Length > 2 ? yp[2] : ""});                     
                    }
                }
                else if (columns[j].ToUpper() == "UNITS_BUILD")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.unitsBuild.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "OBJ_ACTIVATE")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.unitsBuild.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "SKILL_UNLOCK")
                {
                    var yh = tt[j].Split(",");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        mm.skillUnlock.Add(yh[o]);                       
                    }
                }
                else if (columns[j].ToUpper() == "SKILL_PAS_UNLOCK")
                {
                    var yh = tt[j].Split(",");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        mm.skillPasUnlock.Add(yh[o]);                       
                    }
                }
                else if (columns[j].ToUpper() == "EVENT_TRIGGER") mm.eventTrigger = tt[j];
                else if (columns[j].ToUpper() == "EVENT_VAL") mm.eventVal = tt[j];
                else if (columns[j].ToUpper() == "EVENT_NUM") mm.eventNum = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "SUBSCRIBE") mm.subscribe = tt[j];
                else if (columns[j].ToUpper() == "CREATE") mm.create = tt[j];
                else if (columns[j].ToUpper() == "REWARD") mm.reward = tt[j];
                else if (columns[j].ToUpper() == "PARAM") mm.param = tt[j];
                
                else if (columns[j].ToUpper() == "DIALOG") mm.dialog = tt[j];
                else if (columns[j].ToUpper() == "CUTSCENE") mm.cutscene = tt[j];
                
                else if (columns[j].ToUpper() == "FILTER_NUM") mm.filterNum = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "TIME") mm.time = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "MULTI") mm.multi = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "DYN_LIST")
                {
                    var yh = tt[j].Split(",");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        mm.dynList.Add(yh[o]);                       
                    }
                }
                else if (columns[j].ToUpper() == "COND")
                {
                    var yh = tt[j].Split(",");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        mm.condList.Add(yh[o]);                       
                    }
                }
            }

            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
    }
    public void ParseStages(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        
        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatStages();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (columns[j].ToUpper() == "NAME") mm.nm = tt[j];
                else if (columns[j].ToUpper() == "NORMAL_BATTLES") mm.normalBattles = tt[j].Split(',').ToList();
                else if (columns[j].ToUpper() == "ELITE_BATTLES") mm.eliteBattles = tt[j].Split(',').ToList();
                else if (columns[j].ToUpper() == "BOSS_BATTLES") mm.bossBattles = tt[j].Split(',').ToList();
                else if (columns[j].ToUpper() == "REWARDS")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.rewards.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }


            }
            //
            stages.Add(mm);
        }

        
    }

    public void ParseHeroes(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME	COST	ORIGIN	CLASS	HEALTH	ARMOR	ATTACK	RANGE	ASPEED	DPS	SKILLULT	SKILLBASIC	INI_MANA	MAX_MANA

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatHero();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x") continue;
                if (columns[j].ToUpper() == "NAME") mm.monsterName = tt[j];
                
                if (columns[j].ToUpper() == "DISPLAYNAME") mm.displayName = tt[j];
                else if (columns[j].ToUpper() == "ORIGIN") mm.origins.Add(tt[j]);
                else if (columns[j].ToUpper() == "CLASS") mm.classes.Add(tt[j]);
                else if (columns[j].ToUpper() == "DYNAMIC") mm.dynamic = tt[j];
                
                else if (columns[j].ToUpper() == "HEALTH")
                {
                    mm.health = int.Parse(tt[j]);
                    mm.maxHealth = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "MOVE") mm.move = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "LEVEL") mm.level = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "COST") mm.cost = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "ARMOR") mm.armor = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "ATTACK") mm.attack = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "SIZE") mm.size = int.Parse(tt[j]);
                
                
                else if (columns[j].ToUpper() == "ASPEED") mm.attackSpeed = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "LIFESTEAL_PRC") mm.lifestealPrc = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "REGEN") mm.regen = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                
                //else if (columns[j].ToUpper() == "RARITY") mm.rarity = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "SKILLBASIC") mm.skillBasic = tt[j];
                else if (columns[j].ToUpper() == "TREE") mm.sklTree = tt[j];
                else if (columns[j].ToUpper() == "SKILLULT") mm.skillUltimate = tt[j];
                else if (columns[j].ToUpper() == "SKILLOTHER") mm.skillOthers.AddRange(tt[j].Split(','));
                else if (columns[j].ToUpper() == "SKILLINST") mm.skillInst.AddRange(tt[j].Split(','));
                else if (columns[j].ToUpper() == "TRAITS") mm.skillTraits.AddRange(tt[j].Split(','));
                else if (columns[j].ToUpper() == "INI_MANA") mm.mana = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "MAX_MANA") mm.maxMana = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "STARS") mm.stars = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "COUNTASUNIT") mm.countAsUnit = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "AGGRO") mm.aggroRange = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "PRICE") mm.price = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "INITIATIVE") mm.initiative = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "MAGICRES") mm.magicResist = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "DESCRIPTION") mm.description = tt[j];
                else if (columns[j].ToUpper() == "ROLE") mm.role = tt[j];
                else if (columns[j].ToUpper() == "FORMATION") mm.formation = tt[j];
                
                else if (columns[j].ToUpper() == "SPEED") mm.speed = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DODGE") mm.dodge = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "ACCURACY") mm.accuracy = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "APOINTS") mm.apoints = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "NO_MOVE") mm.noMove = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "DO_NOTHING") mm.doNothing = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "IMMORTAL") mm.immortal = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "PASSABLE") mm.passable = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "BUILDING") mm.building = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "DROP_PICK") mm.dropPick = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "CRIT_CHANCE") mm.critChance = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "CRIT_DMG") mm.critDamage = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "MAX_DMG_TAKEN") mm.maxDmgTaken = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DMG_BLOCK") mm.dmgBlock = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "ELEMENT") mm.element = Enum.Parse<ElementType>(tt[j]);
                else if (columns[j].ToUpper() == "RARITY") mm.rarity = (int)Enum.Parse<RarityType>(tt[j]);
                else if (columns[j].ToUpper() == "DROP") mm.drop = tt[j];
                
                else if (columns[j].ToUpper() == "ON_DEATH") mm.onDeath = tt[j];
                else if (columns[j].ToUpper() == "ON_DMG") mm.onDmg = tt[j];
                
                
                else if (columns[j].ToUpper() == "DROP_PER_HIT") mm.dropPerHit = tt[j];
                
            }
            //
            heroes.Add(mm);
        }

    }
    
    public void ParseExtraEffects(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        //NAME	SKILL	DESCRIPTION
        Trymo(columns);

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatExtraEffect();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (columns[j].ToUpper() == "CONDITION")
                {
                    var gg = tt[j].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    mm.artConditions = gg.ToList();
                }
                else if (columns[j].ToUpper() == "EFFECT") mm.endEffect = tt[j];
            }
            //
            extraEffects.Add(mm);
        }

    }

    public void ParseSkills(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME	SKILLTYPE	MANACOST	DESCRIPTION	SKILLJSON

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatSkill();
            var tt = str[i].Split("\t", StringSplitOptions.None);
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x") continue;
                if (columns[j].ToUpper() == "NAME") mm.skillName = tt[j];
                else if (columns[j].ToUpper() == "ATTACK") mm.ATTACK = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "ATTACK_PRC") mm.ATTACK_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "HEALTH") mm.HEALTH = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "HEALTH_PRC") mm.HEALTH_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DEF") mm.DEF = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DEF_PRC") mm.DEF_PRC =float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RES") mm.RES = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RES_PRC") mm.RES_PRC =float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_HEALTH") mm.MAX_HEALTH = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_HEALTH_PRC") mm.MAX_HEALTH_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MANA") mm.MANA = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_MANA") mm.MAX_MANA = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "SPEED") mm.SPEED = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "TAG_APPLY") mm.TAG_APPLY = tt[j];
                else if (columns[j].ToUpper() == "PEN_CNT") mm.PEN_CNT = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RARITY") mm.RARITY = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "DODGE") mm.dodge = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DODGE_PRC") mm.dodge_prc = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MANACOST")
                {
                    mm.manaCost = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "RICOCHET") mm.ricochet = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "BOUNCE") mm.bounce = int.Parse(tt[j]);
                
                
                else if (columns[j].ToUpper() == "MAX_DMG_TAKEN") mm.maxDmgTaken = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DMG_BLOCK") mm.dmgBlock = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "ON_DEATH") mm.onDeath = tt[j];
                else if (columns[j].ToUpper() == "ON_DMG") mm.onDmg = tt[j];
                else if (columns[j].ToUpper() == "SPAWN") mm.spawn = tt[j];
                
                
                else if (columns[j].ToUpper() == "ACCURACY") mm.accuracy = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "APOINTS") mm.apoints = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "ANGLE") mm.ANGLE = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "REGEN") mm.ANGLE = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "LIFESTEAL_PRC") mm.ANGLE = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "IMMORTAL") mm.immortal = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "UNIQUE") mm.unique = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "DT") mm.DT = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "INSTANT") mm.INSTANT = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "COOLDOWN") mm.COOLDOWN = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RANGE") mm.RANGE = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "CRIT_CHANCE") mm.CRIT_CHANCE = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "CRIT_DMG") mm.CRIT_DMG = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
                else if (columns[j].ToUpper() == "FILTER_HP") mm.FILTER_HP = tt[j];
                else if (columns[j].ToUpper() == "FILTER_ATK") mm.FILTER_ATK = tt[j];
                else if (columns[j].ToUpper() == "FILTER_RANGE") mm.FILTER_RANGE = tt[j];
                else if (columns[j].ToUpper() == "FILTER_SELF") mm.FILTER_SELF = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "TARGETS") mm.targets = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "REQ_ACTION") mm.ACTION_REQ = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "REQ_EMPTY") mm.EMPTY_REQ = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "SHIELD") mm.SHIELD = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "FIRST") mm.FIRST = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "REQ2") mm.req2 = int.Parse(tt[j]);
                
                else if (columns[j].ToUpper() == "SECOND") mm.SECOND = tt[j];
                
                else if (columns[j].ToUpper() == "MANA_REC") mm.MANA_REQ = int.Parse(tt[j]);
                
                
                
                else if (columns[j].ToUpper() == "PARS")
                {
                    if (tt[j] == "x") continue;
                    var yy = tt[j].Split("#");
                    mm.PARS = new List<Bon>();
                    for (int k = 0; k < yy.Length; k++)
                    {
                        var bb = yy[k].Split(",");
                        mm.PARS.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                    }
                }
                else if (columns[j].ToUpper() == "AOE") mm.aoe = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "AMOUNT") mm.amount = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "TRAVEL") mm.travel = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "AFFECTED")
                {
                    if (tt[j] == "x") continue;
                    var yy = tt[j].Split(",");
                    mm.affected = new List<string>();
                    for (int k = 0; k < yy.Length; k++)
                    {
                        mm.affected.Add(yy[k]);
                    }
                }
                else if (columns[j].ToUpper() == "BUFF_APPLY")
                {
                    if (tt[j] == "x") continue;
                    var yy = tt[j].Split(",");
                    mm.buffApply = new List<string>();
                    for (int k = 0; k < yy.Length; k++)
                    {
                        mm.buffApply.Add(yy[k]);
                    }
                }
                else if (columns[j].ToUpper() == "CD_RED") mm.cdReduction = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "TIME") mm.time = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DMG_EVERY") mm.dmgEvery = float.Parse(tt[j], CultureInfo.InvariantCulture);
                
            }
            //
            skills.Add(mm);
        }
        
    }
    
    public void ParseArtefacts(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME	SKILL	DESCRIPTION

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatArtefact();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j]== "x") continue;
                if (columns[j].ToUpper() == "NAME") mm.skillName = tt[j];
                else if (columns[j].ToUpper() == "ATTACK") mm.ATTACK = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "ATTACK_PRC") mm.ATTACK_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "HEALTH") mm.HEALTH = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "HEALTH_PRC") mm.HEALTH_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DEF") mm.DEF = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "DEF_PRC") mm.DEF_PRC =float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RES") mm.RES = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RES_PRC") mm.RES_PRC =float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_HEALTH") mm.MAX_HEALTH = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_HEALTH_PRC") mm.MAX_HEALTH_PRC = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MANA") mm.MANA = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "MAX_MANA") mm.MAX_MANA = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "SPEED") mm.SPEED = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "TAG_APPLY") mm.TAG_APPLY = tt[j];
                else if (columns[j].ToUpper() == "PEN_CNT") mm.PEN_CNT = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "INSTANT") mm.INSTANT = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "RARITY") mm.RARITY = tt[j];
                else if (columns[j].ToUpper() == "SLOT") mm.SLOT = tt[j];
                else if (columns[j].ToUpper() == "REF_SKILL") mm.REF_SKILL = tt[j];
                else if (columns[j].ToUpper() == "SIZE") mm.size = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "PRICE")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.price.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                
                
            }
            //
            artefacts.Add(mm);
        }

    }
    
    public void ParseMeta(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        //NAME	SKILL	DESCRIPTION
        Trymo(columns);

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatMeta();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (columns[j].ToUpper() == "PARAM") mm.parName = tt[j];
                else if (columns[j].ToUpper() == "VALUE") mm.val = float.Parse(tt[j], CultureInfo.InvariantCulture);
                else if (columns[j].ToUpper() == "STRINGVAL") mm.stringVal = tt[j];
            }
            //
            metaConf.Add(mm);
        }

    }
    
    public void ParseLocalization(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        //NAME	SKILL	DESCRIPTION
        Trymo(columns);

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatLocalization();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (columns[j].ToUpper() == "KEYS") mm.id = tt[j];
                else if (columns[j].ToUpper() == "ENGLISH") mm.EN = tt[j];
                else if (columns[j].ToUpper() == "UKRAINIAN") mm.UKR = tt[j];
                else if (columns[j].ToUpper() == "RUSSIAN") mm.RUS = tt[j];
                else if (columns[j].ToUpper() == "CHINESE SIMPL") mm.CHN_S = tt[j];
                else if (columns[j].ToUpper() == "CHINESE TRAD") mm.CHN_T = tt[j];
                else if (columns[j].ToUpper() == "KOREAN") mm.KOR = tt[j];
                else if (columns[j].ToUpper() == "JAPANESE") mm.JAP = tt[j];
                else if (columns[j].ToUpper() == "UKRAINIAN") mm.JAP = tt[j];

            }
            //
            //Debug.Log(mm.id);
            doctLoc.Add(mm.id, mm);
        }

        Debug.Log(doctLoc["battle_exit"].EN);
    }
    public void ParseOriginNClassBonuses(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME	SKILL  CONDITION	DESCRIPTION

        for (int i = 1; i < str.Length; i++)
        {
            var mm = new FormatBonusOrigins();
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < tt.Length; j++)
            {
                if (columns[j].ToUpper() == "NAME") mm.bonusName = tt[j];
                else if (columns[j].ToUpper() == "SKILL") mm.skill = tt[j];
                else if (columns[j].ToUpper() == "DESCRIPTION") mm.description = tt[j];
                else if (columns[j].ToUpper() == "FAIL_LOC") mm.falseLoc = tt[j];
                else if (columns[j].ToUpper() == "RENDER") mm.render = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "TOWER") mm.tower = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "KINDEX") mm.kindex = int.Parse(tt[j]);
                else if (columns[j].ToUpper() == "CONDITION")
                {
                    var ttq = tt[j].Split("#", StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < ttq.Length; k++)
                    {
                        var ttb = ttq[k].Split(",", StringSplitOptions.RemoveEmptyEntries);
                        mm.conditions.Add(new KeyValuePair<string, int>(ttb[0], int.Parse(ttb[1])));
                    }
                }
            }
            //
            bonuses.Add(mm);
        }
        
    }
    
    public void ParseLevels_1(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME  VALUE ADDS

        var mm = new FormatLevel1();        
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            bool oo = false;
            for (int j = 0; j < tt.Length; j++)
            {
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] == "x")
                    {
                        oo = true;
                    }
                    else
                    {
                        if (mm.obstacles != string.Empty && mm.obstacles.Length > 2)
                        {
                            levelsOld.Add(mm);
                            mm = new FormatLevel1();
                        }
                        mm.id = tt[j];
                    }
                }
                else if (columns[j].ToUpper() == "VALUE")
                {
                    if (!oo) mm.obstacles = tt[j];
                    else mm.obstacles = mm.obstacles + "\n" + tt[j];
                }
                else if (columns[j].ToUpper() == "ADDS")
                {
                    if (!oo) mm.adds = tt[j];
                    else mm.adds = mm.adds + "\n" + tt[j];
                }
            }
            //
            
            //levels.Add(mm);
        }
        
        if (mm.obstacles != string.Empty)
            levelsOld.Add(mm);        
        
    }
    
    public void ParseLevels(string val)
    {
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        //NAME  VALUE ADDS

        var mm = new FormatLevel();
        int cur = 0;
        for (int i = 0; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            bool oo = false;
            if (tt.Length < 2)
            {
                if (tt[0] == "") continue;
                if (tt[0].IndexOf("LEVEL") >= 0)
                {
                    if (mm.id != "")
                    {
                        mm.n = cur;
                        levels.Add(mm.id, mm);
                        mm = new FormatLevel();
                        mm.id = tt[0];
                        cur = 0;
                    }
                    else
                    {
                        mm = new FormatLevel();
                        mm.id = tt[0];
                        cur = 0;
                    }

                    continue;
                }
            }

            if (mm.m == 0)
            {
                mm.m = tt.Length;
            }
            
            for (int j = 0; j < tt.Length; j++)
            {
                if (j == 0)
                {
                    mm.map.Add(new LevelPars[mm.m]);
                    for (int k = 0; k < mm.m; k++)
                    {
                        mm.map[cur][k] = new LevelPars();
                    }
                }
                
                var yy = tt[j].Split(',');
                for (int k = 0; k < yy.Length; k++)
                    mm.map[cur][j].pars.Add(yy[k]);
            }

            cur++;
            //

            //levels.Add(mm);
        }

        if (mm.id != string.Empty)
        {
            mm.n = cur;
            levels.Add(mm.id, mm);
        }
    }

    public void ParseBattles(string val)
    {
        //NAME	HERO-LEVEL-POSITION	HERO-ARTEFACT	HERO-KINGDOM		ENEMY-LEVEL-POSITION	ENEMY-ARTEFACT	ENEMY-KINGDOM
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);
        
        var mm = new FormatBattles();

        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j]== "x") continue;
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (mm.battleName != String.Empty)
                    {
                        battles.Add(mm);
                        mm = new FormatBattles();
                    }
                    
                    mm.battleName = tt[j];
                }
                else if (columns[j].ToUpper() == "LEVELREF")
                {
                    mm.levelRef = tt[j];
                }
                else if (columns[j].ToUpper() == "TIME")
                {
                    mm.enemies.timeSpawns.Add(float.Parse(tt[j], CultureInfo.InvariantCulture));
                }
                else if (columns[j].ToUpper() == "SIDE")
                {
                    mm.enemies.sides.Add(int.Parse(tt[j], CultureInfo.InvariantCulture));
                }
                else if (columns[j].ToUpper() == "COMPLEXITY")
                {
                    mm.complexity = int.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "AMOUNT")
                {
                    mm.enemies.amounts.Add(int.Parse(tt[j]));
                }
                else if (columns[j].ToUpper() == "BACKG")
                {
                    mm.bgNum = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "POS_REWARDS")
                {
                    mm.posReward = tt[j];
                }
                else if (columns[j].ToUpper() == "FIRST_REWARDS")
                {
                    var bb = tt[j].Split(",");
                    mm.firstReward.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                }
                else if (columns[j].ToUpper() == "3STAR_REWARDS")
                {
                    var bb = tt[j].Split(",");
                    mm.star3Reward.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                }
                else if (columns[j].ToUpper() == "REQSTART")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqStart.Add(ur);
                }
                else if (columns[j].ToUpper() == "MAPPOS")
                {
                    var gg = tt[j].Split("#");
                    mm.mapPosition =  new Vector2(float.Parse(gg[0]), float.Parse(gg[1]));
                }
                else if (columns[j].ToUpper() == "HERO-LEVEL-POSITION")
                {
                    var gg = tt[j].Split(",");
                    mm.heroes.heroLevelPosition.Add(new Tuple<string, int, Vector2, int>(gg[0], int.Parse(gg[1]), new Vector2(float.Parse(gg[2]), float.Parse(gg[3])),0));
                    
                    mm.heroes.heroArtefacts.Add(new List<string>());
                    mm.heroes.heroKingdomBonus.Add(new List<int>());
                    mm.heroes.heroPerks.Add(new List<string>());
                }
                else if (columns[j].ToUpper() == "HERO-ARTEFACT")
                {
                    string[] gg = null;
                    if (tt[j].IndexOf("{") >= 0 || tt[j].IndexOf("#") >= 0)
                    {
                        gg = tt[j].Split("#");
                    }
                    else
                        gg = tt[j].Split(",");
                    
                    var ii = new List<string>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(gg[k]);

                    mm.heroes.heroArtefacts[mm.heroes.heroArtefacts.Count - 1] = ii;
                }
                else if (columns[j].ToUpper() == "HERO-PERKS")
                {
                    var gg = tt[j].Split(",");
                    var ii = new List<string>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(gg[k]);

                    mm.heroes.heroPerks[mm.heroes.heroPerks.Count - 1] = ii;
                }
                else if (columns[j].ToUpper() == "HERO-KINGDOM")
                {
                    var gg = tt[j].Split(",");
                    var ii = new List<int>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(int.Parse(gg[k]));

                    mm.heroes.heroKingdomBonus[mm.heroes.heroKingdomBonus.Count - 1] = ii;
                }
                else if (columns[j].ToUpper() == "ENEMY-LEVEL-POSITION")
                {
                    var gg = tt[j].Split(",");
                    mm.enemies.heroLevelPosition.Add(new Tuple<string, int, Vector2, int>(gg[0], int.Parse(gg[1]), new Vector2(float.Parse(gg[2]), float.Parse(gg[3])), 0));
                  
                    mm.enemies.heroArtefacts.Add(new List<string>());
                    mm.enemies.heroKingdomBonus.Add(new List<int>());
                    mm.enemies.heroPerks.Add(new List<string>());
                }
                else if (columns[j].ToUpper() == "ENEMY-ARTEFACT")
                {
                    var gg = tt[j].Split(",");
                    var ii = new List<string>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(gg[k]);

                    mm.enemies.heroArtefacts[mm.enemies.heroArtefacts.Count - 1] = ii;
                }
                else if (columns[j].ToUpper() == "ENEMY-PERKS")
                {
                    var gg = tt[j].Split(",");
                    var ii = new List<string>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(gg[k]);

                    mm.enemies.heroPerks[mm.enemies.heroPerks.Count - 1] = ii;
                }
                else if (columns[j].ToUpper() == "ENEMY-KINGDOM")
                {
                    var gg = tt[j].Split(",");
                    var ii = new List<int>();
                    
                    for (int k = 0; k < gg.Length; k++)
                        ii.Add(int.Parse(gg[k]));

                    mm.enemies.heroKingdomBonus[mm.enemies.heroKingdomBonus.Count - 1] = ii;
                }
            }
            //
        }
        
        battles.Add(mm);        
        
    }
    
    public void ParseDialogues(string val)
    {
        //NAME	PHRASE	ACTION	AVA1	AVA2	TAVA1	TAVA2
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            var mm = new FormatDialogue();            
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    mm.id = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm.id = tt[j];
                        lastId = mm.id;
                        dictDialogues.Add(mm.id, new List<FormatDialogue>());
                    }
                    
                    mm.id = tt[j];
                }
                else if (columns[j].ToUpper() == "PHRASE")
                {
                    mm.phrase = tt[j];
                }
                else if (columns[j].ToUpper() == "ACTION")
                {
                    mm.action = tt[j];
                }
                else if (columns[j].ToUpper() == "AVA1")
                {
                    mm.ava1 = tt[j];
                }
                else if (columns[j].ToUpper() == "AVA2")
                {
                    mm.ava2 = tt[j];
                }
                else if (columns[j].ToUpper() == "TAVA1")
                {
                    mm.avaT1 = tt[j];
                }
                else if (columns[j].ToUpper() == "TAVA2")
                {
                    mm.avaT2 = tt[j];
                }
                else if (columns[j].ToUpper() == "SPEAKER")
                {
                    mm.speaker = tt[j];
                }
                else if (columns[j].ToUpper() == "REQ_TRIGGER")
                {
                    mm.req_trigger = tt[j];
                }
                else if (columns[j].ToUpper() == "REQ_SHOW")
                {
                    mm.req_show = tt[j];
                }
                else if (columns[j].ToUpper() == "COND")
                {
                    mm.cond = tt[j];
                }
                else if (columns[j].ToUpper() == "C_START")
                {
                    mm.cStart = tt[j];
                }
                

            }
            //
            if (lastId == string.Empty) continue;
            dictDialogues[lastId].Add(mm);        
        }
        
     
        
    }
    
        public void ParseSkilltree(string val)
    {
        //NAME	SKILL	COND	PRICE	LEVEL	ID	REQ
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        var mm = new FormatSkilltree();        
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.id = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm = new FormatSkilltree();
                        mm.nm = tt[j];
                        lastId = mm.nm;
                        dictSkilltrees.Add(mm.nm, mm);
                    }
                    
                    //mm.id = tt[j];
                }
                else if (columns[j].ToUpper() == "SKILL")
                {
                    SkillCond cc = new SkillCond();
                    cc.sklName = tt[j];
                    mm.skills.Add(cc);
                }
                else if (columns[j].ToUpper() == "LEVEL")
                {
                    mm.skills[mm.skills.Count - 1].level = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "ID")
                {
                    mm.skills[mm.skills.Count - 1].treeId = tt[j];
                }
                else if (columns[j].ToUpper() == "PRICE")
                {
                    var yy = tt[j].Split("#");
                    var hh = mm.skills[mm.skills.Count - 1];
                    hh.price = new List<Bon>();
                    for (int k = 0; k < yy.Length; k++)
                    {
                        var bb = yy[k].Split(",");
                        hh.price.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                    }
                }
                else if (columns[j].ToUpper() == "COND")
                {
                    var yy = tt[j].Split("#");
                    var hh = mm.skills[mm.skills.Count - 1];
                    hh.conds = new List<Bon>();
                    for (int k = 0; k < yy.Length; k++)
                    {
                        var bb = yy[k].Split(",");
                        hh.conds.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                    }
                }


            }
            //
            if (lastId == string.Empty) continue;
            //dictDialogues[lastId].Add(mm);        
        }
        
     
        
    }
    
    
    
    public void ParseLootsets(string val)
    {
        //NAME	GROUP	WEIGHT	AMOUNT1	AMOUNT2	ITEM	SHARD
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            var mm = new Seto();            
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    mm.name = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm.name = tt[j];
                        lastId = mm.name;
                        dictSets.Add(mm.name, new List<Seto>());
                    }
                    
                    mm.name = tt[j];
                }
                else if (columns[j].ToUpper() == "GROUP")
                {
                    mm.group = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "WEIGHT")
                {
                    mm.weight = float.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "AMOUNT1")
                {
                    mm.amount1 = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "AMOUNT2")
                {
                    mm.amount2 = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "ITEM")
                {
                    mm.item = tt[j];
                }
                else if (columns[j].ToUpper() == "SHARD")
                {
                    mm.shard = int.Parse(tt[j]);
                }

            }
            //
            if (lastId == string.Empty) continue;
            dictSets[lastId].Add(mm);        
        }
        
     
        
    }
    
        public void ParseMail(string val)
    {
        //NAME	HEADER	DESCRIPTION	START_DATE	END_DATE	REWARDS
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 
        FormatMail mm = null;    
        
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

        
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm = new FormatMail();
                        mm.id = tt[j];
                        lastId = mm.id;
                        allMails.Add(mm);
                    }
                    
                }
                else if (columns[j].ToUpper() == "HEADER")
                {
                    mm.header = tt[j];
                }
                else if (columns[j].ToUpper() == "DESCRIPTION")
                {
                    mm.description = tt[j];
                }
                else if (columns[j].ToUpper() == "START_DATE")
                {
                    DateTime result = DateTime.Parse(tt[j]);
                    mm.startDate = result.Ticks;
                }
                else if (columns[j].ToUpper() == "END_DATE")
                {
                    DateTime result = DateTime.Parse(tt[j]);
                    mm.endDate = result.Ticks;
                }
                else if (columns[j].ToUpper() == "REWARDS")
                {
                    var yp = tt[j].Split(",");
                    mm.rewards.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});
                }

            }
            //
            if (lastId == string.Empty) continue;
   
        }
        
     
        
    }
 
    public void ParsePlayer(string val)
    {
        //NAME	COMMON INVENTORY
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 
        FormatPlayer mm = null;    
        
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            
            if (tt[0] == "name")
            {
                mm = new FormatPlayer();
                mm.nm = tt[1];
            }
            else if (tt[0] == "common")
            {
                mm.common = tt[1];
            }
            else if (tt[0] == "inventory")
            {
                mm.inven = tt[1];
                allPlayer.Add(mm);
            }
            else if (tt[0] == "items")
            {
                var yy = tt[1].Split("#");
                var hh = new List<Bon>();
                for (int k = 0; k < yy.Length; k++)
                {
                    var bb = yy[k].Split(",");
                    hh.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                }

                mm.items = hh;
            }
            else if (tt[0] == "stats")
            {
                var yy = tt[1].Split("#");
                var hh = new List<Bon>();
                for (int k = 0; k < yy.Length; k++)
                {
                    var bb = yy[k].Split(",");
                    hh.Add(new Bon{Key = bb[0], Value = int.Parse(bb[1])});
                }

                mm.stats = hh;
            }
            else if (tt[0] == "dyn_taken")
            {
                var yy = tt[1].Split(",");
                for (int k = 0; k < yy.Length; k++)
                {
                    mm.dynTaken.Add(yy[k]);
                }
                
            }
   
        }
        
     
        
    }
        
        public void ParseTasks(string val)
    {
        //TASKID	CATEGORY	REWARDS	REQSTART	REQFINISH	REQITEMS	EXPIRE	DESCRIPTION	ICON	MARKETID	MARKETPRICE	LIMIT	FREE_EVERY	STAT_NEW

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 
        var mm = new ElTasko();  
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "TASKID")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm = new ElTasko(); 
                        mm.id = tt[j];
                        lastId = mm.id;
                        allTasks.Add(mm.id, mm);
                    }
                    
                    //mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "CATEGORY")
                {
                    mm.category = Enum.Parse<ElTasko.Category>(tt[j]);
                }
                else if (columns[j].ToUpper() == "REWARDS")
                {
                    var yp = tt[j].Split(",");
                    mm.rewards.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});
                }
                else if (columns[j].ToUpper() == "REQITEMS")
                {
                    var yp = tt[j].Split(",");
                    mm.reqItems.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});
                }
                else if (columns[j].ToUpper() == "REQSTART")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqStart.Add(ur);
                }
                else if (columns[j].ToUpper() == "REQFINISH")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqFinish.Add(ur);
                }
                else if (columns[j].ToUpper() == "EXPIRE")
                {
                    string date = "09/16/2019 12:00:00 AM";
                    DateTime result = DateTime.Parse(tt[j]);
                    mm.expire = result.Ticks;
                }
                else if (columns[j].ToUpper() == "DESCRIPTION")
                {
                    mm.description = tt[j];
                }
                else if (columns[j].ToUpper() == "ICON")
                {
                    Debug.Log(mm);
                    Debug.Log(ResourceHolder.instance);
                    mm.icon = ResourceHolder.instance.GetMisc(tt[j]);
                }
                else if (columns[j].ToUpper() == "MARKETID")
                {
                    mm.realID = tt[j];
                }
                else if (columns[j].ToUpper() == "MARKETPRICE")
                {
                    mm.realPrice = float.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "LIMIT")
                {
                    mm.limit = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "FREE_EVERY")
                {
                    mm.freeEvery = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "STAT_NEW")
                {
                    mm.startStatNew = bool.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "AUTO_TAKE")
                {
                    mm.autoTake = bool.Parse(tt[j]);
                }

            }
            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
        
    }
    
    public void ParseShop(string val)
    {
        //TASKID	CATEGORY	REWARDS	REQSTART	REQFINISH	REQITEMS	EXPIRE	DESCRIPTION	ICON	MARKETID	MARKETPRICE	LIMIT	FREE_EVERY	STAT_NEW

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        var mm = new ElTasko();         
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

           
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "TASKID")
                {
                    if (tt[j] != String.Empty && tt[j] != "")
                    {
                        mm = new ElTasko(); 
                        mm.id = tt[j];
                        lastId = mm.id;
                        allShop.Add(mm.id, mm);
                    }
                    
                    //mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "CATEGORY")
                {
                    mm.category = Enum.Parse<ElTasko.Category>(tt[j]);
                }
                else if (columns[j].ToUpper() == "REWARDS")
                {
                    //Debug.Log("FFF " + tt[j] + " " + mm.id);
                    var yp = tt[j].Split(",");
                    mm.rewards.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});
                }
                else if (columns[j].ToUpper() == "REQITEMS")
                {
                    var yp = tt[j].Split(",");
                    mm.reqItems.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});
                }
                else if (columns[j].ToUpper() == "REQSTART")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqStart.Add(ur);
                }
                else if (columns[j].ToUpper() == "REQFINISH")
                {
                    var yp = tt[j].Split(",");
                    UnoReq ur = new UnoReq();
                    ur.typo = Enum.Parse<TaskType>(yp[0]);
                    ur.what = yp[1];
                    ur.val = yp[2];
                    ur.compar = yp[3];
                    mm.reqFinish.Add(ur);
                }
                else if (columns[j].ToUpper() == "EXPIRE")
                {
                    string date = "09/16/2019 12:00:00 AM";
                    DateTime result = DateTime.Parse(tt[j]);
                    mm.expire = result.Ticks;
                }
                else if (columns[j].ToUpper() == "DESCRIPTION")
                {
                    mm.description = tt[j];
                }
                else if (columns[j].ToUpper() == "ICON")
                {
                    mm.icon = ResourceHolder.instance.GetMisc(tt[j]);
                }
                else if (columns[j].ToUpper() == "MARKETID")
                {
                    mm.realID = tt[j];
                }
                else if (columns[j].ToUpper() == "MARKETPRICE")
                {
                    mm.realPrice = float.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "LIMIT")
                {
                    mm.limit = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "FREE_EVERY")
                {
                    mm.freeEvery = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "STAT_NEW")
                {
                    mm.startStatNew = bool.Parse(tt[j]);
                }

            }
            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
        
    }
    
    public void ParseBuildings(string val)
    {
        //NAME	LEVEL	REQ_BUILD	BUILD_TIME	UPGRADE_BUILD	UPDRAGE_TIME	RECIPE_ID	RECIPES	RECIPE_END	RECIPE_TIME	EFFECT

        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        var mm = new FormatBuilding();         
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

           
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    //mm.what = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty && tt[j] != "")
                    {
                        mm = new FormatBuilding(); 
                        mm.bName = tt[j];
                        lastId = mm.bName;
                        //allShop.Add(mm.id, mm);
                    }
                    
                }
                else if (columns[j].ToUpper() == "LEVEL")
                {
                    mm.bLevel = int.Parse(tt[j]);
                    mm.id = mm.bName + tt[j];
                    allBuildings.Add(mm.id, mm);
                }
                else if (columns[j].ToUpper() == "REQ_BUILD")
                {
                    //Debug.Log(tt[j]);
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.reqBuild.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "ACTIONS")
                {
                    mm.actions.Add(tt[j]);
                }
                else if (columns[j].ToUpper() == "BUILD_TIME")
                {
                    mm.buildTm = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "UPGRADE_BUILD")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.updBuild.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "UPGRADE_TIME")
                {
                    mm.updateTm = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "EFFECT")
                {
                    mm.skill = tt[j];
                }
                else if (columns[j].ToUpper() == "REQ_TECH")
                {
                    mm.recipes[mm.recipes.Count - 1].req_tech = tt[j];
                }
                else if (columns[j].ToUpper() == "EXTRA_GAIN")
                {
                    //Debug.Log(tt[j]);
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.recipes[mm.recipes.Count - 1].extraGain.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "RECIPE_ID")
                {
                    FRecipe fr = new FRecipe();
                    fr.id = tt[j];
                    mm.recipes.Add(fr);
                    //mm.recipes
                }
                else if (columns[j].ToUpper() == "RECIPES")
                {
                    var yh = tt[j].Split("#");
                    for (int o = 0; o < yh.Length; o++)
                    {
                        var yp = yh[o].Split(",");  
                        mm.recipes[mm.recipes.Count-1].reqs.Add(new Bon{Key = yp[0], Value = int.Parse(yp[1])});                        
                    }
                }
                else if (columns[j].ToUpper() == "RECIPE_END")
                {
                    var yp = tt[j].Split(",");  
                    mm.recipes[mm.recipes.Count-1].result = new Bon{Key = yp[0], Value = int.Parse(yp[1])};
                }
                else if (columns[j].ToUpper() == "LIMIT")
                {
                    mm.recipes[mm.recipes.Count - 1].limit = int.Parse(tt[j]);
                }
                else if (columns[j].ToUpper() == "RECIPE_TIME")
                {
                    mm.recipes[mm.recipes.Count-1].tm = int.Parse(tt[j]);
                }

            }
            //
            if (lastId == string.Empty) continue;
            //dictDrops[lastId].Add(mm);        
        }
        
    }
    public void ParseDrops(string val)
    {
        //NAME	LOOT TRIGGER
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);

        string lastId = string.Empty; 

        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);

            var mm = new OneLoot();            
            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    mm.what = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        mm.what = tt[j];
                        lastId = mm.what;
                        dictDrops.Add(mm.what, new List<OneLoot>());
                    }
                    
                    mm.what = tt[j];
                }
                else if (columns[j].ToUpper() == "LOOT")
                {
                    mm.lootSet = tt[j];
                }
                else if (columns[j].ToUpper() == "TRIGGER")
                {
                    mm.lootCond = tt[j];
                }

            }
            //
            if (lastId == string.Empty) continue;
            dictDrops[lastId].Add(mm);        
        }
        
    }
    
    public void ParseCutscenes(string val)
    {
        //NAME	PHRASE	ACTION	AVA1	AVA2	TAVA1	TAVA2
        var str = val.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        var columns = str[0].Split("\t", StringSplitOptions.RemoveEmptyEntries);
        Trymo(columns);


        string lastID = string.Empty;
        for (int i = 1; i < str.Length; i++)
        {
            var tt = str[i].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            
            var mm = new FormatCutscene();            

            for (int j = 0; j < tt.Length; j++)
            {
                if (tt[j] == "x")
                {
                    mm.id = "x";
                    continue;
                }
                
                if (columns[j].ToUpper() == "NAME")
                {
                    if (tt[j] != String.Empty)
                    {
                        lastID = tt[j];
                        mm.id = tt[j];
                        dictCutscenes.Add(lastID, new List<FormatCutscene>());
                    }
                    
                    mm.id = tt[j];
                }
                else if (columns[j].ToUpper() == "T0")
                {
                    mm.t0 = float.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "T1")
                {
                    mm.t1 = float.Parse(tt[j], CultureInfo.InvariantCulture);
                }
                else if (columns[j].ToUpper() == "ACTION")
                {
                    mm.action = tt[j];
                }
                else if (columns[j].ToUpper() == "AFTER")
                {
                    mm.afterA = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAL")
                {
                    mm.parallelA = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAL1")
                {
                    mm.parallelA2 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAL2")
                {
                    mm.parallelA3 = tt[j];
                }
                else if (columns[j].ToUpper() == "PODID")
                {
                    mm.podid = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM1")
                {
                    mm.param1 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM2")
                {
                    mm.param2 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM3")
                {
                    mm.param3 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM4")
                {
                    mm.param4 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM5")
                {
                    mm.param5 = tt[j];
                }
                else if (columns[j].ToUpper() == "PARAM6")
                {
                    mm.param6 = tt[j];
                }

            }
            //
            if (lastID == string.Empty) continue;
            dictCutscenes[lastID].Add(mm);
        }
        
        //dictCutscenes.Add(mm.id, mm);      
        
    }


    private string locale = "EN";

    private List<UnoLoc> _locs = new List<UnoLoc>();
    public string GetMeLocale(string phrase, UnoLoc ul = null)
    {
        phrase = phrase.ToLower();

        if (!doctLoc.ContainsKey(phrase))
        {
            
            var bo = phrase.Replace("_descr", "");
            if (allDynamic.ContainsKey(bo))
            {
                return MainStates.instance.GetGeneratedDynamicDescr(bo);
            }

            Obj cc = null;
            
            DatabaseAll.instance.items.TryGetValue(phrase, out cc);
            
            if (cc == null) return "no_descr: " + phrase;
            
            if (cc.skills == null || cc.skills.Count == 0) return "no_descr_SKL: " + phrase;
            
            phrase = cc.skills[0];
        }
        
        if (ul != null)
            _locs.Add(ul);
        
        if (locale == "EN")
            return doctLoc[phrase].EN;
        else if (locale == "RUS")
            return doctLoc[phrase].RUS;
        else if (locale == "CHN_T")
            return doctLoc[phrase].CHN_T;
        else if (locale == "CHN_S")
            return doctLoc[phrase].CHN_S;
        else if (locale == "JAP")
            return doctLoc[phrase].JAP;
        else if (locale == "UKR")
            return doctLoc[phrase].UKR;
        else if (locale == "KOR")
            return doctLoc[phrase].KOR;

        return doctLoc[phrase].JAP;
    }

    public void SwitchToLocale(string newLoc)
    {
        if (newLoc != locale)
        {
            locale = newLoc;
            foreach (var v in _locs)
            {
                v.GetComponent<TextMeshProUGUI>().text = GetMeLocale(v.loco);
            }
        }
    }
}

[System.Serializable]
public class FormatHero
{
    public string monsterName = string.Empty;
    public string displayName = string.Empty;
    public List<string> origins = new List<string>();
    public List<string> classes = new List<string>();
    public float health = 0;
    public float maxHealth = 0;
    public float mana = 0;
    public float maxMana = 0;
    public float armor = 0;
    public float magicResist = 0;
    public float attack = 0;
    public float attackSpeed = 0;
    
    public float speed = 3;

    public int size = 11;
    
    public float dodge = 0;
    public float accuracy = 0;
    public float apoints = 0;
    public int noMove = 0;
    public int doNothing = 0;
    public int passable = 0;
    public int building = 0;
    public int dropPick = 0;
    
    public int immortal = 0;
    
    
    public int rarity = 0;
    public int cost = 0;
    public int level = 1;
    public string skillUltimate = string.Empty;
    public string skillBasic = string.Empty;
    public List<string> skillOthers = new List<string>();
    public List<string> skillTraits = new List<string>();
    public List<string> skillInst = new List<string>();
    public string drop = "";
    public string onDeath = "";
    public string onDmg = "";
    public string dropPerHit = "";
    
    
    public float critChance = 0;
    public float critDamage = 0;
    public float maxDmgTaken = -1;
    public float dmgBlock = 0;
    
    public float lifestealPrc = 0;
    public float regen = 0;
    
    
    public int stars = 1;
    public string description = string.Empty;

    public string role = string.Empty;
    public string formation = string.Empty;

    public ElementType element = ElementType.physical;

    public string sklTree = "s0";
    public int move = 1;
    public int countAsUnit = 1;
    public int initiative = 1;
    public int aggroRange = 6;
    public int price = 3;
    public string dynamic = "";

    public float GetMeStatVal(string parName)
    {
        //"p_atk","p_def","m_def","max_health","atk_spd","mana","max_mana"
        if (parName == "p_atk") return attack;
        if (parName == "m_atk") return 1f;
        if (parName == "p_def") return armor;
        if (parName == "mana") return mana;
        if (parName == "max_mana") return maxMana;
        if (parName == "atk_spd") return attackSpeed;
        if (parName == "m_def") return magicResist;
        if (parName == "crit_rate_prc") return 0f;
        if (parName == "crit_dmg_prc") return 100f;
        if (parName == "range") return 0f;

        return 0;
    }
}


[System.Serializable]
public class FormatStages
{
    public string nm = string.Empty;
    public List<string> normalBattles = new List<string>();
    public List<string> eliteBattles = new List<string>();
    public List<string> bossBattles = new List<string>();
    public List<Bon> rewards = new List<Bon>();
}

public class FormatArtefact_old
{
    public string artefactName = string.Empty;
    public string skill = string.Empty;
    public string skill2 = string.Empty;
    public string skillParam = string.Empty;

    public string skillRare = string.Empty;
    public string skillEpic = string.Empty;
    public string skillLegend = string.Empty;
    public string skillMythic = string.Empty;
    
    
    public string description = string.Empty;
    public ItemSlot slot = ItemSlot.weapon;
    public RarityType rarity = RarityType.common;
    public int dimension = 11;
    public int price;

    public string next = string.Empty;
}

public class FormatArtefact
{
    public string skillName = string.Empty;
    public float ATTACK_PRC;
    public float ATTACK;
    public float HEALTH;
    public float MAX_HEALTH;
    public float DEF;
    public float RES;
    public float HEALTH_PRC;
    public float MAX_HEALTH_PRC;
    public float DEF_PRC;
    public float RES_PRC;
    public float MANA;
    public float MAX_MANA;
    public float SPEED;
    public string TAG_APPLY = "enemy";
    public string REF_SKILL = "";
    public float PEN_CNT;
    public float INSTANT = 1;
    
    public string SLOT = "";
    public string RARITY = "";

    public int size = 11;
    public List<Bon> price = new List<Bon>();

}


public class FormatLocalization
{
    public string id = string.Empty;
    public string EN = string.Empty;
    public string UKR = string.Empty;
    public string RUS = string.Empty;
    public string CHN_T = string.Empty;
    public string CHN_S = string.Empty;
    public string JAP = string.Empty;
    public string KOR = string.Empty;
}

public class FormatBonusOrigins
{
    public string bonusName = string.Empty;
    public string skill = string.Empty;
    public List<KeyValuePair<string, int>> conditions = new List<KeyValuePair<string, int>>();
    public string description = string.Empty;
    public string falseLoc = string.Empty;
    public int render = 1;
    public int kindex = 0;
    public int tower = 0;
}

[System.Serializable]
public class FormatBattles
{
    //NAME	HERO-LEVEL-POSITION	HERO-ARTEFACT	HERO-KINGDOM		ENEMY-LEVEL-POSITION	ENEMY-ARTEFACT	ENEMY-KINGDOM
    public string battleName = string.Empty;

    public UnoTeam heroes = new UnoTeam();
    public UnoTeam enemies = new UnoTeam();

    public Vector2 mapPosition = Vector2.zero;

    public string levelRef = string.Empty;
    public int bgNum = 0;
    public int complexity = 0;

    public List<Bon> firstReward = new List<Bon>();
    public string posReward = "";
    public List<Bon> star3Reward = new List<Bon>();
    public List<UnoReq> reqStart = new List<UnoReq>();
}

[System.Serializable]
public class UnoTeam
{
    public List<Tuple<string, int, Vector2, int>> heroLevelPosition = new List<Tuple<string, int, Vector2, int>>();
    public List<List<string>> heroArtefacts = new List<List<string>>();
    public List<List<int>> heroKingdomBonus = new List<List<int>>();  
    public List<List<string>> heroPerks = new List<List<string>>();
    public List<string> heroSkillTree = new List<string>();
    public List<float> timeSpawns = new List<float>();
    public List<int> sides = new List<int>();
    public List<int> amounts = new List<int>();
}

public class FormatSkill_old
{
    public string skillName = string.Empty;
    public string skillDisplayName = string.Empty;
    public string skillJson = string.Empty;
    public string skillType = string.Empty;
    public string description = string.Empty;
    public string shortDescription = string.Empty;
    public string ava = String.Empty;
    public int manaCost = 0;
    public int tier = 0;
}

public class FormatSkill
{
    public string skillName = string.Empty;
    public float ATTACK_PRC;
    public float ATTACK;
    public float HEALTH;
    public float MAX_HEALTH;
    public float DEF;
    public float RES;
    public float HEALTH_PRC;
    public float MAX_HEALTH_PRC;
    public float DEF_PRC;
    public float RES_PRC;
    public float MANA;
    public float MAX_MANA;
    public float SPEED;
    public string TAG_APPLY = "enemy";
    public float PEN_CNT;
    public float CRIT_CHANCE = 0;
    public float CRIT_DMG = 0;
    public int RARITY = 0;

    public float dodge = 0;
    public float dodge_prc = 0;
    public float accuracy = 0;
    public int manaCost = 0;
    public int ricochet = 0;
    public int bounce = 0;
    
    public float maxDmgTaken = -1;
    public float dmgBlock = 0;
    
    public int apoints = 0;
    public int immortal = 0;
    public int unique = 0;
    
    public float ANGLE;
    public float LIFESTEAL_PRC;
    public float REGEN;
    
    
    public float DT;
    
    public float INSTANT = 1;
    public float COOLDOWN = 1;
    public float RANGE = 1;
    //filters

    public string FILTER_HP = "any";
    public string FILTER_ATK = "any";
    public string FILTER_RANGE = "closest";
    public int FILTER_SELF = 0;
    public int targets = 1;

    public int ACTION_REQ = 0;
    public int EMPTY_REQ = 0;
    
    public int MANA_REQ = 0;
    public int SHIELD = 0;
    public int FIRST = 0;
    public int req2 = 0;

    public string SECOND = "";
    
    public List<Bon> PARS = new List<Bon>();
    public float aoe = 0;
    public int amount = 1;
    public int travel = 0;
    public List<string> affected = new List<string>();
    
    public List<string> buffApply = new List<string>();
    public float cdReduction = 0;
    public float time = -1;
    public float dmgEvery = 0;

    public string onDeath = "";
    public string onDmg = "";
    public string spawn = "";
    

}


[System.Serializable]
public class FormatMeta
{
    public string parName = string.Empty;
    public float val = 0;
    public string stringVal = string.Empty;
}

[System.Serializable]
public class FormatPlayer
{
    public string nm = "";
    public string common = "";
    public string inven = "";
    
    public List<Bon> items = new List<Bon>();
    public List<Bon> stats = new List<Bon>();
    public List<string> dynTaken = new List<string>();
}

[System.Serializable]
public class FormatLevel1
{
    public string id = string.Empty;
    [TextArea(10,20)]
    public string obstacles = string.Empty;
    [TextArea(10,20)]
    public string adds = string.Empty;
    
}

[System.Serializable]
public class FormatLevel
{
    public string id = "";
    public int n, m;
    public List<LevelPars[]> map = new();

}

[System.Serializable]
public class LevelPars
{
    public List<string> pars = new List<string>();
}

[System.Serializable]
public class FormatExtraEffect
{
    public string id = string.Empty;
    public List<string> artConditions = new List<string>();
    public string endEffect = string.Empty;
    
}

[System.Serializable]
public class FormatMail
{
    public string id = string.Empty;
    public List<Bon> rewards = new List<Bon>();
    public long startDate;
    public long endDate;
    public string header;
    public string description;
}

[System.Serializable]
public class FormatDialogue
{
    public string id = string.Empty;
    public string phrase = string.Empty;
    public string action = string.Empty;
    public string ava1 = string.Empty;
    public string ava2 = string.Empty;
    public string avaT1 = string.Empty;
    public string avaT2 = string.Empty;
    public string speaker = string.Empty;
    public string cond = string.Empty;
    public string cStart = string.Empty;
    
    public string req_trigger = string.Empty;
    public string req_show = string.Empty;
}

[System.Serializable]
public class FormatBuilding
{
    public string id = string.Empty;
    
    public string bName = "";
    public int bLevel = 1;

    public string skill = "";

    public List<Bon> reqBuild = new List<Bon>();
    public int buildTm = 0;
    
    public List<Bon> updBuild = new List<Bon>();
    public int updateTm = 0;

    //receipres
    public List<FRecipe> recipes = new List<FRecipe>();

    public List<string> actions = new List<string>();
}

[System.Serializable]
public class FormatDynamic
{
    public string id = string.Empty;

    public List<Bon> price = new List<Bon>();
    public List<Bon1> conds1 = new List<Bon1>();
    
    public List<Bon> statsInc = new List<Bon>();
    public List<Bon> statsExact = new List<Bon>();
    public List<Bon> itemsGet = new List<Bon>();
    public List<Bon> parUpgrade = new List<Bon>();
    public List<Bon> parentUpgrade = new List<Bon>();
    
    public List<string> objActivate = new List<string>();
    public List<string> skillUnlock = new List<string>();
    
    public List<Bon> unitsBuild = new List<Bon>();
    public List<string> skillPasUnlock = new List<string>();
    public string eventTrigger = string.Empty;
    public string eventVal = string.Empty;
    public int eventNum = 0;
    
    
    public string subscribe = string.Empty;
    public string create = string.Empty;
    public string reward = string.Empty;
    public string call = string.Empty;
    public string param = string.Empty;
    public int filterNum = -1;
    public float time = -1;
    
    public List<string> dynList = new List<string>();
    public List<string> condList = new List<string>();
    
    public List<GameObject> toActivate = new List<GameObject>();
    public List<GameObject> toDeactivate = new  List<GameObject>();

    public string dialog = string.Empty;
    public string cutscene = string.Empty;

    public int multi = 0;

}

[System.Serializable]
public class FRecipe
{
    public string id = "";
    public List<Bon> reqs = new List<Bon>();
    public Bon result = new Bon();
    public float tm = 0;
    public int limit = -1;
    public string req_tech = "";
    public List<Bon> extraGain = new List<Bon>();
}


[System.Serializable]
public class FormatCutscene
{
    public string id = string.Empty;
    public string action = string.Empty;
    public float t0 = 0;
    public float t1 = 0;

    public string podid = string.Empty;
    
    public string param1 = string.Empty;
    public string param2 = string.Empty;
    public string param3 = string.Empty;
    public string param4 = string.Empty;
    public string param5 = string.Empty;
    public string param6 = string.Empty;

    public string afterA = string.Empty;
    public string parallelA = string.Empty;
    public string parallelA2 = string.Empty;
    public string parallelA3 = string.Empty;
}

[System.Serializable]
public class FormatSkilltree
{
    public string nm = "";
    public List<SkillCond> skills = new List<SkillCond>();
}

[System.Serializable]
public class SkillCond
{
    public string sklName = "";
    public string treeId = "";
    public List<Bon> price = new List<Bon>();
    public List<Bon> conds = new List<Bon>();
    public int level = 0;
}