using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LayerLab
{
    /// <summary>
    /// Editor utility that scans the "Bridge" tilemap, groups connected bridge tiles into
    /// individual passages, and generates a <see cref="BridgePassage2D"/> trigger plus side
    /// blockers for each group so the player can cross the "Pit" (water) tilemap safely.
    /// </summary>
    public static class BridgePassageGenerator
    {
        private const string BridgeTilemapName = "Bridge";
        private const string PitTilemapName = "Pit";
        private const string GeneratedRootName = "BridgePassages";
        private const string GeneratedPrefix = "BridgePassage_";
        private const float EdgePadding = 0.05f;
        private const float SideBlockerThickness = 0.08f;
        private const float SideBlockerExtraLength = 0.02f;
        // Reused scratch buffer for sprite physics-shape points to avoid per-call allocations.
        private static readonly List<Vector2> PhysicsShapePoints = new();

        [MenuItem("Tools/Layer Lab/Generate Bridge Passages")]
        public static void Generate()
        {
            Tilemap bridgeTilemap = FindTilemap(BridgeTilemapName);
            Tilemap pitTilemap = FindTilemap(PitTilemapName);

            if (bridgeTilemap == null)
            {
                Debug.LogError($"[BridgePassageGenerator] Tilemap named '{BridgeTilemapName}' was not found.");
                return;
            }

            if (pitTilemap == null)
            {
                Debug.LogError($"[BridgePassageGenerator] Tilemap named '{PitTilemapName}' was not found.");
                return;
            }

            // The pit (water) collider is what each passage will ignore while a player is on it.
            Collider2D waterCollider = pitTilemap.GetComponent<CompositeCollider2D>();
            if (waterCollider == null)
                waterCollider = pitTilemap.GetComponent<TilemapCollider2D>();

            if (waterCollider == null)
            {
                Debug.LogError($"[BridgePassageGenerator] '{PitTilemapName}' needs a CompositeCollider2D or TilemapCollider2D.");
                return;
            }

            Transform parent = GetOrCreateGeneratedRoot(bridgeTilemap.transform);
            // Rebuild generated passage objects from the current tilemap so stale colliders are removed first.
            ClearGeneratedPassages(parent);

            List<List<Vector3Int>> groups = FindConnectedTileGroups(bridgeTilemap);
            int created = 0;

            foreach (List<Vector3Int> group in groups)
            {
                if (group.Count == 0)
                    continue;

                Bounds bounds = CalculateWorldBounds(bridgeTilemap, group);
                // A taller-than-wide bridge runs vertically, so its width is along X.
                bool clampX = bounds.size.y >= bounds.size.x;
                CreatePassage(parent, bounds, waterCollider, clampX, created + 1);
                created++;
            }

            EditorSceneManager.MarkSceneDirty(bridgeTilemap.gameObject.scene);
            Debug.Log($"[BridgePassageGenerator] Created {created} bridge passage trigger(s).", bridgeTilemap);
        }

        // Finds a scene tilemap by name (includes inactive objects).
        private static Tilemap FindTilemap(string tilemapName)
        {
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.name == tilemapName && tilemap.gameObject.scene.IsValid())
                    return tilemap;
            }

            return null;
        }

        // Returns the shared "BridgePassages" root under the bridge's parent, creating it if needed.
        private static Transform GetOrCreateGeneratedRoot(Transform bridgeTransform)
        {
            Transform parent = bridgeTransform.parent;
            Transform root = parent != null ? parent.Find(GeneratedRootName) : null;

            if (root != null)
                return root;

            GameObject rootObject = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Bridge Passages Root");

            if (parent != null)
                rootObject.transform.SetParent(parent, false);

            return rootObject.transform;
        }

        // Removes previously generated passages so the generator can run idempotently.
        private static void ClearGeneratedPassages(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith(GeneratedPrefix))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // Flood-fills (4-connected BFS) the bridge tiles into separate connected groups.
        private static List<List<Vector3Int>> FindConnectedTileGroups(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            HashSet<Vector3Int> bridgeCells = new();

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cell))
                    bridgeCells.Add(cell);
            }

            // Flood-fill occupied cells so each contiguous bridge island becomes one passage trigger.
            List<List<Vector3Int>> groups = new();
            Queue<Vector3Int> queue = new();
            Vector3Int[] directions =
            {
                new(1, 0, 0),
                new(-1, 0, 0),
                new(0, 1, 0),
                new(0, -1, 0)
            };

            while (bridgeCells.Count > 0)
            {
                // Pick any remaining cell as the seed for the next group.
                Vector3Int start = default;
                foreach (Vector3Int cell in bridgeCells)
                {
                    start = cell;
                    break;
                }

                List<Vector3Int> group = new();
                bridgeCells.Remove(start);
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    Vector3Int current = queue.Dequeue();
                    group.Add(current);

                    foreach (Vector3Int direction in directions)
                    {
                        Vector3Int next = current + direction;
                        // Remove returns false if the neighbor isn't an unvisited bridge cell.
                        if (!bridgeCells.Remove(next))
                            continue;

                        queue.Enqueue(next);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        // Computes the world-space bounds of a tile group, preferring precise sprite physics shapes.
        private static Bounds CalculateWorldBounds(Tilemap tilemap, List<Vector3Int> cells)
        {
            if (TryCalculatePhysicsShapeWorldBounds(tilemap, cells, out Bounds physicsBounds))
                return physicsBounds;

            // Fallback: encapsulate the full cell rectangles when no physics shapes exist.
            Vector3 cellSize = GetWorldCellSize(tilemap);
            Bounds bounds = new Bounds(tilemap.GetCellCenterWorld(cells[0]), cellSize);

            for (int i = 1; i < cells.Count; i++)
            {
                Bounds cellBounds = new Bounds(tilemap.GetCellCenterWorld(cells[i]), cellSize);
                bounds.Encapsulate(cellBounds);
            }

            return bounds;
        }

        // Builds tight world bounds from the tile sprites' physics shapes, accounting for
        // each tile's transform matrix and anchor. Returns false when no shapes are present.
        private static bool TryCalculatePhysicsShapeWorldBounds(Tilemap tilemap, List<Vector3Int> cells, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (Vector3Int cell in cells)
            {
                Sprite sprite = tilemap.GetSprite(cell);
                if (sprite == null || sprite.GetPhysicsShapeCount() == 0)
                    continue;

                Matrix4x4 tileTransform = tilemap.GetTransformMatrix(cell);
                Vector3 localOrigin = tilemap.CellToLocalInterpolated((Vector3)cell + tilemap.tileAnchor);

                for (int shapeIndex = 0; shapeIndex < sprite.GetPhysicsShapeCount(); shapeIndex++)
                {
                    PhysicsShapePoints.Clear();
                    sprite.GetPhysicsShape(shapeIndex, PhysicsShapePoints);

                    foreach (Vector2 point in PhysicsShapePoints)
                    {
                        // Tile-local shape point -> tilemap-local -> world space.
                        Vector3 shapePoint = new Vector3(point.x, point.y, 0f);
                        Vector3 localPoint = localOrigin + tileTransform.MultiplyPoint3x4(shapePoint);
                        Vector3 worldPoint = tilemap.transform.TransformPoint(localPoint);

                        if (!hasBounds)
                        {
                            bounds = new Bounds(worldPoint, Vector3.zero);
                            hasBounds = true;
                            continue;
                        }

                        bounds.Encapsulate(worldPoint);
                    }
                }
            }

            return hasBounds;
        }

        // World-space size of a single tilemap cell, including the tilemap's lossy scale.
        private static Vector3 GetWorldCellSize(Tilemap tilemap)
        {
            GridLayout grid = tilemap.layoutGrid;
            Vector3 cellSize = grid != null ? grid.cellSize : Vector3.one;
            Vector3 scale = tilemap.transform.lossyScale;

            return new Vector3(
                Mathf.Abs(cellSize.x * scale.x),
                Mathf.Abs(cellSize.y * scale.y),
                Mathf.Abs(cellSize.z * scale.z));
        }

        // Spawns the passage GameObject: a trigger box, the BridgePassage2D component, and side blockers.
        private static void CreatePassage(Transform parent, Bounds bounds, Collider2D waterCollider, bool clampX, int index)
        {
            GameObject passageObject = new GameObject($"{GeneratedPrefix}{index:000}");
            Undo.RegisterCreatedObjectUndo(passageObject, "Create Bridge Passage");

            passageObject.transform.SetParent(parent, true);
            passageObject.transform.position = bounds.center;

            // The center trigger disables water collision while the side blockers keep the player aligned on the bridge.
            BoxCollider2D trigger = passageObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(bounds.size.x, bounds.size.y);

            BridgePassage2D passage = passageObject.AddComponent<BridgePassage2D>();
            ConfigurePassage(passage, trigger, waterCollider, clampX);
            CreateSideBlockers(passageObject.transform, bounds, clampX);
        }

        // Writes the BridgePassage2D serialized fields via SerializedObject to keep them persistent.
        private static void ConfigurePassage(BridgePassage2D passage, BoxCollider2D trigger, Collider2D waterCollider, bool clampX)
        {
            SerializedObject serialized = new SerializedObject(passage);

            SerializedProperty waterColliders = serialized.FindProperty("waterColliders");
            waterColliders.arraySize = 1;
            waterColliders.GetArrayElementAtIndex(0).objectReferenceValue = waterCollider;

            serialized.FindProperty("ignoreWaterLayerWhileOnBridge").boolValue = true;
            serialized.FindProperty("keepPlayerInsideBridgeWidth").boolValue = false;
            serialized.FindProperty("bridgeBoundsCollider").objectReferenceValue = trigger;
            serialized.FindProperty("bridgeEdgePadding").floatValue = EdgePadding;

            // enumValueIndex 1 = ClampX, 2 = ClampY (matches BridgeWidthAxis order).
            SerializedProperty bridgeWidthAxis = serialized.FindProperty("bridgeWidthAxis");
            bridgeWidthAxis.enumValueIndex = clampX ? 1 : 2;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Adds thin static blockers along the bridge sides so the player cannot slip off the edges.
        private static void CreateSideBlockers(Transform parent, Bounds bounds, bool clampX)
        {
            // Block only the bridge edges; the passable center remains controlled by BridgePassage2D.
            if (clampX)
            {
                // Vertical bridge: blockers on the left and right edges.
                Vector2 size = new Vector2(SideBlockerThickness, bounds.size.y + SideBlockerExtraLength);
                CreateBlocker(parent, "LeftBlocker", new Vector2(bounds.min.x - SideBlockerThickness * 0.5f, bounds.center.y), size);
                CreateBlocker(parent, "RightBlocker", new Vector2(bounds.max.x + SideBlockerThickness * 0.5f, bounds.center.y), size);
                return;
            }

            // Horizontal bridge: blockers on the top and bottom edges.
            Vector2 horizontalSize = new Vector2(bounds.size.x + SideBlockerExtraLength, SideBlockerThickness);
            CreateBlocker(parent, "BottomBlocker", new Vector2(bounds.center.x, bounds.min.y - SideBlockerThickness * 0.5f), horizontalSize);
            CreateBlocker(parent, "TopBlocker", new Vector2(bounds.center.x, bounds.max.y + SideBlockerThickness * 0.5f), horizontalSize);
        }

        private static void CreateBlocker(Transform parent, string name, Vector2 worldPosition, Vector2 size)
        {
            GameObject blockerObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(blockerObject, "Create Bridge Side Blocker");

            blockerObject.transform.SetParent(parent, true);
            blockerObject.transform.position = worldPosition;

            BoxCollider2D blocker = blockerObject.AddComponent<BoxCollider2D>();
            blocker.size = size;
        }
    }
}
