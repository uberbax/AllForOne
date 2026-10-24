#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class PlagueSceneAudit
{
    [InitializeOnLoadMethod] static void Init() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        const string flag = "Assets/System/Effects/Editor/.plague-audit";
        if (!File.Exists(flag)) return;
        File.Delete(flag);
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-scene.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {
            playing=EditorApplication.isPlaying,
            scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
            cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new{c.name,c.orthographic,size=c.orthographicSize,pos=c.transform.position.ToString(),rot=c.transform.eulerAngles.ToString()}).ToArray(),
            ground=renderers.Where(r=> !(r is ParticleSystemRenderer)).GroupBy(r=>r.GetType().Name+"/"+r.sortingOrder+"/"+r.sharedMaterial?.renderQueue).Select(g=>new{key=g.Key,count=g.Count(),samples=g.Take(4).Select(r=>new{r.name,pos=r.transform.position.ToString(),bounds=r.bounds.ToString()}).ToArray()}).Take(40).ToArray()
        },Newtonsoft.Json.Formatting.Indented));
    }
}
#endif

