using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLab
{
    // Bakes a shore distance field from the water Tilemap into a runtime texture
    // and feeds it to the Water2D shader (depth, shore band and water mask channels).
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public sealed class Water2DAutoBaker : MonoBehaviour
    {
        private static readonly int ShoreDataTexId = Shader.PropertyToID("_ShoreDataTex");
        private static readonly int ShoreDataWorldRectId = Shader.PropertyToID("_ShoreDataWorldRect");
        private static readonly int UseShoreDataId = Shader.PropertyToID("_UseShoreData");

        [Header("Target")]
        [SerializeField] private Tilemap waterTilemap;
        [SerializeField] private TilemapRenderer targetRenderer;

        [Header("Auto Bake")]
        [SerializeField] private bool autoBake = true;
        [SerializeField, Range(2, 32)] private int texelsPerCell = 32;
        [SerializeField] private bool useSpritePhysicsShape = true;
        [SerializeField, Range(0f, 1f)] private float alphaThreshold = 0.1f;
        [SerializeField, Min(1f)] private float normalizeDistanceCells = 14f;
        [SerializeField, Min(0.25f)] private float shoreBandCells = 5f;

        private MaterialPropertyBlock _propertyBlock;
        private Texture2D _shoreDataTexture;
        private Color32[] _shoreDataPixels;
        private int _lastHash;
        private readonly Dictionary<Texture2D, Color32[]> _texturePixels = new Dictionary<Texture2D, Color32[]>(4);
        private readonly Dictionary<Sprite, List<Vector2>[]> _spritePhysicsShapes = new Dictionary<Sprite, List<Vector2>[]>(16);

#if UNITY_EDITOR
        private double _nextEditorBakeCheckTime;
#endif

        private void Reset()
        {
            CacheComponents();
            BakeAndApply();
        }

        private void OnEnable()
        {
            CacheComponents();
            BakeAndApply();

#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
            ReleaseTexture();
        }

        private void OnValidate()
        {
            texelsPerCell = Mathf.Clamp(texelsPerCell, 2, 32);
            alphaThreshold = Mathf.Clamp01(alphaThreshold);
            normalizeDistanceCells = Mathf.Max(1f, normalizeDistanceCells);
            shoreBandCells = Mathf.Max(0.25f, shoreBandCells);
            CacheComponents();
            BakeAndApply();
        }

        // Rebuilds the shore data texture from the current tiles and pushes it to the renderer.
        [ContextMenu("Bake Water Shore Data")]
        public void BakeAndApply()
        {
            CacheComponents();

            if (waterTilemap == null || targetRenderer == null)
            {
                ApplyEmptyData();
                return;
            }

            BoundsInt bounds = waterTilemap.cellBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;

            if (width <= 0 || height <= 0)
            {
                ApplyEmptyData();
                return;
            }

            // Sample each cell of the tilemap bounds: occupancy, sprite and tile transforms.
            bool[] water = new bool[width * height];
            Sprite[] sprites = new Sprite[water.Length];
            Matrix4x4[] inverseTileTransforms = new Matrix4x4[water.Length];
            Vector3[] localOrigins = new Vector3[water.Length];
            bool hasWater = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = Index(x, y, width);
                    Vector3Int cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                    bool occupied = waterTilemap.HasTile(cell);
                    water[index] = occupied;
                    sprites[index] = occupied ? waterTilemap.GetSprite(cell) : null;
                    if (occupied)
                    {
                        inverseTileTransforms[index] = waterTilemap.GetTransformMatrix(cell).inverse;
                        localOrigins[index] = waterTilemap.CellToLocalInterpolated((Vector3)cell + waterTilemap.tileAnchor);
                    }

                    hasWater |= occupied;
                }
            }

            if (!hasWater)
            {
                ApplyEmptyData();
                return;
            }

            // Upscale to texel resolution, build the per-texel water mask, then the distance field.
            int textureWidth = Mathf.Max(1, width * texelsPerCell);
            int textureHeight = Mathf.Max(1, height * texelsPerCell);
            _texturePixels.Clear();
            _spritePhysicsShapes.Clear();
            bool[] textureMask = BuildTextureMask(bounds, water, sprites, inverseTileTransforms, localOrigins, width, height, textureWidth, textureHeight);
            float[] distance = BuildDistanceField(textureMask, textureWidth, textureHeight);
            EnsureTexture(textureWidth, textureHeight);

            int pixelCount = textureWidth * textureHeight;
            if (_shoreDataPixels == null || _shoreDataPixels.Length != pixelCount)
            {
                _shoreDataPixels = new Color32[pixelCount];
            }

            // Pack per-texel data: R = normalized depth, G = shore band weight, A = water mask.
            Color32[] pixels = _shoreDataPixels;
            float normalizeDistanceTexels = Mathf.Max(1f, normalizeDistanceCells * texelsPerCell);
            float shoreBandTexels = Mathf.Max(1f, shoreBandCells * texelsPerCell);
            for (int y = 0; y < textureHeight; y++)
            {
                int row = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    int texelIndex = row + x;
                    bool isWater = textureMask[texelIndex];
                    float dist = isWater ? distance[texelIndex] : 0f;
                    byte depth = isWater ? ToByte(Mathf.Clamp01(dist / normalizeDistanceTexels)) : (byte)0;
                    byte shore = isWater ? ToByte(1f - Mathf.Clamp01(dist / shoreBandTexels)) : (byte)0;
                    byte mask = isWater ? (byte)255 : (byte)0;
                    pixels[texelIndex] = new Color32(depth, shore, 0, mask);
                }
            }

            _shoreDataTexture.SetPixels32(pixels);
            _shoreDataTexture.Apply(false, false);

            // World-space rect of the baked region, so the shader can map UVs back to world coordinates.
            Vector3 min = waterTilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
            Vector3 max = waterTilemap.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
            ApplyTexture(_shoreDataTexture, new Vector4(min.x, min.y, Mathf.Max(0.001f, max.x - min.x), Mathf.Max(0.001f, max.y - min.y)));
            _lastHash = ComputeBakeHash();
        }

        // Builds the per-texel water mask by sampling the sprite shape/alpha at each texel center.
        private bool[] BuildTextureMask(
            BoundsInt bounds,
            bool[] water,
            Sprite[] sprites,
            Matrix4x4[] inverseTileTransforms,
            Vector3[] localOrigins,
            int cellWidth,
            int cellHeight,
            int textureWidth,
            int textureHeight)
        {
            bool[] textureMask = new bool[textureWidth * textureHeight];
            for (int y = 0; y < textureHeight; y++)
            {
                int cellY = Mathf.Clamp(y / texelsPerCell, 0, cellHeight - 1);
                float localY = ((y % texelsPerCell) + 0.5f) / texelsPerCell;
                int row = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    int cellX = Mathf.Clamp(x / texelsPerCell, 0, cellWidth - 1);
                    int cellIndex = Index(cellX, cellY, cellWidth);
                    if (!water[cellIndex])
                    {
                        continue;
                    }

                    float localX = ((x % texelsPerCell) + 0.5f) / texelsPerCell;
                    Sprite sprite = sprites[cellIndex];
                    if (sprite == null)
                    {
                        textureMask[row + x] = true;
                        continue;
                    }

                    Vector3Int cell = new Vector3Int(bounds.xMin + cellX, bounds.yMin + cellY, 0);
                    textureMask[row + x] = IsInsideSpriteMask(sprite, cell, inverseTileTransforms[cellIndex], localOrigins[cellIndex], localX, localY);
                }
            }

            return textureMask;
        }

        private bool IsInsideSpriteMask(Sprite sprite, Vector3Int cell, Matrix4x4 inverseTileTransform, Vector3 localOrigin, float localX, float localY)
        {
            if (useSpritePhysicsShape && TrySampleSpritePhysicsShape(sprite, cell, inverseTileTransform, localOrigin, localX, localY, out bool insideShape))
            {
                return insideShape;
            }

            return SampleSpriteAlpha(sprite, localX, localY) >= alphaThreshold;
        }

        private bool TrySampleSpritePhysicsShape(Sprite sprite, Vector3Int cell, Matrix4x4 inverseTileTransform, Vector3 localOrigin, float localX, float localY, out bool insideShape)
        {
            insideShape = false;

            List<Vector2>[] shapes = GetSpritePhysicsShapes(sprite);
            if (shapes == null || shapes.Length == 0)
            {
                return false;
            }

            Vector3 sampleLocal = waterTilemap.CellToLocalInterpolated((Vector3)cell + new Vector3(localX, localY, 0f));
            Vector3 spriteLocal = inverseTileTransform.MultiplyPoint3x4(sampleLocal - localOrigin);
            Vector2 point = new Vector2(spriteLocal.x, spriteLocal.y);

            for (int i = 0; i < shapes.Length; i++)
            {
                if (PointInPolygon(point, shapes[i]))
                {
                    insideShape = true;
                    return true;
                }
            }

            return true;
        }

        private List<Vector2>[] GetSpritePhysicsShapes(Sprite sprite)
        {
            if (_spritePhysicsShapes.TryGetValue(sprite, out List<Vector2>[] cachedShapes))
            {
                return cachedShapes;
            }

            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0)
            {
                _spritePhysicsShapes[sprite] = null;
                return null;
            }

            List<Vector2>[] shapes = new List<Vector2>[shapeCount];
            for (int i = 0; i < shapeCount; i++)
            {
                List<Vector2> points = new List<Vector2>();
                sprite.GetPhysicsShape(i, points);
                shapes[i] = points;
            }

            _spritePhysicsShapes[sprite] = shapes;
            return shapes;
        }

        // Ray-casting point-in-polygon test; points exactly on an edge count as inside.
        private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3)
            {
                return false;
            }

            bool inside = false;
            int previous = polygon.Count - 1;
            for (int current = 0; current < polygon.Count; current++)
            {
                Vector2 a = polygon[current];
                Vector2 b = polygon[previous];
                if (IsPointOnSegment(point, a, b))
                {
                    return true;
                }

                bool crossesY = a.y > point.y != b.y > point.y;
                if (crossesY)
                {
                    float denominator = b.y - a.y;
                    if (Mathf.Abs(denominator) > 0.000001f)
                    {
                        float xAtY = (b.x - a.x) * (point.y - a.y) / denominator + a.x;
                        if (point.x < xAtY)
                        {
                            inside = !inside;
                        }
                    }
                }

                previous = current;
            }

            return inside;
        }

        private static bool IsPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            Vector2 toPoint = point - a;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= 0.0000001f)
            {
                return toPoint.sqrMagnitude <= 0.0000001f;
            }

            float t = Vector2.Dot(toPoint, segment) / segmentLengthSquared;
            if (t < 0f || t > 1f)
            {
                return false;
            }

            Vector2 closest = a + segment * t;
            return (point - closest).sqrMagnitude <= 0.000001f;
        }

        private float SampleSpriteAlpha(Sprite sprite, float localX, float localY)
        {
            Texture2D texture = sprite.texture;
            if (texture == null)
            {
                return 1f;
            }

            if (!_texturePixels.TryGetValue(texture, out Color32[] pixels))
            {
                pixels = CopyTexturePixels(texture);
                _texturePixels[texture] = pixels;
            }

            if (pixels == null || pixels.Length == 0)
            {
                return 1f;
            }

            Rect rect = sprite.textureRect;
            float px = rect.x + Mathf.Clamp01(localX) * Mathf.Max(0f, rect.width - 1f);
            float py = rect.y + Mathf.Clamp01(localY) * Mathf.Max(0f, rect.height - 1f);
            return SampleAlphaBilinear(pixels, texture.width, texture.height, px, py);
        }

        private static float SampleAlphaBilinear(Color32[] pixels, int width, int height, float x, float y)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, height - 1);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = Mathf.Clamp01(x - x0);
            float ty = Mathf.Clamp01(y - y0);

            float a00 = pixels[Index(x0, y0, width)].a / 255f;
            float a10 = pixels[Index(x1, y0, width)].a / 255f;
            float a01 = pixels[Index(x0, y1, width)].a / 255f;
            float a11 = pixels[Index(x1, y1, width)].a / 255f;
            return Mathf.Lerp(Mathf.Lerp(a00, a10, tx), Mathf.Lerp(a01, a11, tx), ty);
        }

        // Reads pixels from a (possibly non-readable) texture via a temporary RenderTexture blit.
        private static Color32[] CopyTexturePixels(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D readable = null;

            try
            {
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
                readable.Apply(false, false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);

                if (readable != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(readable);
                    }
                    else
                    {
                        DestroyImmediate(readable);
                    }
                }
            }
        }

        private void CacheComponents()
        {
            _propertyBlock ??= new MaterialPropertyBlock();

            if (waterTilemap == null)
            {
                waterTilemap = GetComponent<Tilemap>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<TilemapRenderer>();
            }
        }

        // Approximate Euclidean distance from each water texel to the nearest shore,
        // computed with a two-pass (forward/backward) chamfer distance transform.
        private float[] BuildDistanceField(bool[] water, int width, int height)
        {
            const float large = 100000f;
            float[] dist = new float[water.Length];

            // Seed shore texels (water adjacent to land/edge) with distance 0, everything else with a large value.

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = Index(x, y, width);
                    if (!water[i])
                    {
                        dist[i] = large;
                        continue;
                    }

                    dist[i] = IsShoreCell(water, width, height, x, y) ? 0f : large;
                }
            }

            // Forward pass: propagate distances from the top-left neighbours.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = Index(x, y, width);
                    if (!water[i])
                    {
                        continue;
                    }

                    Relax(dist, water, width, height, x, y, -1, 0, 1f);
                    Relax(dist, water, width, height, x, y, 0, -1, 1f);
                    Relax(dist, water, width, height, x, y, -1, -1, 1.4142135f);
                    Relax(dist, water, width, height, x, y, 1, -1, 1.4142135f);
                }
            }

            // Backward pass: propagate distances from the bottom-right neighbours.
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    int i = Index(x, y, width);
                    if (!water[i])
                    {
                        continue;
                    }

                    Relax(dist, water, width, height, x, y, 1, 0, 1f);
                    Relax(dist, water, width, height, x, y, 0, 1, 1f);
                    Relax(dist, water, width, height, x, y, 1, 1, 1.4142135f);
                    Relax(dist, water, width, height, x, y, -1, 1, 1.4142135f);
                }
            }

            return dist;
        }

        // A water texel is shore if any 8-neighbour is land or outside the grid.
        private static bool IsShoreCell(bool[] water, int width, int height, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || !water[Index(nx, ny, width)])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Relaxes the distance at (x,y) against a neighbour offset by (ox,oy) plus the step cost.
        private static void Relax(float[] dist, bool[] water, int width, int height, int x, int y, int ox, int oy, float cost)
        {
            int nx = x + ox;
            int ny = y + oy;
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            {
                return;
            }

            int i = Index(x, y, width);
            int n = Index(nx, ny, width);
            if (water[n])
            {
                dist[i] = Mathf.Min(dist[i], dist[n] + cost);
            }
        }

        private void EnsureTexture(int width, int height)
        {
            if (_shoreDataTexture != null && _shoreDataTexture.width == width && _shoreDataTexture.height == height)
            {
                return;
            }

            ReleaseTexture();
            _shoreDataTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Water2D Shore Data",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void ApplyTexture(Texture texture, Vector4 worldRect)
        {
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetTexture(ShoreDataTexId, texture);
            _propertyBlock.SetVector(ShoreDataWorldRectId, worldRect);
            _propertyBlock.SetFloat(UseShoreDataId, 1f);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        // Disables shore data on the shader (used when there is no water to bake).
        private void ApplyEmptyData()
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(UseShoreDataId, 0f);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ReleaseTexture()
        {
            if (_shoreDataTexture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_shoreDataTexture);
            }
            else
            {
                DestroyImmediate(_shoreDataTexture);
            }

            _shoreDataTexture = null;
            _shoreDataPixels = null;
        }

        // Hash of all inputs that affect the bake, used to skip re-baking when nothing changed.
        private int ComputeBakeHash()
        {
            if (waterTilemap == null)
            {
                return 0;
            }

            BoundsInt bounds = waterTilemap.cellBounds;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + bounds.xMin;
                hash = hash * 31 + bounds.yMin;
                hash = hash * 31 + bounds.size.x;
                hash = hash * 31 + bounds.size.y;
                hash = hash * 31 + texelsPerCell;
                hash = hash * 31 + (useSpritePhysicsShape ? 1 : 0);
                hash = hash * 31 + Mathf.RoundToInt(alphaThreshold * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(normalizeDistanceCells * 100f);
                hash = hash * 31 + Mathf.RoundToInt(shoreBandCells * 100f);

                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        TileBase tile = waterTilemap.GetTile(new Vector3Int(x, y, 0));
                        if (tile == null)
                        {
                            continue;
                        }

                        hash = hash * 31 + x;
                        hash = hash * 31 + y;
                        hash = hash * 31 + tile.GetEntityId().GetHashCode();

                        Sprite sprite = waterTilemap.GetSprite(new Vector3Int(x, y, 0));
                        if (sprite != null)
                        {
                            hash = hash * 31 + sprite.GetEntityId().GetHashCode();
                        }
                    }
                }

                return hash;
            }
        }

        private static int Index(int x, int y, int width)
        {
            return y * width + x;
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }

#if UNITY_EDITOR
        // Editor-only watcher: periodically re-bakes when the tilemap or settings change.
        private void EditorUpdate()
        {
            if (!autoBake || Application.isPlaying || this == null)
            {
                return;
            }

            // Throttle the change check so we don't hash the tilemap every editor frame.
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextEditorBakeCheckTime)
            {
                return;
            }

            _nextEditorBakeCheckTime = now + 0.35d;
            CacheComponents();
            int hash = ComputeBakeHash();
            if (hash != _lastHash)
            {
                BakeAndApply();
            }
        }
#endif
    }
}
