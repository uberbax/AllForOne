#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PubPortraitBuilder
{
    public static PubPortraitLibrary Build(ResourceHolder holder)
    {
        const string path = PubBuilder.Root + "/PubPortraits.asset";
        var library = AssetDatabase.LoadAssetAtPath<PubPortraitLibrary>(path);
        if (library == null) { library = ScriptableObject.CreateInstance<PubPortraitLibrary>(); AssetDatabase.CreateAsset(library,path); }
        var existing = library.entries.ToDictionary(e=>e.id,e=>e.sprite,StringComparer.Ordinal);
        var entries = new List<PubPortraitLibrary.Entry>();
        var texturePixels = new Dictionary<Texture2D,Color32[]>();
        try
        {
            foreach (var item in holder.avas)
            {
                if (!PubRecruitment.IsEligibleId(item.Key) || item.Value == null) continue;
                if (existing.TryGetValue(item.Key,out var previous) && previous != null)
                { entries.Add(new PubPortraitLibrary.Entry{id=item.Key,sprite=previous});continue; }
                var source=item.Value;var texture=source.texture;
                if (!texturePixels.TryGetValue(texture,out var pixels))
                {
                    var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
                    var old=RenderTexture.active;Texture2D copy=null;
                    try { Graphics.Blit(texture,rt);RenderTexture.active=rt;copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();pixels=copy.GetPixels32();if(texturePixels.Count>=4||texturePixels.Values.Sum(p=>(long)p.Length)+pixels.Length>32000000)texturePixels.Clear();texturePixels.Add(texture,pixels); }
                    finally { RenderTexture.active=old;if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);RenderTexture.ReleaseTemporary(rt); }
                }
                var rect=source.rect;int x0=Mathf.RoundToInt(rect.x),y0=Mathf.RoundToInt(rect.y),w=Mathf.RoundToInt(rect.width),h=Mathf.RoundToInt(rect.height);
                int minX=w,minY=h,maxX=-1,maxY=-1;
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[(y0+y)*texture.width+x0+x].a>16)
                {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                if(maxX<0)continue;
                minX=Math.Max(0,minX-2);minY=Math.Max(0,minY-2);maxX=Math.Min(w-1,maxX+2);maxY=Math.Min(h-1,maxY+2);
                var cropped=Sprite.Create(texture,new Rect(x0+minX,y0+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
                cropped.name="Pub_"+item.Key;AssetDatabase.AddObjectToAsset(cropped,library);
                entries.Add(new PubPortraitLibrary.Entry{id=item.Key,sprite=cropped});
            }
        }
        finally { texturePixels.Clear(); }
        library.entries=entries;library.ClearLookup();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);return library;
    }
}
#endif
