using System.Collections.Generic;
using UnityEngine;

/// <summary>Reusable visual-only particles. No damage, projectile collision or source-package demo scripts.</summary>
public static class RumbleVfx
{
    public readonly struct Lease
    {
        private readonly RumbleVfxInstance instance;
        private readonly int generation;
        internal Lease(RumbleVfxInstance value){instance=value;generation=value.Generation;}
        public bool IsAlive=>instance!=null&&instance.Generation==generation&&instance.gameObject.activeSelf;
        public Transform Transform=>IsAlive?instance.transform:null;
        public void Stop(){if(IsAlive)instance.Stop();}
        public void SetBeam(float width,float length){if(IsAlive)instance.SetBeam(width,length);}
    }
    private static RumbleVfxLibrary library;
    private static Transform poolRoot;
    private static readonly Dictionary<GameObject,Stack<RumbleVfxInstance>> pool=new Dictionary<GameObject,Stack<RumbleVfxInstance>>();
    private static readonly Dictionary<GameObject,int> liveCounts=new Dictionary<GameObject,int>();
    private const int MaxPerPrefab=12;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){library=null;poolRoot=null;pool.Clear();liveCounts.Clear();}
    private static void EnsureRoot()
    {
        if(poolRoot!=null)return;
        // A scene can be closed while static state survives with domain reload disabled.
        pool.Clear();liveCounts.Clear();
        var root=new GameObject("Rumble VFX Pool");poolRoot=root.transform;
        if(Application.isPlaying)Object.DontDestroyOnLoad(root);
        else root.hideFlags=HideFlags.HideAndDontSave;
    }
    public static Lease Play(string id,RumbleVfxLibrary.Stage stage,Vector3 position,float radius,float duration,
        Vector3 direction=default,Transform follow=null)
    {
        if(library==null)library=Resources.Load<RumbleVfxLibrary>(RumbleVfxLibrary.ResourcePath);
        var prefab=library!=null?library.Get(id,stage):null;
        if(prefab==null)return default;
        EnsureRoot();
        if(!pool.TryGetValue(prefab,out var available))pool[prefab]=available=new Stack<RumbleVfxInstance>();
        RumbleVfxInstance instance=null;
        while(available.Count>0&&instance==null)instance=available.Pop();
        if(instance==null)
        {
            liveCounts.TryGetValue(prefab,out int count);if(count>=MaxPerPrefab)return default;
            var go=Object.Instantiate(prefab,poolRoot,false);go.SetActive(false);
            instance=go.GetComponent<RumbleVfxInstance>();if(instance==null)instance=go.AddComponent<RumbleVfxInstance>();
            instance.Initialize(prefab);liveCounts[prefab]=count+1;
        }
        Quaternion rotation=Quaternion.identity;
        if(stage==RumbleVfxLibrary.Stage.Projectile||stage==RumbleVfxLibrary.Stage.Beam||
            (stage==RumbleVfxLibrary.Stage.Cast&&id!="tranquility"&&id!="healing_strike"))
            if(direction.sqrMagnitude>.0001f)rotation=Quaternion.LookRotation(direction.normalized,MainStates.is3D?Vector3.up:Vector3.forward);
        if(stage==RumbleVfxLibrary.Stage.Cast&&id=="healing_strike"&&!MainStates.is3D)
            rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        instance.Activate(position,rotation,Mathf.Max(.05f,radius),duration,follow);
        return new Lease(instance);
    }
    internal static void Return(RumbleVfxInstance instance,GameObject prefab)
    {
        if(poolRoot==null||prefab==null)
        {if(Application.isPlaying)Object.Destroy(instance.gameObject);else Object.DestroyImmediate(instance.gameObject);return;}
        instance.transform.SetParent(poolRoot,false);instance.gameObject.SetActive(false);
        if(pool.TryGetValue(prefab,out var available))available.Push(instance);
    }
    internal static void Lost(GameObject prefab)
    {
        if(prefab!=null&&liveCounts.TryGetValue(prefab,out int count))liveCounts[prefab]=Mathf.Max(0,count-1);
    }
}
