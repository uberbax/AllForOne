using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.FantasyCharacter.Editor
{
    public static class FemaleForearmMeshBuilder
    {
        public static Mesh Build(Mesh source, Mesh completeArm, string name)
        {
            var mesh = Object.Instantiate(source);
            mesh.name = name;
            var vertices = source.vertices; var uv = source.uv; var normals = source.normals; var weights = source.boneWeights;
            var positionKeys = vertices.Select(Key).ToArray();
            var surfaceKeys = new string[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                surfaceKeys[i] = positionKeys[i] + "/" + Key(normals[i]) + "/" + SkinKey(weights[i]);
            var referenceUV = ReferenceUV(completeArm);
            int total = 0, removed = 0, correctedCuff = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                var retained = new List<int>();
                var seen = new Dictionary<string, int>();
                var scores = new Dictionary<string, float>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    total++;
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    string surface = TriangleKey(surfaceKeys[a], surfaceKeys[b], surfaceKeys[c]);
                    string geometry = TriangleKey(positionKeys[a], positionKeys[b], positionKeys[c]);
                    Vector2 mean = (uv[a] + uv[b] + uv[c]) / 3f;
                    float score = referenceUV.ContainsKey(geometry) ? referenceUV[geometry].Min(p => (p - mean).sqrMagnitude) : 0;
                    int offset;
                    if (seen.TryGetValue(surface, out offset))
                    {
                        removed++;
                        // The two source copies use different atlas patches on six
                        // wrist quads. Match the complete D arm's cuff instead of
                        // retaining the first (incorrectly coloured) copy blindly.
                        if (score < scores[surface])
                        {
                            retained[offset] = a; retained[offset + 1] = b; retained[offset + 2] = c;
                            scores[surface] = score; correctedCuff++;
                        }
                        continue;
                    }
                    seen.Add(surface, retained.Count); scores.Add(surface, score);
                    retained.Add(a); retained.Add(b); retained.Add(c);
                }
                mesh.SetTriangles(retained, sub, false);
            }
            if (total - removed != 148 || (removed != 0 && removed * 2 != total))
            {
                Object.DestroyImmediate(mesh);
                throw new InvalidOperationException(name + ": unexpected duplicate count " + removed + "/" + total + "; source normals or skin weights have changed. Re-audit before rebuilding.");
            }
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/Customization/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);
            Debug.Log(name + ": removed " + removed + "/" + total + " duplicate triangles; " + correctedCuff + " cuff triangles matched to complete-arm colour.");
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            return mesh;
        }

        private static Dictionary<string, List<Vector2>> ReferenceUV(Mesh mesh)
        {
            var positions = mesh.vertices.Select(Key).ToArray(); var uv = mesh.uv; var triangles = mesh.triangles;
            var result = new Dictionary<string, List<Vector2>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                string key = TriangleKey(positions[a], positions[b], positions[c]);
                if (!result.ContainsKey(key)) result.Add(key, new List<Vector2>());
                result[key].Add((uv[a] + uv[b] + uv[c]) / 3f);
            }
            return result;
        }
        private static string SkinKey(BoneWeight w)
        {
            var influences = new[] { w.boneIndex0 + ":" + Mathf.RoundToInt(w.weight0 * 1000000), w.boneIndex1 + ":" + Mathf.RoundToInt(w.weight1 * 1000000), w.boneIndex2 + ":" + Mathf.RoundToInt(w.weight2 * 1000000), w.boneIndex3 + ":" + Mathf.RoundToInt(w.weight3 * 1000000) };
            Array.Sort(influences, StringComparer.Ordinal);
            return string.Join(";", influences);
        }
        private static string TriangleKey(string a, string b, string c) { return string.CompareOrdinal(a, b) <= 0 && string.CompareOrdinal(a, c) <= 0 ? a + "|" + b + "|" + c : string.CompareOrdinal(b, c) <= 0 ? b + "|" + c + "|" + a : c + "|" + a + "|" + b; }
        private static string Key(Vector3 v) { return Mathf.RoundToInt(v.x * 100000) + "," + Mathf.RoundToInt(v.y * 100000) + "," + Mathf.RoundToInt(v.z * 100000); }
    }
}
