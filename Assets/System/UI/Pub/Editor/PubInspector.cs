#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class PubInspector
{
 public const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\pub";
 public const string Theme="Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Prefabs/Prefabs~DemoScenes/";
 [InitializeOnLoadMethod] static void Init(){if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Run;}
 [MenuItem("Tools/UI/Pub/Inspect Theme")]
 public static void Run(){
  if(EditorApplication.isPlaying){File.WriteAllText(Work+"/inspect.txt","NEED_EDIT_MODE");return;}
  File.Delete(Work+"/inspect.request");
  try{
   string[] files={"Character_Trait.prefab","Shop_DailyShop_01.prefab","Rewards_Mailbox.prefab"};
   var scene=SceneManager.GetActiveScene();
   var themes=files.Select(file=>{
    var p=AssetDatabase.LoadAssetAtPath<GameObject>(Theme+file);
    return new{file,rect=((RectTransform)p.transform).sizeDelta.ToString(),
     images=p.GetComponentsInChildren<Image>(true).Select(i=>new{path=AnimationUtility.CalculateTransformPath(i.transform,p.transform),sprite=AssetDatabase.GetAssetPath(i.sprite),color=i.color.ToString(),type=i.type.ToString(),rect=i.rectTransform.sizeDelta.ToString()}).ToArray(),
     texts=p.GetComponentsInChildren<TMP_Text>(true).Take(25).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t.transform,p.transform),text=t.text,font=AssetDatabase.GetAssetPath(t.font),size=t.fontSize,color=t.color.ToString()}).ToArray()};
   }).ToArray();
   var holder=UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
   var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{name=c.name,path=AnimationUtility.CalculateTransformPath(c.transform,null),size=((RectTransform)c.transform).rect.size.ToString(),root=c.isRootCanvas,scale=c.transform.lossyScale.ToString(),children=c.transform.Cast<Transform>().Select(t=>new{name=t.name,active=t.gameObject.activeSelf}).ToArray()}).ToArray();
   File.WriteAllText(Work+"/inspect.json",JsonConvert.SerializeObject(new{scene=scene.path,dirty=scene.isDirty,canvases,themes,avatars=holder!=null?holder.avas.Take(12).Select(v=>new{id=v.Key,path=AssetDatabase.GetAssetPath(v.Value)}).ToArray():null},Formatting.Indented));
   Render(AssetDatabase.LoadAssetAtPath<GameObject>(Theme+"Character_Trait.prefab"),"theme-traits.png");
   File.WriteAllText(Work+"/inspect.txt","PASS");
  }catch(Exception e){File.WriteAllText(Work+"/inspect.txt","FAIL "+e);Debug.LogException(e);}
 }
 public static void Render(GameObject source,string filename){
  var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D tex=null;var old=RenderTexture.active;
  try{
   var root=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(true);
   var rect=(RectTransform)root.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;rect.rotation=Quaternion.identity;
   var size=rect.sizeDelta;if(size.x<100||size.y<100)size=new Vector2(1080,2400);rect.sizeDelta=size;
   var canvas=root.GetComponent<Canvas>();if(canvas==null)canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;rect.localScale=Vector3.one;rect.sizeDelta=size;
   foreach(var scaler in root.GetComponentsInChildren<CanvasScaler>(true))scaler.enabled=false;
   foreach(var group in root.GetComponentsInChildren<CanvasGroup>(true))group.alpha=1;
   var camGo=new GameObject("Pub preview",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.GetComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=size.y*.5f;cam.transform.position=new Vector3(0,0,-1000);cam.farClipPlane=2000;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.06f,.06f);
   rt=new RenderTexture(720,1600,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex=new Texture2D(720,1600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1600),0,0);tex.Apply();File.WriteAllBytes(Work+"/"+filename,tex.EncodeToPNG());
  }finally{RenderTexture.active=old;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
 }
}
#endif
