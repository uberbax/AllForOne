using UnityEditor;
using UnityEngine;

namespace Paperbound.Traveller.Editor
{
    public static class TravellerMaterials
    {
        const string Folder = "Assets/PaperboundTraveller/Materials";
        [MenuItem("Tools/Paperbound Traveller/Use Built-in materials")]
        public static void UseBuiltIn() => Convert("Standard");
        static void Convert(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) { Debug.LogError("Install/enable the target render pipeline first: " + shaderName); return; }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!mat || mat.shader.name == "Paperbound/SoftSprite") continue;
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                Texture map = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : mat.mainTexture;
                Undo.RecordObject(mat, "Convert Paperbound material");
                mat.shader = shader;
                mat.color = color; mat.mainTexture = map;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", map);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0);
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Paperbound materials now use " + shaderName + ". Project render settings were not changed.");
        }
    }
}
