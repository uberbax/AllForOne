#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class PlagueTreePlayCheck
{
    const string Flag="Assets/System/Effects/Editor/.plague-tree-check";
    const string Folder="Assets/System/Effects/PlagueTrail";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && File.Exists(Flag)) { File.Delete(Flag); Run(); }
        };
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    static async void Run()
    {
        GameObject root=null,tree=null,far=null,proxy=null;
        try {
            await Task.Delay(1500);
            var catalog=AssetDatabase.LoadAssetAtPath<PlagueTreeCatalog>(Folder+"/TreeCatalog.asset");
            Require(catalog!=null && catalog.replacements.Count>0,"Missing catalog");
            var source=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .First(r=>catalog.replacements.Any(p=>p.living==r.sprite));
            var pair=catalog.replacements.First(p=>p.living==source.sprite);
            Vector3 origin=new Vector3(20000,20000,0);
            tree=new GameObject("Temporary infected tree",typeof(SpriteRenderer),typeof(BoxCollider2D));
            var renderer=tree.GetComponent<SpriteRenderer>(); renderer.sprite=pair.living; renderer.sharedMaterial=source.sharedMaterial;
            renderer.color=new Color(.85f,.9f,1f,.8f); renderer.flipX=true; renderer.sortingOrder=7;
            Bounds bounds=pair.living.bounds;
            tree.transform.position=origin-new Vector3(-bounds.center.x,bounds.min.y+bounds.size.y*.025f,0);
            tree.transform.localScale=new Vector3(1.25f,.85f,1f);
            Vector3 originalPosition=tree.transform.position;
            Bounds originalWorldBounds=renderer.bounds;
            var collider=tree.GetComponent<BoxCollider2D>(); Vector2 colliderSize=collider.size; collider.offset=Vector2.one*100;
            far=UnityEngine.Object.Instantiate(tree); far.name="Temporary untouched tree"; far.transform.position+=Vector3.right*10;
            proxy=new GameObject("Temporary tree path target"); proxy.transform.position=origin+Vector3.left*.8f;
            root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PlagueTrail.prefab"));
            var trail=root.GetComponent<PlagueTrail>(); var infection=root.GetComponent<PlagueTreeInfection>();
            Require(infection!=null,"Infection component missing from prefab");
            var fields=new SerializedObject(trail); fields.FindProperty("target").objectReferenceValue=proxy.transform; fields.ApplyModifiedPropertiesWithoutUndo();
            await Task.Delay(120);
            Require(renderer.sprite==pair.living && infection.InfectedTreeCount==0,"Stationary trail infected tree");
            Capture(renderer,origin,"before");
            for(int i=1;i<=8;i++) { proxy.transform.position=origin+Vector3.right*(-.8f+i*.2f); await Task.Delay(35); }
            await Task.Delay(250);
            Require(infection.InfectedTreeCount==0 && renderer.sprite==pair.living,"Plague-only contact withered a tree");
            Physics2D.SyncTransforms();
            infection.SendMessage("CheckPlayerContact",origin+Vector3.right*8);
            Require(infection.InfectedTreeCount==0,"Distant player contact withered a tree");
            Vector3 localFoot=new Vector3(-bounds.center.x,bounds.min.y+bounds.size.y*.025f,0);
            Vector3 foot=tree.transform.TransformPoint(localFoot);
            infection.SendMessage("CheckPlayerContact",foot+Vector3.right*1.7f);
            Require(infection.InfectedTreeCount==0,"Tree outside distance radius withered");
            infection.SendMessage("CheckPlayerContact",foot+Vector3.right*1.25f);
            await Task.Delay(250);
            Require(infection.InfectedTreeCount==1,"Tree not infected or duplicate infection: "+infection.InfectedTreeCount);
            Require(renderer.sprite==pair.living && renderer.color.a<.8f && renderer.color.a>0,"Crossfade not gradual");
            Require(renderer.GetComponentsInChildren<SpriteRenderer>().Any(r=>r!=renderer && r.sprite==pair.dead && r.color.a>0),"Dead sprite not fading in");
            var ghost=renderer.GetComponentsInChildren<SpriteRenderer>().First(r=>r!=renderer);
            Require(Vector3.Distance(ghost.bounds.size,originalWorldBounds.size)<.005f && Vector3.Distance(ghost.bounds.center,originalWorldBounds.center)<.005f,"Fade renderer bounds differ: "+ghost.bounds+" vs "+originalWorldBounds);
            Capture(renderer,origin,"during");
            await Task.Delay(1400);
            Require(renderer.sprite==pair.dead && renderer.sprite.name.StartsWith("Dead_"),"Final dead sprite");
            Require(Mathf.Abs(renderer.color.a-.8f)<.001f && renderer.flipX && renderer.sortingOrder==7,"Renderer settings not preserved");
            Capture(renderer,origin,"after");
            var properties=new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
            Vector4 affine=properties.GetVector("_PlagueTransform");
            Require(Mathf.Abs(renderer.sprite.bounds.size.x*affine.x-bounds.size.x)<.001f
                && Mathf.Abs(renderer.sprite.bounds.size.y*affine.y-bounds.size.y)<.001f,"Visible geometry size differs");
            Require(Vector3.Distance(renderer.bounds.size,originalWorldBounds.size)<.005f && Vector3.Distance(renderer.bounds.center,originalWorldBounds.center)<.005f,"Final bounds differ: "+renderer.bounds+" vs "+originalWorldBounds);
            Require(tree.transform.position==originalPosition && collider.size==colliderSize && collider.enabled,"Transform or collider changed");
            Require(far.GetComponent<SpriteRenderer>().sprite==pair.living,"Uncontacted tree infected");
            Require(renderer.GetComponentsInChildren<SpriteRenderer>().Length==1,"Fade overlay leaked");
            trail.ClearTrail(); await Task.Delay(100);
            Require(renderer.sprite==pair.dead,"Clearing plague revived tree");
            Capture(renderer,origin,"after");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-tree-check.txt"),"PASS: prefab wired; "+catalog.replacements.Count+" mappings; plague alone ignored; distant player ignored; distance 1.7 ignored, distance 1.25 infected one tree despite displaced collider; crossfade intermediate and final; renderer bounds and stretched size match original during and after (nonuniform scale + flip); distant tree unchanged; transform, collider, alpha, flip and sorting preserved; no fade overlay left; dead tree survives trail clear. Source="+pair.living.name+" -> "+pair.dead.name);
        } catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-tree-check.txt"),"FAIL: "+e); Debug.LogException(e); }
        finally {
            if(root!=null) UnityEngine.Object.Destroy(root);
            if(tree!=null) UnityEngine.Object.Destroy(tree);
            if(far!=null) UnityEngine.Object.Destroy(far);
            if(proxy!=null) UnityEngine.Object.Destroy(proxy);
        }
    }
    static void Capture(SpriteRenderer renderer,Vector3 origin,string suffix)
    {
        var go=new GameObject("Temporary tree preview camera",typeof(Camera)); var camera=go.GetComponent<Camera>();
        camera.orthographic=true; camera.orthographicSize=3.5f;
        camera.transform.position=origin+new Vector3(0,2,-10);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.23f,.29f,.17f);
        var rt=new RenderTexture(600,700,24); var previous=RenderTexture.active; Texture2D texture=null;
        try {
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            texture=new Texture2D(600,700,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,600,700),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"plague-tree-"+suffix+".png"),texture.EncodeToPNG());
        } finally { camera.targetTexture=null; RenderTexture.active=previous; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            if(texture!=null) UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif
