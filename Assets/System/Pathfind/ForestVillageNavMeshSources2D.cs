using System;
using System.Collections.Generic;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

/// <summary>Builds XY navigation from ground, water and obstacles, keeping bridge passages open.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshSurface))]
[AddComponentMenu("Navigation/Forest Village NavMesh Sources 2D")]
public sealed class ForestVillageNavMeshSources2D : NavMeshExtension
{
    [SerializeField] private Tilemap ground;
    [SerializeField] private Collider2D water;
    [SerializeField] private BoxCollider2D[] bridgePassages = Array.Empty<BoxCollider2D>();
    [SerializeField] private Transform[] obstacleRoots = Array.Empty<Transform>();
    [SerializeField] private Transform treeRoot;
    [SerializeField, Min(0.01f)] private float trunkHeight = 0.35f;

    private const int Walkable = 0;
    private const int NotWalkable = 1;

    protected override void ConnectToVcam(bool connect)
    {
        // Also runs after script reload, when Awake may not run again.
        Order = 100;
        base.ConnectToVcam(connect);
    }

    public override void CollectSources(NavMeshSurface surface, List<NavMeshBuildSource> sources,
        NavMeshBuilderState state)
    {
        if (!isActiveAndEnabled)
            return;

        if (ground == null || water == null || !ground.gameObject.activeInHierarchy ||
            !water.isActiveAndEnabled)
            throw new InvalidOperationException("Assign active Ground and Water sources before baking navigation.");
        if (ground.layoutGrid == null || ground.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle)
            throw new InvalidOperationException("Forest Village navigation requires a rectangular XY grid.");
        foreach (var bridge in bridgePassages)
            if (bridge == null || !bridge.isActiveAndEnabled)
                throw new InvalidOperationException("A navigation bridge passage is missing or disabled.");

        var tileCollider = water.GetComponent<TilemapCollider2D>();
        if (tileCollider != null && tileCollider.hasTilemapChanges)
            tileCollider.ProcessTilemapChanges();
        if (water is CompositeCollider2D composite &&
            composite.generationType == CompositeCollider2D.GenerationType.Manual)
            composite.GenerateGeometry();
        Physics2D.SyncTransforms();

        // Follow NavMeshPlus's per-build mesh lifetime; no generated scene objects or colliders.
        var meshes = state.GetExtraState<BuildMeshes>();
        var waterMesh = water.CreateMesh(false, false);
        if (waterMesh != null)
            meshes.Items.Add(waterMesh);
        if (waterMesh == null || waterMesh.vertexCount == 0)
            throw new InvalidOperationException("Water has no closed geometry to exclude from navigation.");
        var clippedWater = CutBridgePassages(waterMesh);
        meshes.Items.Add(clippedWater);

        // Rebuild from the assigned sources rather than unrelated scene geometry.
        sources.Clear();
        var cellSize = ground.layoutGrid.cellSize;
        cellSize.z = 0f;
        foreach (var cell in ground.cellBounds.allPositionsWithin)
        {
            if (!ground.HasTile(cell))
                continue;
            var center = ground.CellToLocalInterpolated((Vector3)cell + new Vector3(0.5f, 0.5f, 0f));
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = ground.transform.localToWorldMatrix * Matrix4x4.Translate(center),
                size = cellSize,
                area = Walkable,
                component = ground
            });
        }
        sources.Add(new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh,
            transform = Matrix4x4.identity,
            sourceObject = clippedWater,
            area = NotWalkable,
            component = water
        });
        AddObstacleSources(surface, sources, meshes);
        AddTreeSources(surface, sources);
    }

    private void AddTreeSources(NavMeshSurface surface, List<NavMeshBuildSource> sources)
    {
        if (treeRoot == null || !treeRoot.gameObject.activeInHierarchy)
            return;
        var footprints = new Dictionary<Sprite, Bounds>();
        foreach (var tree in treeRoot.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!tree.enabled || !tree.gameObject.activeInHierarchy || tree.sprite == null ||
                (surface.layerMask.value & (1 << tree.gameObject.layer)) == 0)
                continue;
            if (!footprints.TryGetValue(tree.sprite, out var footprint))
            {
                footprint = GetTrunkFootprint(tree.sprite);
                footprints.Add(tree.sprite, footprint);
            }
            var matrix = tree.transform.localToWorldMatrix *
                Matrix4x4.Scale(new Vector3(tree.flipX ? -1f : 1f, tree.flipY ? -1f : 1f, 1f)) *
                Matrix4x4.Translate(footprint.center);
            matrix.m23 = ground.transform.position.z;
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = matrix,
                size = footprint.size,
                area = NotWalkable,
                component = tree
            });
        }
    }

    private Bounds GetTrunkFootprint(Sprite sprite)
    {
        // Use the bottom of the authored physics outline, excluding the canopy and shadow padding.
        var outlines = new List<List<Vector3>>();
        var points = new List<Vector2>();
        var bottom = float.PositiveInfinity;
        for (var shape = 0; shape < sprite.GetPhysicsShapeCount(); shape++)
        {
            sprite.GetPhysicsShape(shape, points);
            var outline = new List<Vector3>();
            foreach (var point in points)
            {
                outline.Add(point);
                bottom = Mathf.Min(bottom, point.y);
            }
            outlines.Add(outline);
        }
        var bounds = new Bounds();
        var hasPoint = false;
        foreach (var outline in outlines)
        {
            var baseOutline = Clip(outline, Matrix4x4.identity, 1, bottom + Mathf.Max(0.01f, trunkHeight), false);
            foreach (var point in baseOutline)
            {
                if (!hasPoint)
                    bounds = new Bounds(point, Vector3.zero);
                else
                    bounds.Encapsulate(point);
                hasPoint = true;
            }
        }
        if (!hasPoint)
            throw new InvalidOperationException("Tree sprite has no physics outline: " + sprite.name);
        return bounds;
    }

    private void AddObstacleSources(NavMeshSurface surface, List<NavMeshBuildSource> sources, BuildMeshes meshes)
    {
        var collected = new HashSet<Collider2D>();
        foreach (var root in obstacleRoots)
        {
            if (root == null || !root.gameObject.activeInHierarchy)
                continue;
            foreach (var collider in root.GetComponentsInChildren<Collider2D>())
                // A directly assigned footprint may be a trigger; child door triggers are not obstacles.
                AddObstacle(collider, surface, sources, meshes, collected, collider.transform == root);
        }

        // Preserve explicit Not Walkable modifiers, including those outside the terrain hierarchy.
        foreach (var modifier in NavMeshModifier.activeModifiers)
        {
            if (modifier.gameObject.scene != gameObject.scene || !modifier.isActiveAndEnabled ||
                modifier.ignoreFromBuild || !modifier.overrideArea || modifier.area != NotWalkable ||
                !modifier.AffectsAgentType(surface.agentTypeID))
                continue;
            foreach (var collider in modifier.GetComponents<Collider2D>())
                AddObstacle(collider, surface, sources, meshes, collected, true);
        }
    }

    private void AddObstacle(Collider2D collider, NavMeshSurface surface, List<NavMeshBuildSource> sources,
        BuildMeshes meshes, HashSet<Collider2D> collected, bool includeTrigger)
    {
        if (collider == null || !collider.isActiveAndEnabled || (!includeTrigger && collider.isTrigger) ||
            (surface.layerMask.value & (1 << collider.gameObject.layer)) == 0)
            return;
        var tilemapCollider = collider.GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null && tilemapCollider.hasTilemapChanges)
            tilemapCollider.ProcessTilemapChanges();
        if (collider.compositeOperation != Collider2D.CompositeOperation.None)
            collider = collider.composite;
        if (collider == null || !collider.isActiveAndEnabled || (!includeTrigger && collider.isTrigger) ||
            collider == water || Array.IndexOf(bridgePassages, collider) >= 0 || !collected.Add(collider))
            return;
        if (collider is CompositeCollider2D composite &&
            composite.generationType == CompositeCollider2D.GenerationType.Manual)
            composite.GenerateGeometry();

        var hasBody = collider.attachedRigidbody != null;
        var mesh = collider.CreateMesh(hasBody, hasBody);
        if (mesh == null)
            return;
        meshes.Items.Add(mesh);
        if (mesh.vertexCount == 0)
            return;
        var vertices = mesh.vertices;
        for (var i = 0; i < vertices.Length; i++)
            vertices[i].z = ground.transform.position.z;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        sources.Add(new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh,
            transform = Matrix4x4.identity,
            sourceObject = mesh,
            area = NotWalkable,
            component = collider
        });
    }

    private Mesh CutBridgePassages(Mesh source)
    {
        var vertices = source.vertices;
        var triangles = source.triangles;
        var body = water.attachedRigidbody;
        var toWorld = body == null ? Matrix4x4.identity :
            Matrix4x4.TRS(body.position, Quaternion.Euler(0f, 0f, body.rotation), Vector3.one);
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = toWorld.MultiplyPoint3x4(vertices[i]);

        var outputVertices = new List<Vector3>();
        var outputTriangles = new List<int>();
        for (var i = 0; i < triangles.Length; i += 3)
        {
            var polygons = new List<List<Vector3>>
            {
                new List<Vector3> { vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]] }
            };
            foreach (var bridge in bridgePassages)
            {
                var remaining = new List<List<Vector3>>();
                foreach (var polygon in polygons)
                    SubtractBridge(polygon, bridge, remaining);
                polygons = remaining;
            }
            foreach (var polygon in polygons)
            {
                var start = outputVertices.Count;
                outputVertices.AddRange(polygon);
                for (var j = 1; j + 1 < polygon.Count; j++)
                {
                    outputTriangles.Add(start);
                    outputTriangles.Add(start + j);
                    outputTriangles.Add(start + j + 1);
                }
            }
        }
        var mesh = new Mesh { name = "Water excluding bridge passages", hideFlags = HideFlags.HideAndDontSave };
        if (outputVertices.Count > ushort.MaxValue)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(outputVertices);
        mesh.SetTriangles(outputTriangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SubtractBridge(List<Vector3> polygon, BoxCollider2D bridge,
        List<List<Vector3>> output)
    {
        var minimum = bridge.offset - bridge.size * 0.5f;
        var maximum = bridge.offset + bridge.size * 0.5f;
        var toLocal = bridge.transform.worldToLocalMatrix;
        // Split off the outside of each edge. Only the final inside polygon is discarded.
        for (var edge = 0; edge < 4 && polygon.Count >= 3; edge++)
        {
            var axis = edge / 2;
            var keepGreater = edge % 2 == 0;
            var boundary = keepGreater ? minimum[axis] : maximum[axis];
            var outside = Clip(polygon, toLocal, axis, boundary, !keepGreater);
            if (outside.Count >= 3)
                output.Add(outside);
            polygon = Clip(polygon, toLocal, axis, boundary, keepGreater);
        }
    }

    private static List<Vector3> Clip(List<Vector3> polygon, Matrix4x4 toLocal, int axis,
        float boundary, bool keepGreater)
    {
        var output = new List<Vector3>();
        if (polygon.Count == 0)
            return output;
        var previous = polygon[polygon.Count - 1];
        var previousValue = toLocal.MultiplyPoint3x4(previous)[axis];
        var previousInside = keepGreater ? previousValue >= boundary : previousValue <= boundary;
        foreach (var current in polygon)
        {
            var currentValue = toLocal.MultiplyPoint3x4(current)[axis];
            var currentInside = keepGreater ? currentValue >= boundary : currentValue <= boundary;
            if (previousInside != currentInside)
                output.Add(Vector3.LerpUnclamped(previous, current,
                    (boundary - previousValue) / (currentValue - previousValue)));
            if (currentInside)
                output.Add(current);
            previous = current;
            previousValue = currentValue;
            previousInside = currentInside;
        }
        return output;
    }

    private sealed class BuildMeshes : IDisposable
    {
        public readonly List<Mesh> Items = new List<Mesh>();
        public BuildMeshes() { }

        public void Dispose()
        {
            foreach (var mesh in Items)
            {
                if (mesh == null)
                    continue;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }
            Items.Clear();
        }
    }
}
