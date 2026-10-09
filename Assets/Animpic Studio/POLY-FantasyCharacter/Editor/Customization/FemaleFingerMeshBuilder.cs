using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.FantasyCharacter.Editor
{
    public static class FemaleFingerMeshBuilder
    {
        [Serializable] private sealed class Face { public Vector3[] vertices = null; }
        [Serializable] private sealed class Hand { public Face[] faces = null; }
        [Serializable] private sealed class Mask { public Hand left = null; public Hand right = null; }

        public static Mesh Build(Mesh source, bool left)
        {
            return Build(source, left, "Assets/Animpic Studio/POLY-FantasyCharacter/Editor/Customization/GloveBFingerMask.json", "CHR_Glove_B_ExposedFingers_" + (left ? "L" : "R"));
        }
        public static Mesh Build(Mesh source, bool left, string maskPath, string meshName)
        {
            var mask = JsonUtility.FromJson<Mask>(File.ReadAllText(maskPath));
            var faces = (left ? mask.left : mask.right).faces;
            var vertices = source.vertices;
            var points = faces.SelectMany(f => f.vertices).Distinct().ToArray();
            var transformed = FindImportedCoordinates(points, vertices);
            var map = new Dictionary<Vector3, string>();
            for (int i = 0; i < points.Length; i++) map[points[i]] = Key(transformed[i]);
            var faceKeys = faces.Select(f => new HashSet<string>(f.vertices.Select(v => map[v]))).ToArray();
            int expected = faces.Sum(f => f.vertices.Length - 2);
            int retainedCount = 0;
            var mesh = Object.Instantiate(source);
            mesh.name = meshName;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var triangles = source.GetTriangles(sub);
                var keep = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    string a = Key(vertices[triangles[i]]), b = Key(vertices[triangles[i + 1]]), c = Key(vertices[triangles[i + 2]]);
                    if (!faceKeys.Any(f => f.Contains(a) && f.Contains(b) && f.Contains(c))) continue;
                    keep.Add(triangles[i]); keep.Add(triangles[i + 1]); keep.Add(triangles[i + 2]); retainedCount++;
                }
                mesh.SetTriangles(keep, sub, false);
            }
            if (retainedCount != expected || retainedCount == 0)
            {
                Object.DestroyImmediate(mesh);
                throw new InvalidOperationException("The audited glove B finger mask no longer matches " + source.name + ": kept " + retainedCount + ", expected " + expected + " triangles. Re-audit the source.");
            }
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/Customization/" + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);
            Debug.Log(mesh.name + ": retained " + retainedCount + " audited finger triangles; original skinning and blend shapes preserved.");
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            return mesh;
        }

        // FBX import axis conventions differ between Blender and Unity. Accept an axis
        // conversion only if EVERY audited point matches the actual imported geometry.
        private static Vector3[] FindImportedCoordinates(Vector3[] source, Vector3[] imported)
        {
            int[][] permutations = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            foreach (float scale in new[] { 1f, 0.01f, 100f })
            foreach (var axes in permutations)
            for (int signs = 0; signs < 8; signs++)
            {
                var result = new Vector3[source.Length];
                bool matches = true;
                for (int i = 0; i < source.Length; i++)
                {
                    var p = new Vector3(source[i][axes[0]] * ((signs & 1) == 0 ? 1 : -1), source[i][axes[1]] * ((signs & 2) == 0 ? 1 : -1), source[i][axes[2]] * ((signs & 4) == 0 ? 1 : -1)) * scale;
                    int found = -1;
                    for (int j = 0; j < imported.Length; j++) if ((p - imported[j]).sqrMagnitude < 0.00000001f * scale * scale) { found = j; break; }
                    if (found < 0) { matches = false; break; }
                    result[i] = imported[found];
                }
                if (matches) return result;
            }
            throw new InvalidOperationException("Cannot align the audited finger mask with imported CHR_Hand geometry. The model has changed.");
        }
        private static string Key(Vector3 v) { return Mathf.RoundToInt(v.x * 100000) + "," + Mathf.RoundToInt(v.y * 100000) + "," + Mathf.RoundToInt(v.z * 100000); }
    }
}
