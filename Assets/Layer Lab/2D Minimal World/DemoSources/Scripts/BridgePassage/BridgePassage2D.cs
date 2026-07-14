using System.Collections.Generic;
using UnityEngine;

namespace LayerLab
{
    /// <summary>
    /// Trigger volume placed over a bridge that lets the player walk across water.
    /// While a player overlaps the bridge trigger, its colliders (and optionally the
    /// whole player/water layer pair) stop colliding with the configured water colliders,
    /// and the player can optionally be clamped inside the bridge width so it cannot
    /// fall off the side. Supports multiple simultaneous players via overlap counting.
    /// </summary>
    public sealed class BridgePassage2D : MonoBehaviour
    {
        // Which axis defines the bridge "width" used to clamp the player on the bridge.
        private enum BridgeWidthAxis
        {
            Auto,
            ClampX,
            ClampY
        }

        [SerializeField] private Collider2D[] waterColliders;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool ignoreWaterLayerWhileOnBridge = true;
        [SerializeField] private string playerLayerName = "Player";
        [SerializeField] private string waterLayerName = "Water";
        [SerializeField] private bool keepPlayerInsideBridgeWidth = true;
        [SerializeField] private Collider2D bridgeBoundsCollider;
        [SerializeField] private BridgeWidthAxis bridgeWidthAxis = BridgeWidthAxis.Auto;
        [SerializeField] private float bridgeEdgePadding = 0.05f;
        [SerializeField] private bool logDebugMessages;

        // Per-player state keyed by the player root's instance id, so several players
        // can be on the bridge at once without disturbing each other.
        private readonly Dictionary<int, int> playerOverlapCounts = new();
        private readonly Dictionary<int, Collider2D[]> playerCollidersById = new();
        private readonly Dictionary<int, Rigidbody2D> playerBodiesById = new();
        private readonly Dictionary<int, Transform> playerTransformsById = new();
        private readonly Dictionary<int, Vector2> lastBridgePositionsById = new();

        private int playerLayer = -1;
        private int waterLayer = -1;
        private Collider2D bridgeTrigger;

        private void Awake()
        {
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            waterLayer = LayerMask.NameToLayer(waterLayerName);
            // Use the explicit bounds collider if assigned, otherwise the collider on this object.
            bridgeTrigger = bridgeBoundsCollider != null ? bridgeBoundsCollider : GetComponent<Collider2D>();
        }

        private void Reset()
        {
            // Ensure the bridge collider acts as a trigger when the component is added.
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryGetPlayerColliders(other, out int playerId, out Collider2D[] playerColliders, out Rigidbody2D playerBody, out Transform playerTransform))
                return;

            playerOverlapCounts.TryGetValue(playerId, out int count);
            playerOverlapCounts[playerId] = count + 1;

            // Only set up collision ignoring on the first overlapping collider of this player.
            if (count == 0)
            {
                playerCollidersById[playerId] = playerColliders;
                playerBodiesById[playerId] = playerBody;
                playerTransformsById[playerId] = playerTransform;
                lastBridgePositionsById[playerId] = GetPlayerPosition(playerBody, playerTransform);
                SetWaterCollision(playerColliders, ignore: true);
                SetWaterLayerCollision(ignore: true);
                Log($"Entered bridge: {other.name}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetPlayerColliders(other, out int playerId, out _, out _, out _))
                return;

            if (!playerOverlapCounts.TryGetValue(playerId, out int count))
                return;

            count--;
            // Still overlapping with another of the player's colliders: keep state.
            if (count > 0)
            {
                playerOverlapCounts[playerId] = count;
                return;
            }

            playerOverlapCounts.Remove(playerId);

            // Last collider left the bridge: restore water collision for this player.
            if (playerCollidersById.TryGetValue(playerId, out Collider2D[] playerColliders))
            {
                SetWaterCollision(playerColliders, ignore: false);
                playerCollidersById.Remove(playerId);
            }

            playerBodiesById.Remove(playerId);
            playerTransformsById.Remove(playerId);
            lastBridgePositionsById.Remove(playerId);

            // Restore the layer-wide ignore only once no players remain on the bridge.
            if (playerOverlapCounts.Count == 0)
                SetWaterLayerCollision(ignore: false);

            Log($"Exited bridge: {other.name}");
        }

        private void FixedUpdate()
        {
            if (!keepPlayerInsideBridgeWidth || bridgeTrigger == null)
                return;

            // Compute the clamp range along the bridge width axis (with edge padding).
            Bounds bounds = bridgeTrigger.bounds;
            bool clampX = ShouldClampX(bounds);
            float min = clampX ? bounds.min.x + bridgeEdgePadding : bounds.min.y + bridgeEdgePadding;
            float max = clampX ? bounds.max.x - bridgeEdgePadding : bounds.max.y - bridgeEdgePadding;

            if (min > max)
                return;

            foreach (int playerId in playerOverlapCounts.Keys)
            {
                if (!playerTransformsById.TryGetValue(playerId, out Transform playerTransform) || playerTransform == null)
                    continue;

                Rigidbody2D body = playerBodiesById.TryGetValue(playerId, out Rigidbody2D foundBody) ? foundBody : null;
                Vector2 position = GetPlayerPosition(body, playerTransform);
                float current = clampX ? position.x : position.y;

                // Already within the bridge width: just remember the position.
                if (current >= min && current <= max)
                {
                    lastBridgePositionsById[playerId] = position;
                    continue;
                }

                Vector2 clampedPosition = position;
                float clamped = Mathf.Clamp(current, min, max);

                if (clampX)
                    clampedPosition.x = clamped;
                else
                    clampedPosition.y = clamped;

                // Skip negligible corrections to avoid fighting the physics solver.
                if ((clampedPosition - position).sqrMagnitude < 0.000001f)
                    continue;

                // Prefer MovePosition for dynamic bodies so movement stays physics-friendly.
                if (body != null && body.bodyType != RigidbodyType2D.Static)
                    body.MovePosition(clampedPosition);
                else
                    playerTransform.position = clampedPosition;
            }
        }

        // Decides whether the clamp happens along X; in Auto mode the longer side is the
        // bridge length, so the shorter side (width) is clamped.
        private bool ShouldClampX(Bounds bounds)
        {
            if (bridgeWidthAxis == BridgeWidthAxis.ClampX)
                return true;

            if (bridgeWidthAxis == BridgeWidthAxis.ClampY)
                return false;

            return bounds.size.y >= bounds.size.x;
        }

        // Prefer the rigidbody position (physics-accurate) over the transform position.
        private Vector2 GetPlayerPosition(Rigidbody2D body, Transform playerTransform)
        {
            return body != null ? body.position : (Vector2)playerTransform.position;
        }

        // Resolves the player root from a triggering collider, validating the tag, and
        // gathers all child colliders so water collisions can be toggled per player.
        private bool TryGetPlayerColliders(
            Collider2D other,
            out int playerId,
            out Collider2D[] playerColliders,
            out Rigidbody2D playerBody,
            out Transform playerTransform)
        {
            playerBody = other.attachedRigidbody;
            GameObject playerRoot = playerBody != null ? playerBody.gameObject : other.transform.root.gameObject;
            playerTransform = playerRoot.transform;

            bool tagMatches = string.IsNullOrEmpty(playerTag)
                || other.CompareTag(playerTag)
                || playerRoot.CompareTag(playerTag);

            if (!tagMatches)
            {
                playerId = 0;
                playerColliders = null;
                playerBody = null;
                playerTransform = null;
                return false;
            }

            playerId = playerRoot.GetEntityId().GetHashCode();
            playerColliders = playerRoot.GetComponentsInChildren<Collider2D>();

            // Fall back to the triggering collider if no colliders were found on the root.
            if (playerColliders.Length == 0)
                playerColliders = new[] { other };

            return true;
        }

        // Toggles collision ignoring between the player's solid colliders and the water colliders.
        private void SetWaterCollision(Collider2D[] playerColliders, bool ignore)
        {
            if (waterColliders == null)
                return;

            foreach (Collider2D playerCollider in playerColliders)
            {
                if (playerCollider == null || playerCollider.isTrigger)
                    continue;

                foreach (Collider2D waterCollider in waterColliders)
                {
                    if (waterCollider == null)
                        continue;

                    Physics2D.IgnoreCollision(playerCollider, waterCollider, ignore);
                }
            }
        }

        // Toggles the global player/water layer collision when that option is enabled.
        private void SetWaterLayerCollision(bool ignore)
        {
            if (!ignoreWaterLayerWhileOnBridge)
                return;

            if (playerLayer < 0 || waterLayer < 0)
            {
                Log($"Layer not found. playerLayer={playerLayerName}, waterLayer={waterLayerName}");
                return;
            }

            Physics2D.IgnoreLayerCollision(playerLayer, waterLayer, ignore);
        }

        private void Log(string message)
        {
            if (logDebugMessages)
                Debug.Log($"[BridgePassage2D] {message}", this);
        }

        private void OnDisable()
        {
            // Restore all collision state so disabling the bridge never leaves players phased.
            foreach (Collider2D[] playerColliders in playerCollidersById.Values)
                SetWaterCollision(playerColliders, ignore: false);

            SetWaterLayerCollision(ignore: false);
            playerOverlapCounts.Clear();
            playerCollidersById.Clear();
            playerBodiesById.Clear();
            playerTransformsById.Clear();
            lastBridgePositionsById.Clear();
        }
    }
}
