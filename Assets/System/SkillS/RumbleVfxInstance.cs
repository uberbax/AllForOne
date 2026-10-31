using UnityEngine;

public sealed class RumbleVfxInstance : MonoBehaviour
{
    [SerializeField]private GameObject prefab;
    private ParticleSystem[] systems;
    private LineRenderer[] lines;
    private float[] lineWidths;
    private Transform follow;
    private Vector3 offset;
    private float endTime,releaseTime,tail;
    private bool stopping;
    public int Generation { get; private set; }
    private void OnEnable(){if(systems==null)Initialize(prefab);}
    internal void Initialize(GameObject source)
    {
        prefab=source;systems=GetComponentsInChildren<ParticleSystem>(true);tail=.15f;
        lines=GetComponentsInChildren<LineRenderer>(true);lineWidths=new float[lines.Length];
        for(int i=0;i<lines.Length;i++)lineWidths[i]=lines[i].widthMultiplier;
        foreach(var system in systems)
        {
            var lifetime=system.main.startLifetime;
            for(int i=0;i<=4;i++)tail=Mathf.Max(tail,lifetime.Evaluate(i/4f,1));
        }
        tail=Mathf.Min(tail,1.5f);
    }
    internal void Activate(Vector3 position,Quaternion rotation,float radius,float duration,Transform target)
    {
        Generation++;follow=target;offset=target!=null?position-target.position:Vector3.zero;stopping=false;
        transform.SetPositionAndRotation(position,rotation);transform.localScale=Vector3.one*radius;
        endTime=duration<0?float.PositiveInfinity:Time.time+Mathf.Max(.05f,duration);
        gameObject.SetActive(true);
        for(int i=0;i<lines.Length;i++){lines[i].widthMultiplier=lineWidths[i];lines[i].startColor=lines[i].endColor=Color.white;}
        foreach(var system in systems)system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach(var system in systems)if(system.main.playOnAwake)system.Play(false);
    }
    public void Stop()
    {
        if(stopping)return;
        stopping=true;follow=null;releaseTime=Time.time+tail;
        foreach(var system in systems)if(system!=null)system.Stop(false,ParticleSystemStopBehavior.StopEmitting);
    }
    internal void SetBeam(float width,float length)
    {
        transform.localScale=new Vector3(1,1,Mathf.Max(.05f,length));
        // Pack textures include a wide transparent glow around the narrow bright core.
        foreach(var line in lines)line.widthMultiplier=Mathf.Max(.01f,width)*5;
    }
    private void LateUpdate()
    {
        if(!stopping)
        {
            if(follow!=null)transform.position=follow.position+offset;
            if(Time.time>=endTime)Stop();
        }
        else
        {
            var color=Color.white;color.a=Mathf.Clamp01((releaseTime-Time.time)/tail);
            foreach(var line in lines)line.startColor=line.endColor=color;
            if(Time.time>=releaseTime)RumbleVfx.Return(this,prefab);
        }
    }
    private void OnDestroy(){RumbleVfx.Lost(prefab);}
}
