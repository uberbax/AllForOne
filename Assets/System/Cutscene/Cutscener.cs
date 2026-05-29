using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Unity.VisualScripting;

public class Cutscener : MonoBehaviour
{
    public static Cutscener instance;
    public GameObject bark;
    public List<StrGo> insts = new List<StrGo>();
    public List<StrCurv> curvesX = new List<StrCurv>();
    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        instance = null;
    }
    public void DoAnimation(string who, string anim, bool loop, GameObject over = null, Action act = null)
    {
        if (anim != string.Empty)
        {
            GameObject go = null;
            if (over != null) go = over;
            else go = Find(who);

            var a = anim.Split('#');
            anim = a[0];
            if (a.Length > 1) loop = true;
            
            var ta = go.GetComponent<Animator>();

            if (ta != null && ta.enabled)
            {
                ta.CrossFadeInFixedTime(anim, 0.1f);
            }
            else
            {
                //var fa = go.GetComponent<SkeletonAnimation>();
                //fa.AnimationState.SetAnimation(0, anim, loop);
            }
        }

        if (act != null)
            act();
    }
    
    public GameObject GetAllObjectsInScene(string who)
    {
        List<GameObject> objectsInScene = new List<GameObject>();
  
        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (go.hideFlags != HideFlags.None)
                continue;

            if (go.name == who)
                return go;

            //if (PrefabUtility.GetPrefabType(go) == PrefabType.Prefab || PrefabUtility.GetPrefabType(go) == PrefabType.ModelPrefab)
            //    continue;

            //objectsInScene.Add(go);
        }
        return null;
    }

    public void Move(string who, string from, string where, float tm, string animation, string animEnd, Action act = null)
    {
        var go = Find(who);

        GameObject p1 = go;
        if (from != "x" && from != "")
            p1 = Find(from);
        
        var gg = where.Split(',');
        List<GameObject> p2 = new List<GameObject>();
        for (int i = 0; i < gg.Length; i++)
            p2.Add(Find(gg[i]));
        
        //utils control
        DoAnimation(who, animation, false, go);

        //calculate spd
        float ll = (p2[0].transform.position - p1.transform.position).magnitude;
        for (int i = 1; i < p2.Count; i++)
            ll += (p2[i].transform.position - p2[i-1].transform.position).magnitude;

        float speed = ll / tm;
        if (speed == 0) speed = 100;
        
        RecMove(go.transform, p2, 0, speed, act, Vector3.zero/*p2[0].transform.position - go.transform.position*/,animEnd);
        
    }

    void RecMove(Transform who, List<GameObject> wha, int cur, float speed, Action endAct, Vector3 sdvig, string endAnim)
    {
        if (cur >= wha.Count)
        {
            if (endAnim != "" && endAnim != "x")
            {
                DoAnimation(who.name, endAnim, true);
            }
            
            if (endAct != null) endAct();
            return;
        }

        Debug.Log("MOVA: " + who.name + " " + wha[cur].name);
        UtilsControl.Instance.MoveTo(who.transform, speed, wha[cur].transform.position + sdvig, () =>
        {
            RecMove(who, wha, cur + 1, speed, endAct, sdvig, endAnim);
        }, null /*wha[cur].transform*/, z0: 0, lalk:wha[cur].name);

    }

    public void ShowEmoji(string who, string emoji, Action after = null)
    {
        var go = Find(who);

        PopupEmote f = go.GetComponent<PopupEmote>();
        if (f == null) f = go.AddComponent<PopupEmote>();
        
        if (emoji != String.Empty)
            f.ShowEmote(emoji);
        else
            f.CloseEmote();

        if (after != null)
            after();
    }
    
    public void Parent(string who, string whom, Action after = null)
    {
        var go = Find(who);

        if (whom == "false")
        {
            go.transform.parent = null;
        }
        else
        {
            var go1 = Find(whom);
            go.transform.parent = go1.transform;
        }

        if (after != null)
            after();
    }

    public void Wait(float tm, Action act)
    {
        if (act != null)
        {
            FunctionTimer.Create(act, tm);
        }
    }
    
    public void Layer(string who, int lr,  Action after)
    {
        var go = Find(who);

        go.GetComponent<Renderer>().sortingOrder = lr;
        
        if (after != null)
            after();
    }
    
    public void Movez(string who, string from, string where, float tm)
    {
        //move, keep Z
        var go = Find(who);
        
        var p1 = Find(from);
        var p2 = Find(where);
        
        //utils control
        
        
    }

    public void Create(string who, string what, Action act)
    {
        var go = Find(who);
        var ee = insts.Find(x => x.id == what);

        var yy = Instantiate(ee.go);
        yy.transform.position = go.transform.position;

        if (act != null)
            act();
    }
    
    public void Flip(string who, Action act)
    {
        var go = Find(who);
        var sc = go.transform.localScale;
        go.transform.localScale = new Vector3(-sc.x, sc.y, sc.z);

        if (act != null)
            act();
    }

    public void Activate(string who, string val, Action act)
    {
        var go = Find(who);
        
        Debug.Log("TO ACTIVATE: " + go.name + " " + val);

        if (val == "false" || val == "FALSE")
        {
            go.SetActive(false);
            if (go.transform.childCount > 0)
                go.transform.GetChild(0).gameObject.SetActive(false);
            if (go.GetComponent<SpriteRenderer>() != null) go.GetComponent<SpriteRenderer>().enabled = false;
        }
        else
        {
            go.SetActive(true);
            if (go.transform.childCount > 0)
                go.transform.GetChild(0).gameObject.SetActive(true);
            if (go.GetComponent<SpriteRenderer>() != null) go.GetComponent<SpriteRenderer>().enabled = true;
        }

        if (act != null)
            act();

    }

    public void Dod(string who, string val)
    {
        var go = Find(who);
        //ITS FOR PixelFantasy/PixelMonsters
        
        /*
        var ff = go.GetComponent<MonsterControls>();

        if (val == "false")
            ff.dod = false;
        else
            ff.dod = true;
            */
    }
    
    public void Bark(string who, string val, Action act)
    {
        var go = Find(who);

        //MiscSettings
        MiscSettings tt = go.GetComponent<MiscSettings>();

        var g = go.GetComponentInChildren<Typewritter>();

        if ((val == ""||val == "x") && g != null)
        {
            Destroy(g.gameObject);
            return;
        }
            
        
        if (g == null)
        {
            var c = GameObject.Instantiate(bark);
            c.transform.parent = go.transform;
            c.transform.localPosition = Vector3.zero;
            
            c.GetComponentInChildren<Typewritter>().SetText(val);

            //if (tt != null) c.transform.position = tt.bubblePos.position;
        }
        else
        {
            g.SetText(val);
        }

        if (act != null)
            act();
    }
    
    public void Floato(string who, string val, string curv, Action act)
    {
        var go = Find(who);

        var tt = go.GetComponent<MiscSettings>();

        Transform where = go.transform;
        if (tt != null)
            where = tt.bubblePos;


        var jj = insts.Find(x => x.id == val).go;
        var gh = GameObject.Instantiate(jj);
        gh.transform.position = where.position;

        var cc = curvesX.Find(x => x.id == curv);

        StartCoroutine(UtilsControl.Instance.FadeNUp(gh.transform, 2, 0.2f, cc == null ? null : cc.go, null));

        if (act != null)
            act();
    }

    public GameObject Find(string who)
    {
        var ff  = GameObject.Find(who);
        if (ff == null)
            ff = GetAllObjectsInScene(who);
        return ff;
    }

    public void SmoothFollow(string who, string whom, bool look)
    {
        var go1 = Find(who);

        if (whom == "false")
        {
            go1.GetComponent<CameraFollow>().target = null;
            return;
        }
        
        var go2 = Find(whom);

        go1.GetComponent<CameraFollow>().target = go2.transform;
        go1.GetComponent<CameraFollow>().look = look;
    }

    public void Shake()
    {
        var go = Find("MainCamera");
        go.GetComponent<CameraShakeSimpleScript>().ShakeCamera();
    }

    public void Custom(string who, string what)
    {
        var go = Find(who);
        go.SendMessage(what);
    }
    
    public void Zoom(float val, float spd, Action act)
    {
        var go = Find("MainCamera");
        StartCoroutine(Zoom1(go, val, spd, act));
    }

    public IEnumerator Zoom1(GameObject go, float val, float spd, Action act)
    {
        var vv = go.GetComponent<Camera>();
        if (spd < 0)
        {
            while (vv.fieldOfView > val)
            {
                vv.fieldOfView += spd * Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            while (vv.fieldOfView < val)
            {
                vv.fieldOfView += spd * Time.deltaTime;
                yield return null;
            }
        }

        vv.fieldOfView = val;

        if (act != null)
            act();
    }
    

    private float tStart;
    public void ExecuteCutscene(string id)
    {
        Debug.Log("Starting execute " + id);
        tStart = Time.time;

        var scn = ConfigLoader.Instance.dictCutscenes[id];
        //clone ?
        foreach (var v in scn)
        {
            var tmp = v;
            if (v.t0 >= 0)
            {
                FunctionTimer.Create(() => ExecuteSingle(tmp, scn), v.t0 );
            }
        }
    }
    
    

    public void ExecuteSingle(FormatCutscene v, List<FormatCutscene> all)
    {
        Debug.Log("Starting SUBexecute " + v.id + " " + v.podid);
        
        //after action ?
        Action after = null;
        if (v.afterA != string.Empty && v.afterA != "x")
        {
            var d = all.Find(x => x.podid == v.afterA);
            after = () => ExecuteSingle(d, all);
        }
        
        Action paral = null;
        if (v.parallelA != string.Empty && v.parallelA != "x")
        {
            var d = all.Find(x => x.podid == v.parallelA);
            paral = () => ExecuteSingle(d, all);
        }
        Action paral2 = null;
        if (v.parallelA2 != string.Empty && v.parallelA2 != "x")
        {
            var d = all.Find(x => x.podid == v.parallelA2);
            paral2 = () => ExecuteSingle(d, all);
        }
        Action paral3 = null;
        if (v.parallelA3 != string.Empty && v.parallelA3 != "x")
        {
            var d = all.Find(x => x.podid == v.parallelA3);
            paral3 = () => ExecuteSingle(d, all);
        }
        
        if (v.action == "move")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Move(v.param1, v.param2, v.param3, float.Parse(v.param4, CultureInfo.InvariantCulture), v.param5, v.param6,after), v.t1);
            else
                Move(v.param1, v.param2, v.param3, float.Parse(v.param4, CultureInfo.InvariantCulture), v.param5, v.param6, after);
        }
        else if (v.action == "emoji")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => ShowEmoji(v.param1, v.param2, after), v.t1);
            else
                ShowEmoji(v.param1, v.param2, after);
        }
        else if (v.action == "dod")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Dod(v.param1, v.param2), v.t1);
            else
                Dod(v.param1, v.param2);
        }
        else if (v.action == "zoom")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Zoom(float.Parse(v.param1), float.Parse(v.param2), after), v.t1);
            else
                Zoom(float.Parse(v.param1), float.Parse(v.param2), after);
        }
        else if (v.action == "wait")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Wait(float.Parse(v.param1), after), v.t1);
            else
                Wait(float.Parse(v.param1), after);
        }
        else if (v.action == "layer")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Layer(v.param1, int.Parse(v.param2), after), v.t1);
            else
                Layer(v.param1, int.Parse(v.param2), after);
        }
        else if (v.action == "anim")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(()=>DoAnimation(v.param1, v.param2, false, act: after), v.t1 );
            else
                DoAnimation(v.param1, v.param2, false, act: after);
        }
        else if (v.action == "parent")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(()=>Parent(v.param1, v.param2,  after), v.t1 );
            else
                Parent(v.param1, v.param2, after);
        }
        else if (v.action == "dialog")
        {
            if (v.t1 > 0)
                FunctionTimer.Create( ()=> Dialoguer.instance.ShowDialogue(v.param1), v.t1);
            else
                Dialoguer.instance.ShowDialogue(v.param1);
        }
        
        else if (v.action == "flip")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Flip(v.param1, after), v.t1);
            else
                Flip(v.param1, after);    
        }
        else if (v.action == "activate")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Activate(v.param1, v.param2, after), v.t1);
            else
                Activate(v.param1, v.param2, after);
        }
        else if (v.action == "cutscene")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => ExecuteCutscene(v.param1), v.t1);
            else
                ExecuteCutscene(v.param1);
        }
        else if (v.action == "bark")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Bark(v.param1, v.param2, after), v.t1);
            else
                Bark(v.param1, v.param2, after);
        }
        else if (v.action == "float")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Floato(v.param1, v.param2, v.param3, after), v.t1);
            else
                Floato(v.param1, v.param2, v.param3, after);
        }
        else if (v.action == "smooth")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => SmoothFollow(v.param1, v.param2, true), v.t1);
            else
                SmoothFollow(v.param1, v.param2, true);
        }
        else if (v.action == "smoothz")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => SmoothFollow(v.param1, v.param2, false), v.t1);
            else
                SmoothFollow(v.param1, v.param2, false);
        }
        else if (v.action == "create")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Create(v.param1, v.param2, after), v.t1);
            else
                Create(v.param1, v.param2, after);
        }
        else if (v.action == "shake")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Shake(), v.t1);
            else
                Shake();
        }
        else if (v.action == "custom")
        {
            if (v.t1 > 0)
                FunctionTimer.Create(() => Custom(v.param1, v.param2), v.t1);
            else
                Custom(v.param1, v.param2);
        }

        if (paral != null)
        {
            paral();
        }
        
        if (paral2 != null)
        {
            paral2();
        }
        
        if (paral3 != null)
        {
            paral3();
        }
    }
    
    
    //
    void Update()
    {

    }
    
    
}

[System.Serializable]
public class StrGo
{
    public string id;
    public GameObject go;
}

[System.Serializable]
public class StrCurv
{
    public string id;
    public AnimationCurve go;
}