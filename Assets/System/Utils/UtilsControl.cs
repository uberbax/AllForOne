using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Linq;
using DamageNumbersPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using Object = System.Object;
using Random = System.Random;

public class UtilsControl : MonoBehaviour
{
    public DamageNumber prefab;
    public DamageNumber prefabPos;
    public DamageNumber prefabPhrase;
    
    public static UtilsControl Instance;
    public GameObject imgTxt;
    public Material chainMat;
    public GameObject anchor;
    
    [Header("Some Sounds")] 
    public AudioClip chestCrash;
    public AudioClip coinDrop;
    public AudioClip gemDrop;
    public Material fillSpriteMat;
    

    public GameObject arrow;
    public Color[] rarColors;
    public Sprite[] gemRars;
    public Sprite[] cardRars;
    public Sprite[] cardFrameRars;
    public Sprite[] classBar;

    public AnimationCurve moveCurve;
    public GameObject emptyImg;
    public GameObject shadowBlob;
    
    public AnimationCurve parabolicCurve;
    
    public AnimationCurve scaleFlyCurve;
    public AnimationCurve scaleFlyCurveInBattle;

    public Camera uiCam;

    public Sprite[] digits;
    
    public Camera GetUICam()
    {
        if (uiCam != null) return uiCam;
        else
        {
            return Camera.main;
        }
    }
    private void Awake()
    {
        Instance = this;
    }
    
    private void OnDestroy()
    {
        Instance = null;
    }

    private GameObject angl;
    public GameObject anglArrow;

    public GameObject angl2;

    public void Teleport(GameObject go, Action act, float spd = 1)
    {
        StartCoroutine(TeleportA(go, act, spd));
    }

    public IEnumerator TeleportA(GameObject go, Action act, float spd = 1)
    {
        if (go.GetComponent<_2dxFX_NewTeleportation>() == null)
            go.AddComponent<_2dxFX_NewTeleportation>();

        var f1 = go.GetComponent<_2dxFX_NewTeleportation>();

        if (spd > 0)
        {
            f1._Fade = 0;
            while (f1._Fade < 1)
            {
                f1._Fade += Time.deltaTime * spd;
                yield return null;
            }
        }
        else
        {
            f1._Fade = 1;
            while (f1._Fade > 0)
            {
                f1._Fade += Time.deltaTime * spd;
                yield return null;
            }
        }
        
                 yield return null;

        if (act != null) act();
    }
    
    
    public void AngleEachFrame(GameObject who, Vector3 pos)
    {
        if (angl == null)
            angl = Instantiate(anglArrow);

        angl.transform.position = who.transform.position;
        
        var yy = pos - who.transform.position;
        yy.z = 0;
        
        angl.transform.forward = yy;
    }

    public Sprite GetClassSprite(RObj m)
    {
        return classBar[
            m.labels.Contains("Tank") ? 0 : 
            m.labels.Contains("Killer") ? 1 : 2];
    }

    public AnimationCurve tintCurve = AnimationCurve.Linear(0,0,1,1);
    public void UntintWhite(GameObject go, float tm)
    {
        StartCoroutine(UntintWhiteA(go, tm));
    }

    public Sprite completedStar;
    public Sprite uncompletedStar;

    public void FillMeStars(Transform root, int val)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            if (i < val)
                root.GetChild(i).GetComponent<Image>().sprite = completedStar;
            else
                root.GetChild(i).GetComponent<Image>().sprite = uncompletedStar;
        }
    }

    public GameObject smallItem;

    public GameObject bibuff;
    public GameObject floorBlood;
    public GameObject deathEffect;

    public GameObject flyUItext;
    public GameObject flyText;
    
    public void FlyText3D(Vector3 where, string txt, Color clr, Vector3 dlt)
    {
        var ff = Instantiate(flyText);
        ff.transform.position = where + dlt;
        ff.SetActive(true);

        var fg = ff.GetComponentInChildren<TextMeshPro>();
        fg.color = clr;
        fg.text = txt;        
    }

    public void LargeSmall(Transform who, Action act)
    {
        ApplyCurve(who, AnimationCurve.Linear(0,1,1,1.2f), CurveType.Scale, act, 0.2f, 5, 1f, 0, Color.white, pong:true, repCount:1);
    }
    public GameObject FlyTextUI(Transform where, string txt, Color clr, Sprite spr = null, string itm = "", bool inFinale = true, float dltY = 0, bool doCam = false)
    {
        Debug.Log("FLYED --- " + txt);
        
        var root = GameObject.FindGameObjectWithTag("Finale").transform;
        if (!inFinale)
            root = where;
        
        var ff = Instantiate(flyUItext, root);
        ff.transform.position = where.position + new Vector3(0, dltY, 0);

        if (doCam)
        {
            //Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, where.position);
            //ff.GetComponent<RectTransform>().anchoredPosition = screenPoint - root.GetComponent<RectTransform>().sizeDelta / 2f;
            
            
            //Vector2 pos = where.position;  // get the game object position
            Vector2 viewportPoint = Camera.main.WorldToViewportPoint(where.position);  //convert game object position to VievportPoint
            var rr = ff.GetComponent<RectTransform>();
            
            rr.anchorMin = viewportPoint;  
            rr.anchorMax = viewportPoint;
            rr.anchoredPosition = Vector3.zero+ new Vector3(0, dltY, 0);
            
        }
            
        
        ff.SetActive(true);
        ff.transform.SetAsLastSibling();

        var fg = ff.GetComponentInChildren<TextMeshProUGUI>();
        fg.color = clr;
        fg.text = txt;

        if (spr == null && itm == "")
            fg.alignment = TextAlignmentOptions.Center;
        else
        {
            fg.transform.GetChild(0).gameObject.SetActive(true);
            if (itm != "")
            {
                //var k = DatabaseItems.Instance.allItems.Find(x => x.name == itm);
                fg.transform.GetChild(0).GetComponent<Image>().sprite = ResourceHolder.instance.GetAva(itm);
            }
            else
                fg.transform.GetChild(0).GetComponent<Image>().sprite = spr;
        }

        return ff;
    }
    
    
    
    public void FlyRewards(List<Bon> rewards, GameObject root, Transform where, Transform container)
    {
        Transform holder = root.transform;
        //clear all
        for (int i = 0; i < rewards.Count; i++)
        {
            var go = GameObject.Instantiate(smallItem, holder).GetComponent<GBind>();

            //var cc = DatabaseItems.Instance.allItems.Find(x => x.name == rewards[i].Key);
            var cc = ResourceHolder.instance.items[rewards[i].Key];
            
            go.name = "ded";
            go.GetImage("icon").sprite = (Sprite)cc;
            go.GetText("amount").text = rewards[i].Value.ToString();
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 300);
            
            //container = HealthSystem.instance.transform
            ParabolikTravel(go.transform, where, null, container, 
                2, null, false, false, true, scaleFlyCurve, true);
        }
    }
    
    public IEnumerator UntintWhiteA(GameObject go, float tm)
    {
        float t = 0;
        var gg = go.GetComponentInChildren<Renderer>();
        gg.material.color = new Color(1, 1, 1, 0);
        while (t < tm)
        {
            yield return null;
            t += Time.deltaTime;
            if (gg == null) break;
            gg.material.color = new Color(1, 1, 1, t/tm);
        }
        yield return null;
    }

    public GameObject CreateArrow(Vector3 pos1, Vector3 pos2, bool followMouse, Action<Vector3> onReleased, float scale, Action<Vector3> eachMa)
    {
        var go = (GameObject)Instantiate(arrow);
        go.GetComponent<ArrowRenderer>().upwards = new Vector3(0, 0, 1);

        var c0 = GetUICam();

        go.transform.localScale = new Vector3(scale, scale, scale);
        go.GetComponent<ArrowRenderer>().SetPositions(c0.ScreenToWorldPoint(pos1), c0.ScreenToWorldPoint(pos2));

        if (followMouse)
        {
            StartCoroutine(ArrowFollow(go.GetComponent<ArrowRenderer>(), pos1, onReleased, eachMa));
        }

        return go;
    }

    public void SetLayerAllChildren(Transform root, int layer)
    {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children)
        {
//            Debug.Log(child.name);
            child.gameObject.layer = layer;
        }
    }

    private GameObject lastRend;
    public void PlaceItemInRender(string nm, float scl = -1)
    {
        var rr = GameObject.FindGameObjectWithTag("RendCamera");
        DestroyImmediate(lastRend);
        Debug.Log(nm);
        var ll = ResourceHolder.instance.monsters[nm.ToLower()];  //DungeonLegend.instance.monsters.Find(x => x.leg2 == nm.ToLower());
        if (rr == null || ll == null)
        {
            Debug.Log("ERROR!!! Render Failed");
            return;
        }
        lastRend = Instantiate(ll, rr.transform);
        lastRend.transform.position = rr.transform.Find("pos1").position;
        lastRend.transform.forward = -rr.transform.forward;

        if (scl >= 0)
        {
            lastRend.transform.localScale = new Vector3(scl, scl, scl);
        }
        
        SetLayerAllChildren(lastRend.transform, LayerMask.NameToLayer("Rend"));
    }

    public void ClearTransform(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    public List<(float, float, float)> GetTransformChildrenToList(Transform root)
    {
        var res = new  List<(float, float, float)>();
        for (int i = 0; i < root.childCount; i++)
        {
            var r = root.GetChild(i);
            res.Add( (r.position.x, r.position.y, r.position.z) );
        }

        return res;
    }

    public void ClearTransformTransform(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            ClearTransform(root.GetChild(i));
        }
    }

    
    public IEnumerator ArrowFollow(ArrowRenderer ar, Vector3 pos0, Action<Vector3> onReleased, Action<Vector3> eachMa)
    {
        while (true)
        {
            if (Input.GetMouseButtonUp(0))
            {
                var c0 = GetUICam();
                
                if (onReleased != null) 
                    onReleased(c0.ScreenToWorldPoint(Input.mousePosition));
                
                yield break;
            }

            var c = GetUICam();
            var po = c.ScreenToWorldPoint(Input.mousePosition);
            po.z = pos0.z - 4;
            //Debug.Log(pos0 + " " + po);
            ar.SetPositions(pos0, po);

            if (eachMa != null)
            {
                eachMa(po);
            }

            yield return null;
        }
    }

    public bool IsClickInsideRect(RectTransform rt)
    {
        Vector3[] v = new Vector3[4];
        rt.GetWorldCorners(v);

        //Debug.Log("World Corners");
        //for (var i = 0; i < 4; i++)
        //{
        //    Debug.Log("World Corner " + i + " : " + v[i]);
        //}

        float minx = Mathf.Min(Mathf.Min(v[0].x, v[1].x), Mathf.Min(v[2].x, v[3].x));

        
        float maxx = Mathf.Max(Mathf.Max(v[0].x, v[1].x), Mathf.Max(v[2].x, v[3].x));
        
        float maxy = Mathf.Max(Mathf.Max(v[0].y, v[1].y), Mathf.Max(v[2].y, v[3].y));
        float miny = Mathf.Min(Mathf.Min(v[0].y, v[1].y), Mathf.Min(v[2].y, v[3].y));        
        
        
        var vv = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (minx < vv.x && vv.x < maxx && vv.y > miny && vv.y < maxy)
            return true;

        return false;
    }

    public Color GetRarityColor(RarityType rar)
    {
        return rarColors[(int)rar];
    }
    
    public Sprite GetRarityGem(RarityType rar)
    {
        return gemRars[(int)rar];
    }
    
    public Sprite GetRarityCard(RarityType rar)
    {
        return cardRars[(int)rar];
    }
    
    public Sprite GetFrameRarityCard(RarityType rar)
    {
        return cardFrameRars[(int)rar];
    }

    public static Vector3 GetRandomVec(Vector3 lo, Vector3 high)
    {
        return new Vector3(UnityEngine.Random.Range(lo.x, high.x), UnityEngine.Random.Range(lo.y, high.y),UnityEngine.Random.Range(lo.z, high.z));
    }

    public List<int> GetMeRands(List<Object> what, int num, bool canRepeat)
    {
        List<int> ans = new List<int>();

        if (canRepeat)
        {
            for (int i = 0; i < num; i++)
                ans.Add(UnityEngine.Random.Range(0, what.Count));
        }
        else
        {
            int l = 0;
            for (int i = 0; i < num; i++)
            {
                int u = UnityEngine.Random.Range(l, what.Count - num + i + 1);
                ans.Add(u);
                l = u + 1;
            }
        }

        return ans;
    }
    
    public List<int> GetMeRandsWeight(List<Object> what, List<float> weight, int num, bool canRepeat)
    {
        List<int> ans = new List<int>();

        if (canRepeat)
        {
            float sum = 0;
            sum = weight.Sum(x => x);
            
            for (int i = 0; i < num; i++)
            {
                var rf = UnityEngine.Random.Range(0, sum);
                float s0 = 0;
                for (int j = 0; j < weight.Count; j++)
                {
                    s0 += weight[j];
                    if (s0 >= rf)
                    {
                        ans.Add(j);
                        break;
                    }
                }
            }
        }
        else
        {
            int l = 0;
            for (int i = 0; i < num; i++)
            {
                float sum = 0;
                for (int j = l; j < what.Count - num + i + 1; j++)
                    sum += weight[j];
                
                var rf = UnityEngine.Random.Range(0, sum);
                float s0 = 0;
                
                for (int j = l; j < what.Count - num + i + 1; j++)
                {
                    s0 += weight[j];
                    if (s0 >= rf)
                    {
                        ans.Add(j);
                        l = j + 1;
                        break;
                    }
                }

            }
        }

        return ans;
    }
    public void AssignPhysics(GameObject go, bool isProj)
    {
        go.layer = LayerMask.NameToLayer("Projectile");
        
        if (ConfigLoader.GetMetaParamValue("is_2d") > 0)
        {
            if (go.GetComponent<BoxCollider>() != null)
                Destroy(go.GetComponent<BoxCollider>());
            
            if (go.GetComponent<Collider2D>() == null)
            {
                var tt = go.AddComponent<BoxCollider2D>();
                if (!isProj)
                {
                    go.GetComponent<BoxCollider2D>().size = new Vector2(2, 3);
                }

                tt.offset = new Vector2(ConfigLoader.GetMetaParamValue("offset_2d_x"), ConfigLoader.GetMetaParamValue("offset_2d_y"));
                tt.size = new Vector2(ConfigLoader.GetMetaParamValue("size_2d_x"), ConfigLoader.GetMetaParamValue("size_2d_y"));
            }
            
            if (ConfigLoader.GetMetaParamValue("trigger_state") != 2)
                go.GetComponent<Collider2D>().isTrigger = true;
            
            if (isProj)
            {
                var gg = go.AddComponent<Rigidbody2D>();
                gg.interpolation = RigidbodyInterpolation2D.None;
                gg.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                gg.gravityScale = 0;
                gg.isKinematic = true;

                if (ConfigLoader.GetMetaParamValue("trigger_state") == 2)
                {
                    gg.isKinematic = false;
                    gg.interpolation = RigidbodyInterpolation2D.None;
                }

                //if (ww.triggerState == 2)
               //     gg.GetComponent<Collider2D>().sharedMaterial = ww.physMat;
                

            }
        }

        if (ConfigLoader.GetMetaParamValue("is_3d") > 0)
        {
            if (go.GetComponent<Collider>() == null)
            {
                go.AddComponent<BoxCollider>();
            }
            go.GetComponent<Collider>().isTrigger = true;

            if (isProj)
            {
                go.AddComponent<Rigidbody>();
                go.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    public void SpawnArea(Transform root, List<Dops> what, List<Dops> except, int amount, Transform lo, Transform hi)
    {
        
        var ww = what.Select(x => x.val1).ToList();

        
        for (int i = 0; i < amount; i++)
        {
            var c1 = new Vector3(UnityEngine.Random.Range(lo.position.x, hi.position.x ), UnityEngine.Random.Range(lo.position.y, hi.position.y), 0);
            bool q = false;
            for (int j = 0; j < except.Count; j++)
            {
                var c2 = except[j].what.transform.position - c1;
                c2.z = 0;
                if (c2.magnitude < except[j].val1)
                    q = true;
            }
            //?
            if (q) continue;
            
            var g = ModelSet.GetMeNum(ww);

            var gg = GameObject.Instantiate(what[g].what, root);
            gg.transform.position = c1;

        }
    }
    
    public IEnumerator DoFunkyMovement(Transform who, bool toZero = false, float koef = 1)
    {
        float curRot = 0;
        float curDir = 1;
        float rotSpeed = 100;
        float rotRange = 30;
        if (who.name.IndexOf("_move") >= 0) yield break;
        who.name += "_move";

        while (true)
        {
            if (who.name.IndexOf("_move") < 0)
            {
                toZero = true;
                koef = 5;
                //zamedl
                //yield break;
            }
            
            if (toZero && curRot < 0 && curRot + curDir * rotSpeed * Time.deltaTime * koef > 0)
            {
                who.Rotate(0, 0, -curRot);
                curRot = 0;
                yield break;
                //yield return null;
                //continue;
            }
            else if (toZero && curRot > 0 && curRot + curDir * rotSpeed * Time.deltaTime * koef < 0)
            {
                who.Rotate(0, 0, -curRot);
                curRot = 0;
                yield break;
                //yield return null;
                //continue;
            }


            if (curRot + curDir * rotSpeed * Time.deltaTime * koef > rotRange)
            {
                curDir *= -1;
                continue;
                //OneRotIteration(toZero, koef);
            }
            else if (curRot + curDir * rotSpeed * Time.deltaTime * koef < -rotRange)
            {
                curDir *= -1;
                continue;
                //OneRotIteration(toZero, koef);
            }
            else
            {
                who.Rotate(0, 0, curDir * rotSpeed * Time.deltaTime * koef);
                curRot += curDir * rotSpeed * Time.deltaTime * koef;
            }

            yield return null;
        }
    }

    public IEnumerator DoFunkyMovement2(Transform who)
    {
        if (who.name.IndexOf("_move") >= 0) yield break;
        who.name += "_move";
        var sl = who.GetChild(0).localPosition;
        
        ApplyCurve(who.transform.GetChild(0), AnimationCurve.Linear(0,0,1,1), CurveType.MoveY, () =>
            {
                who.GetChild(0).localPosition = sl;
            }, 
            0.2f, 5,0.6f,0, Color.white, pong:true, func: () =>
            {
                return who.name.IndexOf("_move") < 0;
            });
        
        yield return null;
    }

    public IEnumerator DoFunkyHit(Transform who, int side, GameObject effect)
    {
        float cur = 0;
        
        float dl = 30;
        float dl1 = -15;
        
        float spd = 200;
        
        var p0 = who.position - new Vector3(0, 0.3f, 0);
        while (true)
        {
            who.RotateAround(p0, new Vector3(0,0,-1), -side * spd * Time.deltaTime);
            cur += Time.deltaTime * spd;
            if (cur >= dl) break;
            yield return null;
        }

        bool doEff = false;
        
        while (true)
        {
            who.RotateAround(p0, new Vector3(0,0,-1), side * spd * 2 * Time.deltaTime);
            cur -= Time.deltaTime * spd * 2;

            /*
            if (cur < 0 && !doEff)
            {
                doEff = true;
                var ee = Instantiate(effect);
                ee.transform.position = who.position + new Vector3(0, 0.5f, 0);
                ee.SetActive(true);
                ee.transform.localScale = new Vector3(ee.transform.localScale.x * side, ee.transform.localScale.y, ee.transform.localScale.z);                
            }
            */
            
            if (cur <= dl1) break;
            yield return null;
        }


        
        while (true)
        {
            float bs = Time.deltaTime * spd * 3;
            if (cur + bs >= 0) bs = -cur;
            who.RotateAround(p0, new Vector3(0,0,-1), -side * bs);
            cur += bs;
            if (cur >= 0) break;
            yield return null;
        }
        
        yield return null;
    }
    
    
    public void FastMoveAndBack(Transform who, Transform where, float speed, float wait = 0, float waitIn = 0)
    {
        StartCoroutine(FastMoveAndBackA(who, where, speed, wait, waitIn));
    }
    
    public IEnumerator FastMoveAndBackA(Transform who, Transform where, float speed, float wait, float waitIn)
    {
        if (wait > 0)
        {
            yield return new WaitForSeconds(wait);
        }
        
        var savedPos = who.transform.position;
        MoveTo(who, speed, where.transform.position, () =>
        {
            FunctionTimer.Create(() => MoveTo(who, speed, savedPos, null, null, ignoreFlip:true), waitIn);
        }, where);

        yield return null;
    }
    
    public void FastMoveAndBack(Transform who, Vector3 where, float speed, float wait = 0, float waitIn = 0, Action act = null)
    {
        StartCoroutine(FastMoveAndBackB(who, where, speed, wait, waitIn, act));
    }
    
    public IEnumerator FastMoveAndBackB(Transform who, Vector3 where, float speed, float wait, float waitIn, Action act)
    {
        if (wait > 0)
        {
            yield return new WaitForSeconds(wait);
        }

        if (who == null)
        {
            if (act != null) act();
            yield break;
        }
        var savedPos = who.transform.position;
        MoveTo(who, speed, where, () =>
        {
            FunctionTimer.Create(() => MoveTo(who, speed, savedPos, act, null, ignoreFlip:true), waitIn);
        }, null);

        yield return null;
    }


    public IEnumerator RepeatMove(Vector3 from, Vector3 end, Transform who, float spd, Func<bool> check)
    {
        var vec = end - from;
        who.transform.position = from;
        
        while (true)
        {
            if (check())
            {
                yield break;
            }

            float dist = (end - who.position).magnitude;
            //Debug.Log(dist);
            if (dist < Time.deltaTime * spd)
            {
                who.transform.position = from;
            }
            else
            {
                who.transform.position += vec.normalized * Time.deltaTime * spd;
            }

            yield return null;

        }
    }

    public void DoWhirl(float spd, Transform who, Action act)
    {
        StartCoroutine(DoWhirlA(spd, who, act));
    }

    public IEnumerator DoWhirlA(float spd, Transform who, Action act)
    {
        float t = 0;
        while (t < 360)
        {
            float d = spd * Time.deltaTime;
            if (t + d > 360) d = 360 - t;
            who.Rotate(new Vector3(0, d, 0));
            t += d;
            yield return null;
        }

        if (act != null)
            act();
    }

    public enum CurveType
    {
        Scale,
        CanvasFade,
        MoveY,
        Color,
        ScaleAbs,
        Rotate,
        Fill,
        TextFade,
        TextNum,
        ShaderVal
    }

    public void DoButtonAnimation(Transform t)
    {
        ApplyCurve(t, AnimationCurve.Linear(0,1,1,0.75f), CurveType.ScaleAbs, null, 0.2f, 1/0.2f, 1, 0, Color.white,true, repCount:1 );
    }

    public void DoChestAnimation(Transform t, Action act)
    {
        float tm1 = 0.4f;
        float tm2 = 0.4f;
        
        float ev1_y = 0.5f; 
        float ev2_y = 1.5f; 
        float ev1_x = 0.5f;

        float valY = 100;
        
        ApplyCurve(t, AnimationCurve.Linear(0,1,1,ev1_y), CurveType.ScaleAbs, () =>
        {
            ApplyCurve(t, AnimationCurve.Linear(0,ev1_y,1,ev2_y), CurveType.ScaleAbs, () =>
            {

                
            }, tm2, 1.0f/tm2, 1, 0, Color.white, scaleMask:7-4-1, pong:true, repCount:1 );
            
            ApplyCurve(t, AnimationCurve.Linear(0,1,1,ev1_x), CurveType.ScaleAbs, () =>
            {
                
                
            }, tm2, 1.0f/tm2, 1, 0, Color.white, scaleMask:7-4-2, pong:true, repCount:1  );
            
            ApplyCurve(t, AnimationCurve.Linear(0,0,1,valY), CurveType.MoveY, () =>
            {
                
                ApplyCurve(t, AnimationCurve.Linear(0,ev1_y,1,1), CurveType.ScaleAbs, () =>
                {
                    t.GetComponent<SpriterAnim>().CrossFade("open", 0f);
                    if (act != null) act();

                }, tm1, 1.0f/tm1, 1, 0, Color.white, scaleMask:7-4-1 );
                
                
            }, tm2, 1.0f/tm2, 1, 0, Color.white, pong:true, repCount:1 );
            
            
            
            
            
        }, tm1, 1.0f/tm1, 1, 0, Color.white, scaleMask:7-4-1 );

    }
    
    public void ApplyCurve(Transform who, AnimationCurve ac, CurveType curve, Action act, float time, float speed, float evKoef, float wait, Color endColor, bool pong = false, int rotMask = 0, int scaleMask = 7, int repCount = -1, float was = -1, float now = -1, float waitBetween = 0, Func<bool> func = null, string str = "")
    {
        StartCoroutine(ApplyCurveA(who, ac, curve, act, time, speed, evKoef, wait, endColor, pong, rotMask, scaleMask, repCount, was, now, waitBetween, func, str));
    }
    
    public IEnumerator ApplyCurveA(Transform who, AnimationCurve ac, CurveType curve, Action act, float time, float speed, float evKoef, float wait, Color endColor, bool pong, int rotMask, int scaleMask, int repCount, float was, float now, float waitBetween, Func<bool> func, string str)
    {
        if (wait > 0)
        {
            yield return new WaitForSeconds(wait);
        }

        if (curve == CurveType.Scale)
        {
            var savedScl = who.localScale;
            if (savedScl == Vector3.zero)
                savedScl = Vector3.one;
                
            FIF_Z:
            
            float tm = 0;
            while (tm < time)
            {
                yield return null;
                if (this == null) yield break;
                
                if (who == null) break;
                var g = ac.Evaluate(tm * speed) *evKoef;

                var ff = savedScl * g;
                if ((scaleMask & 1) == 0) ff.x = who.localScale.x;
                if ((scaleMask & 2) == 0) ff.y = who.localScale.y;
                if ((scaleMask & 4) == 0) ff.z = who.localScale.z;
                who.localScale = ff;

                tm += Time.deltaTime;

                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }
            }
            
            if (pong)
            {
                tm = time;
                while (tm > 0)
                {
                    yield return null;
                    if (this == null) yield break;
                    
                    if (who == null) break;
                    var g = ac.Evaluate(tm * speed) *evKoef;

                    var ff = savedScl * g;
                    if ((scaleMask & 1) == 0) ff.x = who.localScale.x;
                    if ((scaleMask & 2) == 0) ff.y = who.localScale.y;
                    if ((scaleMask & 4) == 0) ff.z = who.localScale.z;
                    who.localScale = ff;

                    tm -= Time.deltaTime;
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }
                }

                who.localScale = savedScl;

                if (repCount < 0)
                goto FIF_Z;
            }
        }
        else if (curve == CurveType.ShaderVal)
        {
            var spriteRenderer = who.GetComponentInChildren<SpriteRenderer>();
            var propertyBlock = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(propertyBlock);

            float tm = 0;
            while (tm < time)
            {
                if (who == null) break;
                
                float b = was + (now - was) * tm;
                propertyBlock.SetFloat(str, b);
                spriteRenderer.SetPropertyBlock(propertyBlock);
                tm += Time.deltaTime * speed;
                yield return null;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }
            }

            if (pong)
            {
                tm = 0;
                while (tm < time)
                {
                    if (who == null) break;
                
                    float b = now + (was - now) * tm;
                    propertyBlock.SetFloat(str, b);
                    spriteRenderer.SetPropertyBlock(propertyBlock);
                    
                    tm += Time.deltaTime * speed;
                    yield return null;
                
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }
                }
                
                propertyBlock.SetFloat(str, was);
                if (spriteRenderer) spriteRenderer.SetPropertyBlock(propertyBlock);
            }
            
            

            if (act != null)
                act();
        }
        else if (curve == CurveType.TextNum)
        {
            float tm = 0;
            var im = who.GetComponent<TextMeshProUGUI>();
            while (tm < time)
            {
                if (who == null) break;
                
                int b = (int)(was + (now - was) * tm);
                im.text = b.ToString();
                
                tm += Time.deltaTime * speed;
                yield return null;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }
            }

            im.text = now.ToString();
        }
        else if (curve == CurveType.Fill)
        {
            float tm = 0;
            var im = who.GetComponent<Image>();
            while (tm < time)
            {
                if (who == null) break;
                im.fillAmount += Time.deltaTime;
                tm += Time.deltaTime;
                yield return null;
            }

            im.fillAmount = time;
            
            if (func != null && func())
            {
                if (act != null) act();
                yield break;
            }
        }
        else if (curve == CurveType.Rotate)
        {
            float tm = 0;
            while (tm < time)
            {
                if (who == null) break;
                who.Rotate((rotMask & 1) * Time.deltaTime * 90, ((rotMask & 2) / 2) * Time.deltaTime * 90, ((rotMask & 4) / 4) * Time.deltaTime * 90);
                tm += Time.deltaTime;
                yield return null;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }
            }
        }
        else if (curve == CurveType.ScaleAbs)
        {
            var savedScl = who.localScale;
            
            FIF_A:
            
            //Debug.Log(Time.time);
            float tm = 0;
            while (tm < time)
            {
                yield return null;
                if (this == null) break;
                var g = ac.Evaluate(tm * speed) * evKoef;
                var ff = new Vector3(g, g, g);
                if ((scaleMask & 1) == 0) ff.x = who.localScale.x;
                if ((scaleMask & 2) == 0) ff.y = who.localScale.y;
                if ((scaleMask & 4) == 0) ff.z = who.localScale.z;
                who.localScale = ff;
                tm += Time.deltaTime;
                    //Debug.Log(tm + " " + g);
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }
            }

            var g1 = ac.Evaluate(time*speed) * evKoef;
            //who.localScale = new Vector3(g1, g1, g1);

            if (pong)
            {
                while (tm > 0)
                {
                    yield return null;
                    if (this == null) break;
                    var g = ac.Evaluate(tm * speed) * evKoef;
                    var ff = new Vector3(g, g, g);
                    if ((scaleMask & 1) == 0) ff.x = who.localScale.x;
                    if ((scaleMask & 2) == 0) ff.y = who.localScale.y;
                    if ((scaleMask & 4) == 0) ff.z = who.localScale.z;
                    who.localScale = ff;
                    tm -= Time.deltaTime;
                    //Debug.Log(tm + " " + g);
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }
                }

                //who.localScale = savedScl;
                
                if (repCount < 0)
                    goto FIF_A;
            }

            //Debug.Log(Time.time);
        }
        else if (curve == CurveType.MoveY)
        {
            var svy = who.transform.localPosition;
            
            FIF_Y:
            
            float tm = 0;
            while (tm < time)
            {
                yield return null;
                if (this == null) yield break;
                
                if (who == null) break;
                var g = ac.Evaluate(tm * speed) * evKoef;
                who.transform.localPosition = svy + new Vector3(0, g, 0);
                tm += Time.deltaTime;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }
            }

            if (pong)
            {
                tm = time;
                while (tm > 0)
                {
                    yield return null;
                    if (this == null) yield break;
                    
                    if (who == null) break;
                    var g = ac.Evaluate(tm * speed) * evKoef;
                    who.transform.localPosition = svy + new Vector3(0, g, 0);
                    tm -= Time.deltaTime;
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }
                }
                
                if (repCount < 0)
                goto FIF_Y;
            }
        }
        else if (curve == CurveType.CanvasFade)
        {
            var cv = who.GetComponent<CanvasGroup>();
            if (cv == null) cv = who.gameObject.AddComponent<CanvasGroup>();
            
            FIF:
            
            float tm = 0;
            while (tm < time)
            {
                yield return null;
                if (this == null) yield break;
                
                if (who == null) break;
                var g = ac.Evaluate(tm * speed) * evKoef;
                cv.alpha = g;
                tm += Time.deltaTime;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }

            }

            if (waitBetween > 0)
                yield return new WaitForSeconds(waitBetween);
            
            if (pong)
            {
                tm = time;
                while (tm > 0)
                {
                    yield return null;
                    if (this == null)
                        yield break;
                    
                    if (who == null) break;
                    var g = ac.Evaluate(tm * speed) * evKoef;
                    cv.alpha = g;
                    tm -= Time.deltaTime;
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }

                }
                
                if (repCount < 0)
                goto FIF;
            }
            
        }
        else if (curve == CurveType.TextFade)
        {
            var cv = who.GetComponent<TextMeshPro>();
            //if (cv == null) cv = who.gameObject.AddComponent<CanvasGroup>();
            
            FIF:
            
            float tm = 0;
            while (tm < time)
            {
                yield return null;
                if (this == null) yield break;
                
                if (who == null) break;
                var g = ac.Evaluate(tm * speed) * evKoef;
                cv.alpha = g;
                tm += Time.deltaTime;
                
                if (func != null && func())
                {
                    if (act != null) act();
                    yield break;
                }

            }

            if (pong)
            {
                tm = time;
                while (tm > 0)
                {
                    yield return null;
                    if (this == null)
                        yield break;
                    
                    if (who == null) break;
                    var g = ac.Evaluate(tm * speed) * evKoef;
                    cv.alpha = g;
                    tm -= Time.deltaTime;
                    
                    if (func != null && func())
                    {
                        if (act != null) act();
                        yield break;
                    }

                }
                goto FIF;
            }
            
        }
        else if (curve == CurveType.Color)
        {
            var svk = GetColor(who.gameObject);
            FadeToColor(who.gameObject, endColor, speed, () =>
            {
                if (pong)
                {
                    FadeToColor(who.gameObject, svk, speed, () =>
                    {
                        if (repCount < 0)
                        {
                            ApplyCurve(who, ac, curve, act, time, speed, evKoef, wait, endColor, pong, rotMask);
                        }
                    });    
                }
            });
        }

        yield return null;

        if (act != null)
            act();
    }

    public void FillStars(Transform starsHolder, int stars)
    {
        starsHolder.gameObject.SetActive(true);
            
        for (int i = 0; i < starsHolder.transform.childCount; i++)
            starsHolder.transform.GetChild(i).GetChild(0).gameObject.SetActive(stars > i);
    }

    public GameObject CreateOtlet(Transform t, Sprite spr, string txt, Color sprColor, Color txtColor, Vector3 dlt)
    {
        var gg = Instantiate(imgTxt, t.parent);
        gg.transform.position = t.position;

        gg.GetComponentInChildren<Image>().sprite = spr;
        gg.GetComponentInChildren<TextMeshProUGUI>().text = txt;

        gg.GetComponentInChildren<Image>().color = sprColor;
        gg.GetComponentInChildren<TextMeshProUGUI>().color = txtColor;
        
        gg.transform.position += dlt;

        return gg;
    }

    public void AttachHook(Transform who, Vector3 ep, float spd)
    {
        StartCoroutine(AttachHookA(who, ep, spd));
    }

    public IEnumerator AttachHookA(Transform who, Vector3 ep, float spd)
    {
        var pp = new GameObject();
        pp.name = "HookL";
        var pl = pp.AddComponent<LineRenderer>();
        pl.useWorldSpace = true;
        pl.material = chainMat;
        pl.sortingOrder = 2002;
        pl.textureScale = new Vector2(3, 3);
        pl.textureMode = LineTextureMode.Tile;
        var hh = Instantiate(anchor);
        hh.transform.position = ep;
        hh.transform.right = - (ep - who.position - new Vector3(0, 0.3f));
            ;        
        MoveTo(who, spd, ep, () =>
        {
          Destroy(pp);
          Destroy(hh);
        }, null, eachF: (x) =>
        {
            pl.SetPositions(new Vector3[]{x.transform.position + new Vector3(0, 0.3f), ep});
        });
        //move to each ?

        yield return null;

    }

    public bool DistCheck(Transform t)
    {
        var gg = GameObject.FindGameObjectWithTag("Player");

        if (ConfigLoader.GetMetaParamValue("runtime_fp") > 0)
        {
            var vec = t.position - gg.transform.position;
            if (vec.magnitude < ConfigLoader.GetMetaParamValue("use_distance"))
                return true;
            else return false;
        }
        else
        {
            var c0 = gg.GetComponent<ObjHolder>().obj.Position;
            var c1 = MainStates.instance.GetClosestPos(t.position, false);
            if (MainStates.instance.GetManLen((int)c0.x, (int)c0.y, (int)c1.x, (int)c1.y) <= 1)
            {
                return true;
            }
            else return false;            
        }

    }

    public static List<(float, float, float)> ConvertPath(List<(int, int, int)> path)
    {
        List<(float, float, float)> result = new List<(float, float, float)>();
        foreach (var v in path)
        {
            result.Add((v.Item1, v.Item2, v.Item3));
        }
        return result;
    }

    public static Vector3 GetMousePoint()
    {
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var h = Physics.Raycast(ray, out RaycastHit hit, 100, 1<< LayerMask.NameToLayer("Earth"));
            return hit.point;
        }
    }
    
    public void MoveToMany(Transform who, float speed, List<(float, float, float)> path, int cur, Action act, Action onEach = null)
    {
        //if (who.name.IndexOf("_move") < 0) who.name += "_move";
        
            var mon = who.GetComponent<ObjHolder>();
        if (cur < path.Count)
        {
            //its endpos
            Vector3 pp = Vector3.zero;
            
            
            //if (ConfigLoader.GetMetaParamValue("runtime_mapping") == 0)
            //    pp = MainStates.instance.mappingPositions[(int)path[cur].Item1, (int)path[cur].Item2].position;
            if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0 
                || ConfigLoader.GetMetaParamValue("mode_isometric") > 0
                || ConfigLoader.GetMetaParamValue("mode_hex") > 0)
            {
                pp = PositionSetter.instance.floors[(int)path[cur].Item1, (int)path[cur].Item2].transform.position;
            }
            else
            {
                pp = new Vector3(path[cur].Item1, path[cur].Item2, path[cur].Item3);
            }
            
            //boat condition BINOME THWBBB
            if (false)
            {
                var wasF = MainStates.instance.GetBinome(mon.obj.Position.x, mon.obj.Position.y);
                var nowF = MainStates.instance.GetBinome(path[cur].Item1, path[cur].Item2);

                if ((wasF == null || wasF.name.IndexOf("floor") >= 0) && nowF != null &&
                    nowF.name.IndexOf("water") >= 0 && mon.attachedVeh == null)
                {
                    var jj = Instantiate(ResourceHolder.instance.miscGO["boat"]);
                    jj.transform.position = mon.transform.position;
                    mon.attachedVeh = jj;
                    jj.transform.SetParent(mon.transform);
                }

                if (wasF != null && (nowF == null || nowF.name.IndexOf("floor") >= 0) &&
                    wasF.name.IndexOf("water") >= 0)
                {
                    Destroy(mon.attachedVeh);
                    mon.attachedVeh = null;
                }
            }


            if (mon != null && ConfigLoader.GetMetaParamValue("mode_manhattan") < 1)
            {
                mon.obj.Position = new Vector2(path[cur].Item1, path[cur].Item2);
            }

            if (mon) mon.DoAnim("walk");
            
            MoveTo(who, speed, pp + MainStates.instance.dlt, () =>
            {
                var bb = who.GetComponent<ObjHolder>();
                if (bb != null) bb.obj.Position = new Vector2(path[cur].Item1, path[cur].Item2);
                if (onEach != null) onEach();
                MoveToMany(who, speed, path, cur + 1, act);
            }, null, useRight:false);

        }
        else
        {
            if (mon) mon.DoAnim("idle");
            if (act != null)
                act();
        }
    }
        
    public void MoveTo(Transform who, float speed, Vector3 endPos, Action act, Transform target, float x0 = -1, float y0 = -1, float z0 = -1, bool? useRight = null, TravelType travelType = TravelType.linear, float dltTime = 0, bool penetrate = false, Transform dopTravel = null, Vector3 fPos = default, bool useMoveCurve = false, float dAcc = 1.0f, string lalk = "xx", float travelTm =-1,
        Action<Transform> eachF = null, Func<Transform, bool> inter = null, bool ignoreFlip = false)
    {
        if (who.name.IndexOf("_move") < 0) who.name += "_move";
        
        var nm = who.GetComponent<ObjHolder>();

        if (nm != null && !ignoreFlip)
        {
            nm.obj.SetScale(endPos.x > nm.transform.position.x);
            //Debug.Log("INMOVE: " + who.name + " " + (nm.cIdler == null ? "NULL" : nm.cIdler.inTravel));
        }
        else
        {
            //Debug.Log(  "INMOVE: " + who.name + " COMBAT IDLER NULL");
        }

        float eps = 0.1f;
        if (nm != null && !ignoreFlip)
        {
            if (endPos.x < who.transform.position.x - eps && !nm.ignoreScale)
                nm.SetFlipScale(-1);
            else if (endPos.x > who.transform.position.x + eps && !nm.ignoreScale)
                nm.SetFlipScale(1);
        }
        
        if (travelTm > 0)
        {
            if (target != null)
            {
                var dd = target.position - who.position;
                if (z0 >= 0) dd.z = 0;
                speed = dd.magnitude / travelTm;
            }
            else
            {
                var dd = endPos - who.position;
                if (z0 >= 0) dd.z = 0;
                speed = dd.magnitude / travelTm;
            }
        }
        
        bool overCurv = true;
        if (ConfigLoader.GetMetaParamValue("over_proj_spd") > 0)
        {
            overCurv = false;
            useMoveCurve = false;
            speed = ConfigLoader.GetMetaParamValue("over_proj_spd");
        }
        
        if (dopTravel)
        {
            who.GetOrAddComponent<MoveDir>().cr =
            StartCoroutine(MoveToA(who, speed, endPos, act, target, x0, y0, z0, useRight, travelType, dltTime,
                penetrate, null, fPos, overCurv, 3f, lalk, eachF, inter));
            
            dopTravel.GetOrAddComponent<MoveDir>().cr =
            StartCoroutine(MoveToA(dopTravel, speed, endPos, null, target, x0, y0, z0, useRight, travelType, dltTime,
                false, null, fPos, overCurv, 1, lalk, eachF, inter));
        }
        else
        {
            who.GetOrAddComponent<MoveDir>().cr =
            StartCoroutine(MoveToA(who, speed, endPos, act, target, x0, y0, z0, useRight, travelType, dltTime,
                penetrate, dopTravel, fPos, useMoveCurve, dAcc, lalk, eachF, inter));
        }
    }

    public IEnumerator MoveToA(Transform who, float speed, Vector3 endPos, Action act, Transform target, float x0 = -1, float y0 = -1, float z0 = -1, bool? useRight = null, TravelType travelType = TravelType.linear, float dltTime = 0, bool penetrate = false, Transform dopTravel = null, Vector3 fPos = default, bool useMoveCurve = false, float dAcc = 1.0f, string lalk = "xx", Action<Transform> eachF = null, Func<Transform, bool> inter = null)
    {
        //thwbbb
        /*
        var ho = who.GetComponent<UnoProjectile>();
        if (ho != null && ho.delay > 0)
        {
            dltTime = ho.delay + (dltTime < 0 ? 0 : dltTime);
        }
        */

        if (ConfigLoader.GetMetaParamValue("over_proj_spd") < 0 && dltTime > 0)
        yield return new WaitForSeconds(dltTime);
        //max lifetime ? 10
        float maxLifeTime = 10;


        if (z0 >= 0)
            endPos.z = who.transform.position.z;

        //case of self projectiles
        //if (dopTravel)
        //{
            //dAcc ?
        //    useMoveCurve = true;
        //}

        if (who == null) yield break;
        who.gameObject.SetActive(true);

        endPos = endPos + fPos;
        float tCalc = 0;
        float t0 = 0;
        if (who.position != endPos)
        {
            tCalc = (who.position - endPos).magnitude / speed;
        }

        Vector3 dr = Vector3.one;
        if (penetrate)
        {
            dr = (endPos - who.position).normalized;
        }

        var startPos = who.transform.position;
        var scl = who.transform;

        //physics ?
        //thwbbb
        
        /*
        var ww = who.GetComponent<UnoProjectile>();
        if (ww == null && dopTravel != null)
            ww = dopTravel.GetComponent<UnoProjectile>();

        if (ww != null) ww.prevPos = ww.transform.position;
        
        if (ww != null && ConfigLoader.GetMetaParamValue("ally_angle") > 0)
        {
            if (ww.projectile.who != null && ww.projectile.who.tags.Contains("player"))
            {

                Rigidbody2D riga = who.GetComponent<Rigidbody2D>();
                if (riga == null) riga = who.gameObject.AddComponent<Rigidbody2D>();
                
                riga.velocity = who.GetComponent<UnoProjectile>().angle * 10;
                who.GetComponent<UnoProjectile>().RecalcVel();

                yield break;
            }
        }
        */

        Vector3 ee = endPos - who.transform.position;
        if (target != null)
            ee = target.position + fPos - who.transform.position;

        var jj = who.GetOrAddComponent<MoveDir>();
        jj.where = endPos;
        jj.dir = ee;
        
        float prevDist = 1e+10f;
        
        while (true)
        {
            if (who != null && who.name.IndexOf("_move") < 0)
            {
                yield break;
            }
            
            if (inter != null)
            {
                var g = inter(who);
                if (!g) break;
            }
            
            if (eachF != null)
                eachF(who);
            
            if (dopTravel) dopTravel.transform.position = who.transform.position;

            if (ConfigLoader.GetMetaParamValue("recalc_layer_every_frame") > 0)
            {
                if (who == null) yield break;
                CalculateLayer(who.gameObject);
            }
            
            if (who == null) yield break;

            endPos = jj.where;
            if (target != null) endPos = target.position + fPos;
            
            
            var dir = endPos - who.transform.position;
            t0 += Time.deltaTime * dAcc;
            
            if (x0 >= 0) dir.x = 0;
            if (y0 >= 0) dir.y = 0;
            if (z0 >= 0) dir.z = 0;

            if (t0 > maxLifeTime && penetrate)
            {
                break;
            }

            bool cc = false;
            //bool cc = (ww != null && ww.penetrateEndPos);
            
            //move curve by 1 sec
            if (useMoveCurve && t0 > 1)
            {
                break;
            }

            if (useMoveCurve)
            {
                who.transform.position = startPos + ee * moveCurve.Evaluate(t0);
            }
            else
            {
                
                if (penetrate && !cc)
                {
                    who.transform.position += dr.normalized * speed * Time.deltaTime;

                    if (useRight.Value)
                    {
                        who.transform.right = dir;
                    }

                    yield return null;
                    continue;
                }
                
                
                if (dir.magnitude < Time.deltaTime * speed || (t0 > tCalc && !penetrate))
                {
                    who.transform.position = new Vector3(endPos.x, endPos.y, endPos.z);
                    //Debug.Log("STTT-A");

                    break;
                }
                else
                
                
                {
                    //Debug.Log("STTT-B");
                    if (travelType == TravelType.parabolik)
                    {
                        float dy = 0;
                        dy = ConfigLoader.GetMetaParamValue("k_parab") * 3 * t0 * (tCalc - t0);

                        //Debug.Log(t0 + " " + tCalc + " " + dy + " " + startPos + " " + endPos + " " + who.transform.position);
                        //need to be reworked
                        who.transform.position = startPos + (endPos - startPos) * (t0 / tCalc) + new Vector3(0, dy, 0);
                        //Debug.Log(t0 + " -- " + who.transform.position);
                        
                        //who.transform.position += dir.normalized * Time.deltaTime * speed;
                        
                    }
                    else
                    {
                        var lop = dir.normalized;
                        //if (dir.magnitude < 1) lop = dir;
                        who.transform.position += lop * speed * Time.deltaTime;
                    }

                    var nn = useRight.HasValue && useRight.Value == false;
                    if (
                        (useRight is true || ConfigLoader.GetMetaParamValue("use_right") > 0) &&
                        (!nn)
                        )
                    {
                        //who.transform.right = dir;

                        if (who.GetComponent<ParticleSystem>() != null && dir.x < 0)
                        {
                            who.transform.localScale = new Vector3(-Mathf.Abs(scl.localScale.x), scl.localScale.y,
                                scl.localScale.z);
                        }

                        //who.transform.localEulerAngles = new Vector3(0, 0, who.transform.localEulerAngles.z);
                        
                        
                        
                        who.transform.rotation = Quaternion.LookRotation(dir, new Vector3(0, 0, -1));
                        who.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1), dir);
                        who.transform.rotation *= Quaternion.AngleAxis(90f, who.transform.forward);
                        
                    }
                }


                prevDist = dir.magnitude;
            }



            yield return null;
        }
        
        if (dopTravel) 
            dopTravel.transform.position = who.transform.position;

        //if (ww != null) ww.FastApplyLine(startPos, endPos);
            //ww.LateUpdate();
        
            Debug.Log(who.name);
            
            if (who != null)
                EventManager.INV("MOVE_ENDED",new ArgPass{go = who.gameObject});
            //EventManager.MOVE_ENDED.Invoke(new ArgPass{who = who.gameObject});

            //CalculateLayer(who.gameObject);
            if (who != null)
            {
                who.name = who.name.Replace("_move", "");
            }
            
        if (act != null)
        {
            act();
        }
    }

    void AlignRightTo_Manual(Vector3 dir, Transform who)
    {
        if (dir == Vector3.zero) return;
        
        // Store current Euler angles
        Vector3 currentEuler = who.transform.eulerAngles;
        
        // Calculate the target rotation while preserving original up direction
        Quaternion targetRotation = Quaternion.LookRotation(who.transform.forward, who.transform.up);
        
        // Align right axis while maintaining forward/up constraints
        Quaternion finalRotation = Quaternion.FromToRotation(who.transform.right, dir.normalized) * targetRotation;
        
        // Apply the rotation while keeping the original Euler angles for axes we don't want to change
        Vector3 finalEuler = finalRotation.eulerAngles;
        finalEuler.x = currentEuler.x;
        finalEuler.z = currentEuler.z;
        
        who.transform.rotation = Quaternion.Euler(finalEuler);
    }

    public void RotateToward(Transform who, Vector3 end, Action act)
    {
        StartCoroutine(RotateTowardsA(who, end, act));
    }

    public IEnumerator RotateTowardsA(Transform who, Vector3 end, Action act)
    {
        float t = 0;
        var vecW = who.forward;
        while (t < 1)
        {
            var ans = vecW * (1 - t) + end * t;
            who.forward = ans;
            t += Time.deltaTime;
            yield return null;
        }
        
        yield return null;

        if (act != null)
            act();
    }
    
    public void MoveToTime(Transform who, float tm, Vector3 endPos, Action act, float x0 = -1, float y0 = -1, float z0 = -1)
    {
        StartCoroutine(MoveToTimeA(who, tm, endPos, act, x0, y0, z0));
    }

    public IEnumerator MoveToTimeA(Transform who, float tm, Vector3 endPos, Action act, float x0 = -1, float y0 = -1, float z0 = -1)
    {

        var cc = endPos - who.transform.position;
        if (x0 >= 0) cc.x = 0;
        if (y0 >= 0) cc.y = 0;
        if (z0 >= 0) cc.z = 0;

        float speed = cc.magnitude / tm;

        while (true)
        {
            var dir = endPos - who.transform.position;

            if (x0 >= 0) dir.x = 0;
            if (y0 >= 0) dir.y = 0;
            if (z0 >= 0) dir.z = 0;

            if (dir.magnitude < Time.deltaTime * speed)
            {
                who.transform.position = new Vector3(endPos.x, endPos.y, who.transform.position.z);
                break;
            }
            else
            {
                who.transform.position += dir.normalized * speed * Time.deltaTime;
            }
            yield return null;
        }

        if (act != null)
        {
            act();
        }
    }

    public IEnumerator LeafFall(Transform who, float downSpd, float rotRange, float rotSpeed, float earth = 0, float koef = 1, Action after = null)
    {
        float curRot = 0;
        float curDir = 1;
        while (who.position.y > earth)
        {
            if (curRot + curDir * rotSpeed * Time.deltaTime * koef > rotRange)
            {
                curDir *= -1;
                //OneRotIteration(toZero, koef);
            }
            else if (curRot + curDir * rotSpeed * Time.deltaTime * koef < -rotRange)
            {
                curDir *= -1;
                //OneRotIteration(toZero, koef);
            }
            
            who.RotateAround(who.transform.position + new Vector3(0,0.4f,0), new Vector3(0,0,-1), curDir * rotSpeed * Time.deltaTime * koef);
            //who.Rotate(0,0,curDir * rotSpeed * Time.deltaTime * koef);
            curRot += curDir * rotSpeed * Time.deltaTime * koef;

            who.transform.position -= new Vector3(0, downSpd, 0) * Time.deltaTime;
            yield return null;

        }

        if (after != null)
            after();
        
        yield break;
    }

    public IEnumerator FastTwitch(Transform who, float rotRange, float rotSpeed, float koef = 1)
    {
        if (who.name.IndexOf("_twich") >= 0) yield break;
        who.name += "_twich";
        
        float curRot = 0;
        float curDir = -1;
        int l = 0;
        while (true)
        {
            if (curRot + curDir * rotSpeed * Time.deltaTime * koef > rotRange)
            {
                curDir *= -1;
                l++;
                //OneRotIteration(toZero, koef);
            }
            else if (curRot + curDir * rotSpeed * Time.deltaTime * koef < -rotRange)
            {
                curDir *= -1;
                l++;
                //OneRotIteration(toZero, koef);
            }

            if (l >= 2 && curRot + curDir * rotSpeed * Time.deltaTime * koef < 0)
            {
                who.Rotate(0,0,-curRot);
                curRot = 0;
                break;
            }
            //who.RotateAround(who.transform.position + new Vector3(0,0.4f,0), new Vector3(0,0,-1), curDir * rotSpeed * Time.deltaTime * koef);
            who.Rotate(0,0,curDir * rotSpeed * Time.deltaTime * koef);
            curRot += curDir * rotSpeed * Time.deltaTime * koef;

            //who.transform.position -= new Vector3(0, downSpd, 0) * Time.deltaTime;
            yield return null;

        }

        who.name = who.name.Replace("_twich", "");
        yield break;
    }

    public void BlinkRed(GameObject who, float spd = 2, bool forceRed = false)
    {
        if (ConfigLoader.GetMetaParamValue("use_flash") > 0 && !forceRed)
            StartCoroutine(DoFlash(who));
        else 
            StartCoroutine(BlinkRedA(who, spd));
    }

    public Material flashMaterial;
    public Material spriteMaterial;
    public Material shadowMat;

    public static GameObject CreateRuntimeShadow(GameObject go)
    {
        var shd = new GameObject();
        shd.name = "SHADOW";
        shd.transform.parent = go.transform;
        shd.transform.localPosition = Vector3.zero;
        var sr = shd.AddComponent<SpriteRenderer>();
        sr.sprite = go.GetComponent<SpriteRenderer>().sprite;
        shd.GetComponent<Renderer>().material = Instance.shadowMat;
        shd.GetComponent<Renderer>().sortingOrder = go.GetComponent<Renderer>().sortingOrder - 2;
        shd.transform.localScale = new Vector3(1, 0.333f, 0.333f);
        return shd;
    }

    public static string GetLowest(string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            var sklName1 = sklName.Substring(0, sklName.Length - 1);
            return  sklName1;
        }

        return sklName;
    }

    public static int GetNum(string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            return tt - '0';
        }

        return 0;
    }
    
    public static string GetPrev(string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            var sklName1 = sklName.Substring(0, sklName.Length - 1);
            if (tt - 1 != '0')
            {
                sklName1 += (char)(tt - 1);
            }

            return  sklName1;
        }

        return sklName;
    }
    
    public static string GetNext(string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            var sklName1 = sklName.Substring(0, sklName.Length - 1);
            if (tt + 1 != '0')
            {
                sklName1 += (char)(tt + 1);
            }

            return  sklName1;
        }

        return sklName + "1";
    }
    
    public void BuildAngleFillMesh(GameObject filler, Vector3 center, Vector3 p1, Vector3 p2)
    {
        var angleFillMeshFilter = filler.GetComponent<MeshFilter>();
        var angleFillMesh = angleFillMeshFilter.mesh;
        
        if (angleFillMeshFilter == null)
            return;

        if (angleFillMesh == null)
        {
            angleFillMesh = new Mesh();
            angleFillMesh.name = "AngleFillMesh";
            angleFillMeshFilter.mesh = angleFillMesh;
        }

        angleFillMesh.Clear();

        Vector3[] vertices = new Vector3[3];
        vertices[0] = angleFillMeshFilter.transform.InverseTransformPoint(center);
        vertices[1] = angleFillMeshFilter.transform.InverseTransformPoint(p1);
        vertices[2] = angleFillMeshFilter.transform.InverseTransformPoint(p2);

        float cross =
            (vertices[1].x - vertices[0].x) * (vertices[2].y - vertices[0].y) -
            (vertices[1].y - vertices[0].y) * (vertices[2].x - vertices[0].x);

        int[] triangles = cross >= 0f
            ? new int[] { 0, 1, 2 }
            : new int[] { 0, 2, 1 };

        angleFillMesh.vertices = vertices;
        angleFillMesh.triangles = triangles;
        angleFillMesh.uv = new Vector2[]
        {
            new Vector2(0.5f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        angleFillMesh.RecalculateBounds();
        angleFillMesh.RecalculateNormals();
    }
    
    public List<Vector2> GetAllColliderBoundaryPoints(Collider2D col)
    {
        List<Vector2> worldPoints = new List<Vector2>();
        Collider2D collider = col;
        
        if (collider == null) return worldPoints;
        
        switch (collider)
        {
            case EdgeCollider2D edge:
                foreach (Vector2 point in edge.points)
                    worldPoints.Add(transform.TransformPoint(point));
                break;
                
            case PolygonCollider2D poly:
                for (int i = 0; i < poly.pathCount; i++)
                {
                    foreach (Vector2 point in poly.GetPath(i))
                        worldPoints.Add(transform.TransformPoint(point));
                }
                break;
                
            case BoxCollider2D box:
                Vector2[] boxPoints = GetBoxCollider2DWorldPoints(box);
                worldPoints.AddRange(boxPoints);
                break;
                
            case CircleCollider2D circle:
                // For circles, sample points along perimeter
                Vector2 circleCenter = transform.TransformPoint(circle.offset);
                float radius = circle.radius * Mathf.Max(
                    Mathf.Abs(transform.lossyScale.x),
                    Mathf.Abs(transform.lossyScale.y)
                );
                
                int sampleCount = 24; // Adjust for resolution
                for (int i = 0; i < sampleCount; i++)
                {
                    float angle = i * 360f / sampleCount * Mathf.Deg2Rad;
                    Vector2 point = circleCenter + new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    );
                    worldPoints.Add(point);
                }
                break;
        }
        
        return worldPoints;
    }
    
    Vector2[] GetBoxCollider2DWorldPoints(BoxCollider2D box)
    {
        Vector2 center = box.offset;
        Vector2 size = box.size;
        
        Vector2[] localPoints = new Vector2[4];
        localPoints[0] = center + new Vector2(-size.x / 2, -size.y / 2);
        localPoints[1] = center + new Vector2( size.x / 2, -size.y / 2);
        localPoints[2] = center + new Vector2( size.x / 2,  size.y / 2);
        localPoints[3] = center + new Vector2(-size.x / 2,  size.y / 2);
        
        Vector2[] worldPoints = new Vector2[4];
        for (int i = 0; i < 4; i++)
            worldPoints[i] = transform.TransformPoint(localPoints[i]);
        
        return worldPoints;
    }
  
    public IEnumerator DoFlash(GameObject who)
    {
        if (who.name.IndexOf("_flash") >= 0) yield break;
        who.name += "_flash";
        
        //Debug.Log("flashy");
        var spriteRenderer = who.GetComponentInChildren<SpriteRenderer>();
        
        //well
        //it can be meshrenderer


        Renderer rend = spriteRenderer;
        if (spriteRenderer == null)
        {
            rend = who.GetComponentInChildren<Renderer>();
        }

        if (rend.name == "MeshRoot")
        {
            FadeToColor(who, Color.white, 5, () =>
                FadeToColor(who, new Color(0.5f, 0.5f, 0.5f, 1), 5));
            yield break;
        }
        
            // Get the material that the SpriteRenderer uses, 
        // so we can switch back to it after the flash ended.
        var originalMaterial = rend.material;
        
        rend.material = flashMaterial;

        // Pause the execution of this function for "duration" seconds.
        yield return new WaitForSeconds(0.2f);

        // After the pause, swap back to the original material.
        if (rend != null)
            rend.material = originalMaterial;

        if (who) who.name = who.name.Replace("_flash", "");
    }

    public IEnumerator StopLeg(Transform who)
    {
        if (who.name.IndexOf("_leg") >= 0) yield break;
        who.name += "_leg";
        var tt = who.transform.Find("rightLeg");

        int l = 0;
        float curRot = 0;
        float yy = Mathf.Sign(who.localScale.x);
        float curDir = 1;
        float rotSpeed = 200;
        float koef = 1;
        float rotRange = 30;
        
        while (true)
        {
            if (curRot + curDir * rotSpeed * Time.deltaTime * koef > rotRange)
            {
                curDir *= -1;
                l++;
                //OneRotIteration(toZero, koef);
            }
            else if (curRot + curDir * rotSpeed * Time.deltaTime * koef < 0)
            {
                curDir *= -1;
                l++;
                //OneRotIteration(toZero, koef);
            }

            if (l >= 2)
            {
                who.RotateAround(tt.position, new Vector3(0,0,-1), -curRot * yy);
                curRot = 0;
                break;
            }
            
            who.RotateAround(tt.position, new Vector3(0,0,-1), curDir * rotSpeed * Time.deltaTime * koef * yy);
            //who.Rotate(0,0,curDir * rotSpeed * Time.deltaTime * koef);
            curRot += curDir * rotSpeed * Time.deltaTime * koef;

            yield return null;
        }
        
        //
        who.name = who.name.Replace("_leg", "");
    }

    public IEnumerator BlinkRedA(GameObject who, float spd = 2)
    {
        var rend = who.GetComponentsInChildren<Renderer>();
        var rend1 = who.GetComponentInChildren<Image>();
        //var rend2 = who.GetComponent<ShapeRenderer>();

        float t = 1;

        if (rend1 != null)
        {
            while (t > 0)
            {
                if (rend1 == null) yield break;

                t -= Time.deltaTime * spd;
                rend1.color = new Color(1, t, t);
                yield return null;
            }

            while (t < 1)
            {
                if (rend1 == null) yield break;

                t += Time.deltaTime * spd;
                rend1.color = new Color(1, t, t);
                yield return null;
            }

            rend1.color = Color.white;   
        }
        else if (rend != null && rend.Length > 0)
        {
            while (t > 0)
            {
                if (rend == null || rend.Length == 0) yield break;

                t -= Time.deltaTime * spd;
                for (int i = 0; i < rend.Length; i++)
                    if (rend[i] != null) rend[i].material.color = new Color(1, t, t);
                yield return null;
            }

            while (t < 1)
            {
                if (rend == null) yield break;

                t += Time.deltaTime * spd;
                for (int i = 0; i < rend.Length; i++)
                    if (rend[i] != null) rend[i].material.color = new Color(1, t, t);
                yield return null;
            }

            for (int i = 0; i < rend.Length; i++)
                if (rend[i] != null) rend[i].material.color = Color.white;            
            
        }

        yield return null;
    }

    public IEnumerator BlinkRedASingle(GameObject who, float spd = 2)
    {
        var rend = who.GetComponentInChildren<Renderer>().material;
        //var rend1 = who.GetComponentInChildren<MeshRenderer>();

        float t = 1;

        while (t > 0)
        {
            if (rend == null) yield break;

            t -= Time.deltaTime * spd;
            rend.color = new Color(1, t, t);
            yield return null;
        }

        while (t < 1)
        {
            if (rend == null) yield break;

            t += Time.deltaTime * spd;
            rend.color = new Color(1, t, t);
            yield return null;
        }

        rend.color = Color.white;


        yield return null;
    }

    public static bool IsPointerOverUIElement()
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0 || EventSystem.current.IsPointerOverGameObject() ; //  results.Where(r => r.gameObject.layer == 5).Count() > 0;
    }

    public static int CalculateLayer(GameObject o, Transform overLo = null, Transform overHi = null)
    {
        float dm = 1;
        float dm1 = 0;

        if (overLo == null)
        {
            dm = MainStates.instance.maxT.position.y - MainStates.instance.minT.position.y;
            dm1 = MainStates.instance.maxT.position.y - o.transform.position.y;
        }
        else
        {
            dm = overHi.position.y - overLo.position.y;
            dm1 = overHi.position.y - o.transform.position.y;
        }

        float rat = dm1 / dm;

        int val = (int)(2 + rat * 1000);

        SortingGroup ff = o.GetComponent<SortingGroup>();
        if (ff == null) ff = o.AddComponent<SortingGroup>();
        ff.sortingOrder = val;
        /*
        var oo = o.GetComponentInChildren<SpriteRenderer>();
        if (oo != null) oo.sortingOrder = val;
        else
        {
            var oo1 = o.GetComponentInChildren<Renderer>();
            if (oo1 != null) oo1.sortingOrder = val;
        }
        */

        return val;
    }

    public void MakeAUnit(GameObject player, int hp, float atk, List<string> tags, List<string> allytags, List<string> enemytags,
        float healthTrack, float healthDlt, float nameLvlDlt, string nm, int lvl)
    {
        /*
        RObj plr = player.GetComponent<ObjHolder>().obj;
        //if (plr == null) plr = player.AddComponent<MonsterParams>();
        plr.iniPars.health = hp;
        plr.iniPars.max_health = hp;
        plr.iniPars.p_atk = atk;

        plr.tags.Clear();
        plr.tags = tags;

        plr.allyTags.Clear();
        plr.allyTags = allytags;

        plr.enemyTags.Clear();
        plr.enemyTags = enemytags;

        plr.RecalcParams();

        player.AddComponent<Deatho>();

        //
        EventManager.SYSTEM_ADD_UNIT.Invoke(new ArgPass { who = plr.gameObject, dlt = new Vector3(0, healthDlt, 0) });



        if (lvl > 0)
        {
            EventManager.SYSTEM_ADD_UNIT_AND_NAME.Invoke(new ArgPass { who = plr.gameObject, dlt = new Vector3(0, nameLvlDlt, 0), what = nm, num = lvl });
            //HealthSystem.instance.SetNameLevel(plr, new Vector3(0, nameLvlDlt, 0), lvl, nm);
        }

        player.AddComponent<BuffVisualController>();

        AssignPhysics(player, false);
        */

    }

    public void Rotato(Transform what)
    {
        StartCoroutine(RotatoA(what));
    }

    public IEnumerator RotatoA(Transform what)
    {
        
        yield return null;
    }
    
    public void Dovodka(Transform b, Action act, Vector3 end)
    {
        StartCoroutine(DovodkaA(b, act, end));
    }

    public IEnumerator DovodkaA(Transform b, Action act, Vector3 end)
    {
        //inDovodka = true;

        float spd = 2;
        float t = 0;

        Vector3 ini = Vector3.zero;
        if (b == null) ini = Camera.main.transform.forward;
        else ini = b.forward;
        
        while (t < 1)
        {
            t += Time.deltaTime * spd;
            
            if (b == null)
                Camera.main.transform.forward = ini * (1 - t) + end * t;
            else
                b.forward = ini * (1 - t) + end * t;    
            
            yield return null;
        }

        if (b == null)
            Camera.main.transform.forward = end;
        else
            b.forward = end;
        
        yield return null;

        //inDovodka = false;
        
        if (act != null)
        {
            act();
        }
    }

    public float GetMaxHeight(Transform root, float max, ref Transform res)
    {
        if (root.position.y > max)
        {
            max = root.position.y;
            res = root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var ff = GetMaxHeight(root.GetChild(i), max, ref res);
            if (ff > max)
            {
                max = ff;
            }
        }

        return max;
    }

    public static string Formatted(int val, bool formatted)
    {
        if (!formatted)
            return val.ToString();
        
        if (val > 1000)
        {
            float f = val / 1000.0f;
            return f.ToString("0.0") + "K";
        }
        else return val.ToString();
    }

    public List<Bon> GetItemsToBon(List<RObj> items)
    {
        var res = new List<Bon>();
        foreach (var v in items)
        {
            res.Add(new Bon { Key = v.dbObj.ID, Value = (int)v.GetPar("amount") });
        }
        return res;
    }
    
    public void DroppyLoot(GameObject who, string setName, Action<List<RObj>> addItems, string exactItem = "", bool ui = false, Transform ep = null, int cnt = 1, Action after = null)
    {
        List<RObj> ti = new List<RObj>();
        if (exactItem != "")
        {
            ti.Add(DatabaseAll.instance.CreateItem(exactItem, 1));
        }
        else
            ti = ModelSet.GetMeItems(setName);
        
    }

    public IEnumerator RandomParabolDrop(Transform b, float kx, float ky, Action after, float kSPD = 1)
    {

        float t = 0;
        float savedY = b.position.y;

        //b.GetComponentInChildren<Renderer>().sortingOrder = 0;
        b.localScale *= 2;

        var speed= new Vector2(UnityEngine.Random.Range(-2.9f, 2.9f) * kx, UnityEngine.Random.Range(1, 3f) * 5.0f * ky) * 2;

        while (true)
        {
            t += Time.deltaTime;
            b.position += new Vector3(speed.x, speed.y, 0) * Time.deltaTime * kSPD - 30.8f * new Vector3(0, t, 0) * Time.deltaTime * kSPD;
            if (b.position.y < savedY) break;
            yield return null;            
        }
        
        //destroy trail renderer
        yield return new WaitForSeconds(0.5f);
        Destroy(b.GetComponent<TrailRenderer>());

        //up and fade

        if (after == null && ConfigLoader.GetMetaParamValue("k_stay_type") == 1)
        {
            var tg = MainStates.instance.GetClosestPos(b.position);
            MainStates.instance.AddAsResource(b.gameObject);
        }
        else if (after == null)
        {
            StartCoroutine(FadeNUp(b, 1, 1, null, () =>
            {
                //EventManager.SYSTEM_SHOW_MESSAGE.Invoke(new ArgPass {what = "1", who = b.gameObject});
                EventManager.INV("SYSTEM_SHOW_MESSAGE", new ArgPass{what = "1", go = b.gameObject});
                //HealthSystem.instance.ShowMessage("1", b.gameObject, 0, Color.green);
                Destroy(b.gameObject);
            }));
        }
        else
        {
            after();
        }

        //message

    }


    public GameObject fadePanel;
    private bool inUlt = false;

    private void Update()
    {
        //Debug.Log(Time.time);
    }

    public void DoUlt(GameObject who)
    {
        if (inUlt) return;
        inUlt = true;

        //Debug.Break();
        who.GetComponent<Renderer>().sortingOrder = 1;
        /*
        FadeToColor(fadePanel.gameObject, new Color(0,0,0,1f), 0.1f, () =>
        {
            FadeToColor(fadePanel.gameObject, new Color(0,0,0,0f), 0.1f, () =>
            {
                inUlt = false;
                if (who != null)
                    who.GetComponent<Renderer>().sortingOrder = 0;
            });
        });
        */
        
        FadeToAlpha(fadePanel, 2, 0.6f, () =>
        {
            FadeToAlpha(fadePanel, 2, 0f, () =>
            {
                inUlt = false;
                if (who != null)
                    who.GetComponent<Renderer>().sortingOrder = 0;
            });
        });
    }

    public void FadeToAlpha(GameObject who, float spd, float alpha, Action act)
    {
        StartCoroutine(FadeToAlphaA(who, spd, alpha, act));
    }
    public IEnumerator FadeToAlphaA(GameObject who, float spd, float alpha, Action act)
    {
        var rend = who.GetComponent<Renderer>();
        float t = rend.material.color.a;

        if (t < alpha)
        {
            while (t < alpha)
            {
                t += Time.deltaTime * spd;
                rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, t);
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            while (t > alpha)
            {
                t -= Time.deltaTime * spd;
                rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, t);
                yield return new WaitForFixedUpdate();
            }
        }

        rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, alpha);

        if (act != null)
            act();
    }

    public void FadeCanvasG(CanvasGroup cg, float spd, float end, Action act = null)
    {
        StartCoroutine(FadeCanvasG1(cg, spd, end, act));
    }

    public IEnumerator FadeCanvasG1(CanvasGroup cg, float spd, float end, /*0 - 1*/ Action act = null)
    {
        if (cg.alpha < end)
        {
            while (cg.alpha < end)
            {
                cg.alpha += spd * Time.deltaTime;
                yield return null;
            }
        }
        else if (cg.alpha > end)
        {
            while (cg.alpha > end)
            {
                cg.alpha -= spd * Time.deltaTime;
                yield return null;
            }
        }

        cg.alpha = end;

        yield return null;

        if (act != null)
            act();
    }
    
    public void FadeToColor(GameObject who, Color endColor, float spd /* its time */, Action end = null)
    {
        StartCoroutine(FadeToColorA(who, endColor, spd, end));
    }

    public Color GetColor(GameObject who)
    {
        var rend = who.GetComponent<Renderer>();
        var rend1 = who.GetComponent<Image>();

        if (rend1) return rend1.color;
        return rend.material.color;
    }
    
    public IEnumerator FadeToColorA(GameObject who, Color endColor, float spd, Action end)
    {
        var rend = who.GetComponentInChildren<Renderer>();
        var rend0 = who.GetComponentInChildren<SpriteRenderer>();
        var rend1 = who.GetComponent<Image>();
        

        float t = 0;
        Vector4 iniClr = rend0 != null ? rend0.color : rend1 != null ? rend1.color : rend.material.color;
        Vector4 v = (Vector4)endColor - iniClr;
        

        while (t < 1 && (rend != null || rend1 != null || rend0 != null))
        {
            
            if (rend0 != null) rend0.color = iniClr + v * t;
            else if (rend != null) rend.material.color = iniClr + v * t;
            else if (rend1 != null) rend1.color = iniClr + v * t;
            
            
            t += Time.deltaTime * spd;
            yield return null;
        }

        if (rend) rend.material.color = endColor;
        if (rend1) rend1.color = endColor;
        //if (rend2) rend2.Color = endColor;

        if (end != null)
            end();
    }

    public IEnumerator FadeNUp(Transform who, float spd1, float spd2, AnimationCurve anim, Action act)
    {
        float t = 0;
        var aa = who.GetComponentInChildren<Renderer>();

        float savedX = who.position.x;
        
        if (who.GetComponent<TextMeshPro>() != null)
        {
            var hh = who.GetComponent<TextMeshPro>();
            while (hh.color.a > 0)
            {
                t += Time.deltaTime;
                hh.color = new Color(hh.color.r, hh.color.g, hh.color.b,
                    hh.color.a - spd2 * Time.deltaTime);
                hh.transform.position += new Vector3(0, spd1 * Time.deltaTime, 0);

                if (anim != null)
                {
                    hh.transform.position = new Vector3(savedX + anim.Evaluate(t/4), hh.transform.position.y,
                        hh.transform.position.z);
                }
                
                yield return null;
            }
        }
        else
        { 
            while (aa.material.color.a > 0)
            {
                t += Time.deltaTime;
                aa.material.color = new Color(aa.material.color.r, aa.material.color.g, aa.material.color.b,
                    aa.material.color.a - spd2 * Time.deltaTime);
                aa.transform.position += new Vector3(0, spd1 * Time.deltaTime, 0);
                
                if (anim != null)
                {
                    aa.transform.position = new Vector3(savedX + anim.Evaluate(t/4), aa.transform.position.y,
                        aa.transform.position.z);
                }
                
                yield return null;
            }

        }

    if (act != null) act();
    }
    public void SetDestination(Transform who, Vector3 endPos, Action act, float minDistAct)
    {
        StartCoroutine(SetDestinationA(who, endPos, act, minDistAct));
    }

    public static List<Bon> GetItemsFromString(string s)
    {
        var ss = s.Split('#');
        List<Bon> res = new List<Bon>();
        for (int i = 0; i < ss.Length; i++)
        {
            var b = ss[i].Split(',');
            res.Add(new Bon{Key = b[0], Value = int.Parse(b[1])});
        }

        return res;
    }
    
    public static List<Bon> GetBonsFromStringList(List<string> s)
    {
        List<Bon> res = new List<Bon>();
        for (int i = 0; i < s.Count; i++)
        {
            res.Add(new Bon{Key = s[i], Value = 1});
        }

        return res;
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    public IEnumerator SetDestinationA(Transform who, Vector3 endPos, Action act, float minDistAct)
    {
        who.GetComponent<NavMeshAgent>().SetDestination(endPos);

        while (true)
        {
            var dist = (who.position - endPos).magnitude;
            if (dist < minDistAct)
            {
                if (act != null)
                {
                    act();
                }
                yield break;
            }

            yield return null;
        }
    }


    Vector3 savedPos = Vector3.zero;
    Vector3 dragS0 = Vector3.zero;
    float lastDragT = -1;
    public bool isDrag = false;
    private bool isPressed = true;
    
    public void CameraGrab(Camera camera, float speed, ref bool drag, Transform lo, Transform hi, float szx, int btn)
    {
        drag = isDrag;
        if (IsPointerOverUIElement()) return;
        
        if (Input.GetMouseButtonDown(btn))
        {
            dragS0 = Input.mousePosition;
            savedPos = Vector3.zero;
            isDrag = false;
            isPressed = true;
        }

        if (!Input.GetMouseButton(btn)) return;

        if (Input.GetMouseButtonUp(btn))
        {
            savedPos = Vector3.zero;
            isPressed = false;
            isDrag = false;
            return;

        }
        
            if ((Input.mousePosition - dragS0).magnitude > 10 && !isDrag)
            {
                isDrag = true;
                lastDragT = Time.time;
                savedPos = dragS0;
            }
            else if (isDrag)
            {
                //
            }

            if (isDrag)
            {
                var pipi = (-Input.mousePosition + savedPos);
                camera.transform.position += new Vector3(pipi.x, pipi.y, 0) * Time.deltaTime * speed;
                savedPos = Input.mousePosition;

                if (lo != null && camera.transform.position.x - szx < lo.position.x)
                {
                    camera.transform.position = new Vector3(lo.position.x + szx, camera.transform.position.y, camera.transform.position.z);
                }
                if (hi != null && camera.transform.position.x + szx > hi.position.x)
                {
                    camera.transform.position = new Vector3(hi.position.x - szx, camera.transform.position.y, camera.transform.position.z);
                }
                //y
                if (lo != null && camera.transform.position.y - szx < lo.position.y)
                {
                    camera.transform.position = new Vector3(camera.transform.position.x, lo.position.y + szx, camera.transform.position.z);
                }
                if (hi != null && camera.transform.position.y + szx > hi.position.y)
                {
                    camera.transform.position = new Vector3(camera.transform.position.x, hi.position.y - szx, camera.transform.position.z);
                }
                
                
            }



    }

    public IEnumerator CameraZoom(Camera cam)
    {
        float endE = 4.7f;
        var vv = cam;
        var dlt = vv.orthographicSize - endE;
        while (vv.orthographicSize > endE)
        {
            if (vv.orthographicSize - dlt * Time.deltaTime < endE)
                break;
            vv.orthographicSize -= dlt * Time.deltaTime;
            yield return null;
        }

        vv.orthographicSize = endE;
    }
    public void CameraZoom(Camera camera, float lo, float hi, float speed)
    {
        if (!Application.isMobilePlatform)
        {

            if (Input.mouseScrollDelta.y != 0)
            {
                if (!camera.orthographic)
                {

                    camera.transform.position -= new Vector3(0, Input.mouseScrollDelta.y * speed, 0);
                    if (camera.transform.position.y < lo) camera.transform.position = new Vector3(camera.transform.position.x, lo, camera.transform.position.z);
                    if (camera.transform.position.y > hi) camera.transform.position = new Vector3(camera.transform.position.x, hi, camera.transform.position.z);

                }
                else
                {
                    camera.orthographicSize -= Input.mouseScrollDelta.y;

                    if (camera.orthographicSize < lo) camera.orthographicSize = lo;
                    if (camera.orthographicSize > hi) camera.orthographicSize = hi;
                }

            }
            else
            {
                if (Input.touchSupported)
                {
                    // Pinch to zoom
                    if (Input.touchCount == 2)
                    {

                        // get current touch positions
                        Touch tZero = Input.GetTouch(0);
                        Touch tOne = Input.GetTouch(1);
                        // get touch position from the previous frame
                        Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
                        Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

                        float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
                        float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

                        // get offset value
                        float deltaDistance = oldTouchDistance - currentTouchDistance;

                        camera.fieldOfView += deltaDistance * speed;
                        // set min and max value of Clamp function upon your requirement
                        camera.fieldOfView = Mathf.Clamp(camera.fieldOfView, lo, hi);
                        //Zoom(deltaDistance, speed);


                    }


                    if (camera.fieldOfView < lo)
                    {
                        camera.fieldOfView = lo;
                    }
                    else
                    if (camera.fieldOfView > hi)
                    {
                        camera.fieldOfView = hi;
                    }
                }
            }

        }
    }

    public void ShowOutline(GameObject who, bool val, Color clt, float wid)
    {
        if (val)
        {
            who.AddComponent<Biba.Outline>();
            who.GetComponent<Biba.Outline>().OutlineColor = clt;
            who.GetComponent<Biba.Outline>().OutlineWidth = wid;
        }
        else
        {
            Destroy(who.GetComponent<Biba.Outline>());
        }
    }

    public void DragReleasedUnit(List<Transform> points, Transform who)
    {
        float min = 1e+10f;
        int l = -1;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = (who.position - points[i].position).magnitude;
            if (dist < min)
            {
                min = dist;
                l = i;
            }
        }
        //
        if (points[l].childCount > 0)
        {
            if (points[l].GetChild(0) == who)
            {
                who.transform.localPosition = Vector3.zero;
            }
            else
            {
                points[l].GetChild(0).SetParent(who.parent);
                var pr = who.parent;
                who.parent = points[l];
                who.localPosition = Vector3.zero;

                pr.GetChild(0).localPosition = Vector3.zero;
            }
        }
        else
        {
            who.SetParent(points[l]);
            who.localPosition = Vector3.zero;
        }
    }


    public void ScaleFun(Transform who, float spd = 5)
    {
        StartCoroutine(ScaleFunA(who, spd));
    }

    public IEnumerator ScaleFunA(Transform who, float spd)
    {
        float t = 0;
        while (t < 1)
        {
            float b = Time.deltaTime * spd;
            t += Time.deltaTime * spd;
            if (t > 1) b = 1 - (t - Time.deltaTime * spd);
            who.localScale += new Vector3(b/5, b/5, b/5);
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            float b = Time.deltaTime * spd;
            t += Time.deltaTime * spd;
            if (t > 1) b = 1 - (t - Time.deltaTime * spd);
            who.localScale -= new Vector3(b/5, b/5, b/5);
            yield return null;
        }
    }

    public void ParabolikTravel(Transform who, Transform where, Sprite icon, Transform container, float spd,
        Action after = null, bool useSelfCopy = false, bool useCamCast = true, bool withAtwist = false,
        AnimationCurve scaleCurve = null, bool useSelf = false, bool useCamWorld = false, int overSort = -1)
    {
        GameObject go = null;

        if (useSelf)
        {
            go = who.gameObject;
            //go.transform.parent = container;
        }
        else if (!useSelfCopy)
        {
            go = (GameObject) Instantiate(emptyImg, container);
            go.GetComponent<Image>().sprite = icon;
        }
        else
        {
            go = (GameObject) Instantiate(who.gameObject, container);
        }

        go.name = "travi";
        go.transform.position = who.position;
        if (overSort >= 0)
            go.AddComponent<SortingGroup>().sortingOrder = overSort;
        
        if (useCamCast) go.transform.position = Camera.main.WorldToScreenPoint(who.position);

        if (withAtwist)
        {
            StartCoroutine(TravelByRandDeccel(go.transform, spd*2, 1 + Time.deltaTime,
                UnityEngine.Random.Range(0.25f, 0.8f),
                () => StartCoroutine(ParabolikTravelA(go.transform, where, spd, after, scaleCurve))
            ));
        }
        else
        {
            StartCoroutine(ParabolikTravelA(go.transform, where, spd, after, scaleCurve));            
        }

    }

    public IEnumerator TravelByRandDeccel(Transform who, float spd, float div, float tm, Action after = null)
    {
        float t = 0;
        Vector2 dir = new Vector2(UnityEngine.Random.Range(-0.99f, 0.99f), UnityEngine.Random.Range(-0.99f, 0.99f));
        dir.Normalize();
        while (t < tm)
        {
            who.transform.position += new Vector3(dir.x, dir.y, 0) * spd * Time.deltaTime;
            spd /= div;
            yield return null;
            t += Time.deltaTime;
        }

        if (after != null)
        {
            after();
        }
    }
    public IEnumerator ParabolikTravelA(Transform who, Transform where, float spd, Action after, AnimationCurve scaleCurve)
    {
        var dir = where.position - who.position;
        dir.z = 0;
        Vector3 savedIni = who.position;

        Vector3 fn = new Vector3(-dir.y, dir.x, 0);
        fn.Normalize();

        Vector3 savedScale = who.transform.localScale;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * spd;
            who.transform.position = savedIni + dir * t + fn * parabolicCurve.Evaluate(t) * 400;

            if (scaleCurve != null)
                who.transform.localScale =
                    new Vector3(savedScale.x * scaleCurve.Evaluate(t), savedScale.y * scaleCurve.Evaluate(t), savedScale.z * scaleCurve.Evaluate(t));    
                
            yield return null;
        }

        yield return null;

        if (after != null)
        {
            after();
        }

        Destroy(who.gameObject);
    }
    
    public static GameObject FindPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }

    public static GameObject GetMeClosest(GameObject[] objs, Vector3 ver, out float dst, string tg = "")
    {
        if (tg != "")
            objs = GameObject.FindGameObjectsWithTag(tg);
        
        GameObject ans = null;
        float min = 1e+10f;
        dst = min;

        for (int i = 0; i < objs.Length; i++)
        {
            var dr = objs[i].transform.position - ver;
            if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
                dr.y = 0;
            else if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
                dr.z = 0;
                
            if (dr.magnitude < min)
            {
                min = dr.magnitude;
                dst = min;
                ans = objs[i];
            }
        }

        return ans;
    }
    
    public static GameObject GetMeClosest<T>(List<T> objs, Vector3 ver, bool camCast, out float dst, List<float> weights = null) where T : MonoBehaviour
    {
        GameObject ans = null;
        float min = 1e+10f;
        dst = min;

        for (int i = 0; i < objs.Count; i++)
        {
            var dr = objs[i].transform.position - ver;
            //Debug.Log("KK" + objs[i].transform.position);
            if (camCast)
            {
                var jj = Camera.main.WorldToScreenPoint(objs[i].transform.position);
                dr = jj - ver;
            }

            float dd = dr.magnitude;
            if (weights != null)
            {
                dd = dr.x * dr.x * weights[0] + dr.y * dr.y * weights[1] + dr.z * dr.z * weights[2];
                dd = Mathf.Sqrt(dd);
            }
            
            if (dd < min)
            {
                min = dd;
                dst = min;
                ans = objs[i].gameObject;
            }
        }

        return ans;
    }
    public static GameObject GetMeClosest(List<GameObject> objs, Vector3 ver, out float dst)
    {
        GameObject ans = null;
        float min = 1e+10f;
        dst = min;

        for (int i = 0; i < objs.Count; i++)
        {
            var dr = objs[i].transform.position - ver;
            if (dr.magnitude < min)
            {
                min = dr.magnitude;
                dst = min;
                ans = objs[i];
            }
        }

        return ans;
    }
    
    public static GameObject GetMeClosest(List<RObj> objs, Vector3 ver, out float dst)
    {
        GameObject ans = null;
        float min = 1e+10f;
        dst = min;

        for (int i = 0; i < objs.Count; i++)
        {
            var dr = objs[i].Position - ver;
            if (dr.magnitude < min)
            {
                min = dr.magnitude;
                dst = min;
                ans = objs[i].main;
            }
        }

        return ans;
    }

    public static int GetMeClosestByY(Transform root, Vector3 ver, out float dst, Func<Transform, bool> check = null, bool zeroZ = true)
    {
        int ans = -1;
        float min = 1e+10f;

        for (int i = 0; i < root.childCount; i++)
        {
            if (check != null)
            {
                if (!check(root.GetChild(i))) continue;
            }
            
            var dir = root.GetChild(i).position - ver;
            
            if (zeroZ)
                dir.z = 0;
            
            if (dir.magnitude < min)
            {
                min = dir.magnitude;
                ans = i;
            }
        }

        dst = min;
        return ans;
    }

    public static GameObject GetMeClosestByMany(Transform root, Vector3 ver, out float dst,
        Func<Transform, bool> check = null)
    {
        List<Transform> tt = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
            tt.Add(root.GetChild(i));
        
        return GetMeClosestByMany(tt, ver, out dst, check);
    }
    
    public static GameObject GetMeClosestByMany(List<Transform> root, Vector3 ver, out float dst, Func<Transform, bool> check = null)
    {
        GameObject ans = null;
        float min = 1e+10f;
        bool zeroZ = ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0;

        //for (int i = 0; i < root.Count; i++)
        //    Debug.Log(root[i].name);
            
        for (int j = 0; j < root.Count; j++)
        {
            for (int i = 0; i < root[j].childCount; i++)
            {
                if (check != null)
                {
                    if (!check(root[j].GetChild(i))) continue;
                }

                var dir = root[j].GetChild(i).position - ver;

                if (zeroZ)
                    dir.z = 0;

                if (dir.magnitude < min)
                {
                    min = dir.magnitude;
                    ans = root[j].GetChild(i).gameObject;
                }
            }
        }

        dst = min;
        return ans;
    }
    
    
    
    
    public void CameraMove(Transform target, Action act)
    {
        StartCoroutine(CameraMoveA(target, act));
    }

    public IEnumerator CameraMoveA(Transform target, Action act)
    {

        //save position
        //save localEulerAngles

        float t = 0;
        var vv = Camera.main.transform;
        Vector3 ini = vv.position;
        Vector3 end = target.position - target.right * 25 + new Vector3(0, 3, 0);

        while (t < 1)
        {
            t += Time.deltaTime;
            vv.position = ini + (end - ini) * t;

            Vector3 newDirection = Vector3.RotateTowards(vv.forward, target.position - vv.position, 2 * Time.deltaTime, 0);
            vv.forward = target.position + new Vector3(0, -1, 0) - vv.position;
            //vv.forward = newDirection;
            //vv.rotation = Quaternion.LookRotation(newDirection);

            yield return null;
        }

        vv.position = end;
        
        if (act != null)
        {
            act();
        }

    }


    public Vector3 savedPosCam = Vector3.zero;
    public Vector3 savedCamForw = Vector3.zero;
    public bool inMoveCam = false;

    public void CheckCamBack()
    {
        if (inMoveCam)
        {
            inMoveCam = false;
            CameraMoveT(savedPosCam, savedCamForw, 1, () =>
            {
                inMoveCam = false;
            });
        }
    }
    public void CameraMoveT(Vector3 ep, Vector3 epR, float spd, Action act)
    {
        StartCoroutine(CameraMoveTA(ep, epR, spd, act));
    }

    public IEnumerator CameraMoveTA(Vector3 ep, Vector3 epR, float spd, Action act)
    {
        if (inMoveCam) yield break;
        inMoveCam = true;

        savedPosCam = Camera.main.transform.position;
        savedCamForw = Camera.main.transform.forward;
        
        float t = 0;
        while (t < 1)
        {
            Camera.main.transform.position = savedPosCam * (1 - t) + t * ep;
            Camera.main.transform.forward = savedCamForw * (1 - t) + t * epR;
            t += Time.deltaTime * spd;
            yield return null;
        }
        
        Camera.main.transform.position = ep;
        Camera.main.transform.forward = epR;
        
        
        yield return null;

        if (act != null)
            act();
    }
    
    //parabolik drop
    private float height = 1f;              //5, 10, 2
    private float distance = 1f;
    private float duration = 0.5f;
    
    [ContextMenu("Start Random Drop")]
    public void StartRandomDrop(Transform who, RObj a, Action end, Transform instigator)
    {
        StartCoroutine(ParabolicDrop(who, a, end,  instigator));
    }
    
    IEnumerator ParabolicDrop(Transform who, RObj a, Action end, Transform instigator)
    {
        var sg = who.gameObject.AddComponent<SortingGroup>();
        sg.sortingOrder = -20;
        
        Vector3 startPos = who.position;
        Vector3 randomDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        Vector3 targetPos = startPos + randomDir * distance;
        
        float elapsedTime = 0;
        //stay z ? bads thwbbb
        float STAY_Z = MainStates.instance.SCENE_Z;
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            targetPos.z = STAY_Z;
        else STAY_Z = startPos.z;

        var tt = instigator.GetComponentInChildren<PointsControl>();
        Vector3 endPos = who.position;
        if (tt != null && tt.loPoint != null) endPos = tt.loPoint.position;
        var dY = endPos.y - startPos.y;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            
            // Horizontal movement
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Parabolic height (peaks at middle)
            float yOffset = 4f * height * (t - t * t) + dY * t;
            
            who.position = new Vector3(horizontalPos.x, startPos.y + yOffset, STAY_Z/*horizontalPos.z*/);
            who.transform.Rotate(0,0,90* Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (end != null) end();
         who.position = endPos;
        
        if (ConfigLoader.GetMetaParamValue("autotake_drop") == 1)
        {
            MainStates.instance.PickDrop(a);
        }
    }
    
    
    
}


public static class Cloner
{
    public static T Clone<T>(T source)
    {
        if (ReferenceEquals(source, null))
            return default(T);

        var settings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };

        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, settings), settings);
    }

    class ContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => base.CreateProperty(p, memberSerialization))
                .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(f => base.CreateProperty(f, memberSerialization)))
                .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
        }
    }
}