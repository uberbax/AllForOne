using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModelSet : MonoBehaviour
{
    public string name = "set0";
    // Start is called before the first frame update
    public List<Seto> items = new List<Seto>();

    public string reso = "none";
    
    public static List<RObj> GetMeItems(string setName)
    {
        var tt = ConfigLoader.Instance.dictSets[setName];
        //var t0 = tt.Find(x => x.name == setName);
        return GetMeItems(tt);
    }

    public static List<Bon> GetMeItemsBon(string setName)
    {
        var tt = ConfigLoader.Instance.dictSets[setName];
        //var t0 = tt.Find(x => x.name == setName);
        return GetMeItemsBon(tt);
    }

    public static List<Bon> GetMeItemsRefined(List<Bon> someItems, int mult = 1)
    {
        List<Bon> res = new List<Bon>();

        for (int i = 0; i < mult; i++)
            foreach (var v in someItems)
            {
                List<Bon> ado = new List<Bon>();
                if (v.Key.IndexOf("set_") >= 0)
                {
                    var bb = GetMeItemsBon(v.Key.Replace("set_", ""));
                    ado.AddRange(bb);
                }
                else
                {
                    ado.Add(new Bon{Key = v.Key, Value = v.Value});
                }
                //
                res.AddRange(ado);
                
            }

        return res;
    }

    public static Dictionary<string, List<RObj>> calcSets = new Dictionary<string, List<RObj>>();
    public static List<RObj> GetMeItemsAll(string setName)
    {
        if (calcSets.ContainsKey(setName))
        {
            return calcSets[setName];
        }
        
        var tt = DatabaseAll.instance.GetComponentsInChildren<ModelSet>().ToList();
        var t0 = tt.Find(x => x.name == setName);
        calcSets.Add(setName, GetMeItemsAll(t0));
        return calcSets[setName];
    }

    public static List<RObj> GetMeItemsAll(ModelSet ms)
    {
        List<RObj> ans = new List<RObj>();
        
        for (int i = 0; i < ms.items.Count; i++)
        {
            //Debug.Log(ms.items[i].item);
            var mn = DatabaseAll.instance.CreateItem(ms.items[i].item, 1);
            mn.SetPar("amount", ms.items[i].amount1);
            mn.SetPar("amount2", ms.items[i].amount2);
            ans.Add(mn);
        }

        return ans;
    }

    public static List<RObj> GetMeItems(List<Seto> ms)
    {
        //det max group
        int max = ms.Max(x => x.group);
        List<RObj> ans = new List<RObj>();
        

        for (int i = 0; i <= max; i++)
        {
            var tt = ms.FindAll(x => x.group == i);
            if (tt.Count == 0) continue;
            var pp = tt.Sum(x => x.weight);
            float y = UnityEngine.Random.Range(0, 100);
            int l = -1;
            float sum = 0;
            for (int j = 0; j < tt.Count; j++)
            {
                if (y < sum + tt[j].weight)
                {
                    l = j;
                    break;
                }

                sum += tt[j].weight;
            }
            //
            //over probability
            if (l < 0) continue;
            int um = UnityEngine.Random.Range(tt[l].amount1, tt[l].amount2);
            if (tt[l].amount1 >= tt[l].amount2) um = tt[l].amount1;
            //ok, generate amount of items
            tt[l].item = tt[l].item.Replace(" ", "");
            //either monster either item

            var mn = DatabaseAll.instance.CreateItem(tt[l].item, um);
            
            ans.Add(mn);


        }

        return ans;
    }

    public static int GetMeNum(List<float> weights)
    {
        var pp = weights.Sum(x => x);
        float y = UnityEngine.Random.Range(0, 100);
        
        int l = -1;
        float sum = 0;
        for (int j = 0; j < weights.Count; j++)
        {
            if (y < sum + weights[j])
            {
                l = j;
                break;
            }

            sum += weights[j];
        }
        //
        //over probability
        return l;
    }
    
    public static List<Bon> GetMeItemsBon(List<Seto> ms)
    {
        //det max group
        int max = ms.Max(x => x.group);
        List<Bon> ans = new List<Bon>();
        

        for (int i = 0; i <= max; i++)
        {
            var tt = ms.FindAll(x => x.group == i);
            if (tt.Count == 0) continue;
            var pp = tt.Sum(x => x.weight);
            float y = UnityEngine.Random.Range(0, 100);
            int l = -1;
            float sum = 0;
            for (int j = 0; j < tt.Count; j++)
            {
                if (y < sum + tt[j].weight)
                {
                    l = j;
                    break;
                }

                sum += tt[j].weight;
            }
            //
            //over probability
            if (l < 0) continue;
            int um = UnityEngine.Random.Range(tt[l].amount1, tt[l].amount2);
            if (tt[l].amount1 >= tt[l].amount2) um = tt[l].amount1;
            //ok, generate amount of items
            tt[l].item = tt[l].item.Replace(" ", "");
            //either monster either item

            //trying to get set from

            Bon mn = new Bon();
            mn.Key = tt[l].item;
            mn.Value = um;
            ans.Add(mn);


        }

        return ans;
    }
    public static List<string> GetMeAllStrings(string setName)
    {
        List<string> ans = new List<string>();
        var tt1 = DatabaseAll.instance.GetComponentsInChildren<ModelSet>().ToList();
        var ms = tt1.Find(x => x.name == setName);

        foreach (var v in ms.items)
        {
            ans.Add(v.item);
        }

        return ans;
    }
    
    public static List<Bon> GetMePossibleItems(string setName)
    {
        List<Bon> ans = new List<Bon>();

        if (setName == "")
            return ans;
        
        var tt = ConfigLoader.Instance.dictSets[setName];

        foreach (var v1 in tt)
        {
            ans.Add(new Bon{Key = v1.item, Value = v1.amount1});
        }

        return ans;
    }

    public static List<string> GetMeNonRepeat(List<string> lst, int num)
    {
        List<string> res = new List<string>();
        var g = new List<string>(lst);
        for (int i = 0; i < num; i++)
        {
            var p = g[Random.Range(0, g.Count)];
            res.Add(p);
            g.Remove(p);
        }

        return res;
    }

    public static List<string> GetMeStrings(string setName)
    {
        var tt1 = DatabaseAll.instance.GetComponentsInChildren<ModelSet>().ToList();
        var ms = tt1.Find(x => x.name == setName);
        //det max group
        int max = ms.items.Max(x => x.group);
        List<string> ans = new List<string>();

        for (int i = 0; i <= max; i++)
        {
            var tt = ms.items.FindAll(x => x.group == i);
            if (tt.Count == 0) continue;
            var pp = tt.Sum(x => x.weight);
            float y = UnityEngine.Random.Range(0, 100);
            int l = -1;
            float sum = 0;
            for (int j = 0; j < tt.Count; j++)
            {
                if (y < sum + tt[j].weight)
                {
                    l = j;
                    break;
                }

                sum += tt[j].weight;
            }
            //
            //over probability
            if (l < 0) continue;
            int um = UnityEngine.Random.Range(tt[l].amount1, tt[l].amount2);
            if (tt[l].amount1 >= tt[l].amount2) um = tt[l].amount1;
            //ok, generate amount of items
            tt[l].item = tt[l].item.Replace(" ", "");
            //either monster either item

            //trying to get set from
            Debug.Log("Trying get set from : " + tt[l].item);
            ans.Add(tt[l].item);


        }

        return ans;
    }

}

