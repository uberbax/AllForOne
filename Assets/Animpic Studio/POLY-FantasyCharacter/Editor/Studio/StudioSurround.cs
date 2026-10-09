using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class StudioSurround
    {
        // Continuous inward-facing floor, cove, circular wall and rounded ceiling.
        // The complete camera orbit (maximum radius 5 m) stays inside the 6.4 m floor.
        public static Mesh CreateMesh()
        {
            const int segments = 128, curveSteps = 16;
            const float floor = -.145f, floorRadius = 6.4f, curve = 1.6f, wallTop = 8f;
            var profile = new List<Vector4> { new Vector4(floorRadius, floor, 0, 1) };
            for (int i = 1; i <= curveSteps; i++)
            {
                float a = i * Mathf.PI / (2 * curveSteps);
                profile.Add(new Vector4(floorRadius + curve * Mathf.Sin(a), floor + curve * (1 - Mathf.Cos(a)), -Mathf.Sin(a), Mathf.Cos(a)));
            }
            profile.Add(new Vector4(floorRadius + curve, wallTop, -1, 0));
            for (int i = 1; i <= curveSteps; i++)
            {
                float a = i * Mathf.PI / (2 * curveSteps);
                profile.Add(new Vector4(floorRadius + curve * Mathf.Cos(a), wallTop + curve * Mathf.Sin(a), -Mathf.Cos(a), -Mathf.Sin(a)));
            }
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>();
            foreach (var p in profile)
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments; float x = Mathf.Cos(a), z = Mathf.Sin(a);
                    vertices.Add(new Vector3(p.x * x, p.y, p.x * z)); normals.Add(new Vector3(p.z * x, p.w, p.z * z));
                }
            for (int ring = 0; ring < profile.Count - 1; ring++)
                for (int i = 0; i < segments; i++)
                {
                    int a = ring * segments + i, b = ring * segments + (i + 1) % segments, c = a + segments, d = b + segments;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            int floorCenter = vertices.Count; vertices.Add(new Vector3(0, floor, 0)); normals.Add(Vector3.up);
            int ceilingCenter = vertices.Count; vertices.Add(new Vector3(0, wallTop + curve, 0)); normals.Add(Vector3.down);
            int lastRing = (profile.Count - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.AddRange(new[] { floorCenter, next, i, lastRing + i, lastRing + next, ceilingCenter });
            }
            var mesh = new Mesh { name = "360 degree seamless studio" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        public static void CopyGeometry(Mesh source, Mesh destination)
        {
            destination.Clear(); destination.name = source.name; destination.indexFormat = source.indexFormat;
            destination.vertices = source.vertices; destination.normals = source.normals;
            destination.triangles = source.triangles; destination.bounds = source.bounds;
            destination.UploadMeshData(false);
        }

        public static void UpdateOpenStudio()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Update the saved studio outside Play mode.");
            CharacterStudioSetup.OpenStudio();
            var ui = Object.FindObjectOfType<CharacterStudioUI>();
            if (!ui || !ui.viewCamera || !ui.presentation) throw new InvalidOperationException("Studio camera references are missing.");
            var scene = SceneManager.GetActiveScene();
            var filter = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).Single(f => f.name == "Infinite cyclorama");
            string backup = Path.GetFullPath("Library/CharacterStudioSceneBackups"); Directory.CreateDirectory(backup);
            if (!EditorSceneManager.SaveScene(scene, (backup + "/CharacterStudio-before-360-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity").Replace('\\', '/'), true))
                throw new IOException("Could not preserve the current studio scene.");
            var replacement = CreateMesh();
            try { CopyGeometry(replacement, filter.sharedMesh); }
            finally { Object.DestroyImmediate(replacement); }
            EditorUtility.SetDirty(filter.sharedMesh); AssetDatabase.SaveAssetIfDirty(filter.sharedMesh);
            filter.transform.position = ui.stageRoot.position;
            EditorUtility.SetDirty(filter.transform);
            filter.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            ui.presentation.viewportWidth = 1;
            ui.viewCamera.rect = new Rect(0, 0, 1, 1);
            EditorUtility.SetDirty(ui.presentation); EditorUtility.SetDirty(ui.viewCamera); EditorUtility.SetDirty(filter.GetComponent<Renderer>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the updated studio.");
            RenderOrbit(ui);
            SceneView.RepaintAll();
            Debug.Log("Studio updated: full camera viewport and seamless 360-degree backdrop.");
        }

        private static void RenderOrbit(CharacterStudioUI ui)
        {
            string output = Path.GetFullPath("Library/CharacterStudioValidation/surround"); Directory.CreateDirectory(output);
            var camera = ui.viewCamera; Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
            try
            {
                Vector3 target = ui.stageRoot.position + Vector3.up * ui.presentation.targetHeight;
                for (int i = 0; i < 4; i++)
                {
                    camera.transform.position = target + Quaternion.Euler(-ui.presentation.defaultPitch, ui.presentation.defaultYaw + i * 90, 0) * (Vector3.forward * ui.presentation.defaultDistance);
                    camera.transform.LookAt(target);
                    CharacterStudioSetup.Render(ui, Path.Combine(output, "orbit-" + (i * 90).ToString("000") + ".png"));
                }
                File.WriteAllText(Path.Combine(output, "result.txt"), "Camera Rect: " + camera.rect + "\nOrbit captures: 0, 90, 180, 270 degrees\nBackdrop: closed circular floor/wall/ceiling, radius 8 m\nRuntime viewportWidth: " + ui.presentation.viewportWidth);
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); }
        }
    }
}
