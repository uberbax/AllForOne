using System.Collections.Generic;
using UnityEngine;

namespace BRATANDRONIKLIB
{
    /// <summary>Local cinematic presentation; does not apply damage or change game time.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BRATViewBattleReset))]
    public sealed class BRATEffectBattle : MonoBehaviour
    {
        [Header("Scene references")]
        public Camera targetCamera;
        public SpriteRenderer hero;
        public SpriteRenderer enemy;
        public SpriteRenderer healer;
        public SpriteRenderer[] sceneSprites;
        public Material spriteEffectMaterial;
        public Material vfxMaterial;

        [Header("Framing")]
        [Range(0.55f, 1f)] public float zoom = 0.78f;
        [Range(1f, 1.4f)] public float foregroundScale = 1.12f;
        [Range(0f, 1f)] public float backgroundBrightness = 0.44f;
        [Range(0f, 4f)] public float backgroundBlur = 2.4f;
        [Min(0f)] public float shake = 0.075f;
        [Min(0f)] public float lunge = 1.0f;

        [Header("Timing, seconds")]
        [Min(0.01f)] public float focusTime = 0.18f;
        [Min(0.01f)] public float strikeTime = 0.12f;
        [Min(0f)] public float impactPause = 0.09f;
        [Min(0.01f)] public float holdTime = 0.38f;
        [Min(0.01f)] public float returnTime = 0.32f;

        private static readonly int TintId = Shader.PropertyToID("_BattleTint");
        private static readonly int BlurId = Shader.PropertyToID("_BattleBlur");
        private static readonly int FlashId = Shader.PropertyToID("_BattleFlash");
        private BRATViewBattleReset _reset;
        private BRATViewBattleVfx _vfx;
        private SpriteState[] _sprites;
        private ActorState _hero;
        private ActorState _enemy;
        private ActorState _healer;
        private Camera _camera;
        private Vector3 _cameraPosition;
        private Quaternion _cameraRotation;
        private float _cameraSize;
        private float _cameraFov;
        private float _elapsed;
        private double _startedAt;
        private Effect _effect;
        private bool _ready;
        private bool _running;

        private enum Effect { Strike, HeavyStrike, Heal }

        private sealed class ActorState
        {
            public SpriteRenderer renderer;
            public Transform transform;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Vector3 foot;
            public Vector3 footLocal;
            public Vector3 center;

            public ActorState(SpriteRenderer value)
            {
                renderer = value;
                if (!value) return;
                transform = value.transform;
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
                center = value.bounds.center;
                foot = center - Vector3.up * (value.bounds.extents.y * 0.72f);
                footLocal = transform.InverseTransformPoint(foot);
            }

            public void Restore()
            {
                if (!transform) return;
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }

            public void Pose(Vector3 offset, float enlargement, float angle)
            {
                if (!transform) return;
                Restore();
                transform.localScale = scale * enlargement;
                transform.localRotation = rotation * Quaternion.Euler(0f, 0f, angle);
                transform.position += foot + offset - transform.TransformPoint(footLocal);
            }
        }

        private sealed class SpriteState
        {
            public SpriteRenderer renderer;
            public Material material;
            public MaterialPropertyBlock original;
            public MaterialPropertyBlock working;
            public bool hadBlock;
            public int sortingOrder;
            public int sortingLayer;

            public SpriteState(SpriteRenderer value)
            {
                renderer = value;
                material = value.sharedMaterial;
                original = new MaterialPropertyBlock();
                working = new MaterialPropertyBlock();
                hadBlock = value.HasPropertyBlock();
                value.GetPropertyBlock(original);
                value.GetPropertyBlock(working);
                sortingOrder = value.sortingOrder;
                sortingLayer = value.sortingLayerID;
            }

            public void Restore()
            {
                if (!renderer) return;
                renderer.sharedMaterial = material;
                renderer.SetPropertyBlock(hadBlock ? original : null);
                renderer.sortingOrder = sortingOrder;
                renderer.sortingLayerID = sortingLayer;
            }
        }

        private void Awake()
        {
            _reset = GetComponent<BRATViewBattleReset>();
            _camera = targetCamera;
            _hero = new ActorState(hero);
            _enemy = new ActorState(enemy);
            _healer = new ActorState(healer);
            var sprites = new List<SpriteState>();
            var seen = new HashSet<SpriteRenderer>();
            if (sceneSprites != null)
                for (int i = 0; i < sceneSprites.Length; i++)
                    Cache(sceneSprites[i], sprites, seen);
            Cache(hero, sprites, seen);
            Cache(enemy, sprites, seen);
            Cache(healer, sprites, seen);
            _sprites = sprites.ToArray();
            if (_camera)
            {
                _cameraPosition = _camera.transform.localPosition;
                _cameraRotation = _camera.transform.localRotation;
                _cameraSize = _camera.orthographicSize;
                _cameraFov = _camera.fieldOfView;
            }
            if (vfxMaterial) _vfx = new BRATViewBattleVfx(transform, vfxMaterial);
            _ready = _camera && spriteEffectMaterial && vfxMaterial;
        }

        private static void Cache(SpriteRenderer sprite, List<SpriteState> items, HashSet<SpriteRenderer> seen)
        {
            if (sprite && sprite.gameObject.activeInHierarchy && seen.Add(sprite))
                items.Add(new SpriteState(sprite));
        }

        private void OnEnable()
        {
            if (_reset) _reset.BeforeReset += StopAndRestore;
        }

        public void PlayStrike() => Play(Effect.Strike);
        public void PlayHeavyStrike() => Play(Effect.HeavyStrike);
        public void PlayHeal() => Play(Effect.Heal);

        private void Play(Effect effect)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            StopAndRestore();
            if (!_ready || !_camera || !_hero.renderer ||
                (effect == Effect.Heal ? !_healer.renderer : !_enemy.renderer)) return;
            if (!_hero.renderer.gameObject.activeInHierarchy ||
                !(effect == Effect.Heal ? _healer.renderer : _enemy.renderer).gameObject.activeInHierarchy) return;

            _effect = effect;
            _elapsed = 0f;
            _startedAt = Time.realtimeSinceStartupAsDouble;
            _running = true;
            for (int i = 0; i < _sprites.Length; i++)
                if (_sprites[i].renderer) _sprites[i].renderer.sharedMaterial = spriteEffectMaterial;
            ApplyFrame();
        }

        private void LateUpdate()
        {
            if (!_running) return;
            if (!_camera || !_hero.renderer ||
                (_effect == Effect.Heal ? !_healer.renderer : !_enemy.renderer))
            {
                StopAndRestore();
                return;
            }
            _elapsed = (float)(Time.realtimeSinceStartupAsDouble - _startedAt);
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            bool heal = _effect == Effect.Heal;
            bool heavy = _effect == Effect.HeavyStrike;
            float focusDuration = Mathf.Max(0.01f, focusTime);
            float strikeDuration = Mathf.Max(0.01f, strikeTime);
            float returnDuration = Mathf.Max(0.01f, returnTime);
            float hitTime = focusDuration + strikeDuration;
            float releaseTime = hitTime + Mathf.Max(0f, impactPause) + Mathf.Max(0.01f, holdTime) + (heal ? 0.45f : 0f);
            if (_elapsed >= releaseTime + returnDuration)
            {
                StopAndRestore();
                return;
            }

            float returning = Smooth((_elapsed - releaseTime) / returnDuration);
            float focus = EaseOut(_elapsed / focusDuration) * (1f - returning);
            float strike = EaseOut((_elapsed - focusDuration) / strikeDuration);
            float afterHit = _elapsed - hitTime;
            // A local held pose provides hit-stop; the scene's time scale is untouched.
            float afterPause = Mathf.Max(0f, afterHit - Mathf.Max(0f, impactPause));
            float impact = afterHit < 0f ? 0f : Mathf.Clamp01(1f - afterPause / (heavy ? 0.36f : 0.26f)) * (1f - returning);
            float recoil = impact * Mathf.Sin(Mathf.Clamp01(afterPause / 0.18f) * Mathf.PI * 0.6f);
            ActorState source = heavy ? _enemy : heal ? _healer : _hero;
            ActorState target = heavy || heal ? _hero : _enemy;
            float direction = target.center.x >= source.center.x ? 1f : -1f;
            float grow = Mathf.Lerp(1f, foregroundScale, focus);

            _hero.Restore();
            _enemy.Restore();
            _healer.Restore();
            if (heal)
            {
                source.Pose(new Vector3(0.28f * focus, -0.08f * focus, 0f), grow, -2f * focus);
                target.Pose(new Vector3(-0.10f * focus, -0.08f * focus, 0f), grow, 1.2f * focus);
            }
            else
            {
                float windup = Mathf.Sin(Mathf.Clamp01(_elapsed / focusDuration) * Mathf.PI);
                float travel = (0.22f * focus + lunge * (heavy ? 1.1f : 1f) * strike * (1f - returning) - 0.22f * windup);
                source.Pose(new Vector3(direction * travel, -0.08f * focus, 0f), grow,
                    direction * (4f * windup - (heavy ? 7f : 5f) * strike * (1f - returning)));
                target.Pose(new Vector3(direction * (0.15f * focus + recoil * (heavy ? 0.55f : 0.3f)), -0.06f * focus, 0f), grow,
                    -direction * recoil * (heavy ? 9f : 5f));
            }

            float pulse = heal ? Smooth(afterHit / 0.16f) * (1f - Smooth((afterHit - 0.55f) / 0.45f)) : 0f;
            float flash = impact * Mathf.Clamp01(1f - afterPause / 0.085f);
            float tremor = heal ? 0f : impact * (afterHit >= impactPause ? 1f : 0f);
            ApplyCamera(source, target, focus, tremor, heavy, direction);
            ApplySprites(source.renderer, target.renderer, focus, flash, impact, pulse, heal);
            Vector3 center = target.renderer.bounds.center;
            _vfx?.Draw(_camera, center, focus, heal ? 0f : impact, pulse,
                afterPause, heavy);
        }

        private void ApplyCamera(ActorState source, ActorState target, float focus, float tremor, bool heavy, float direction)
        {
            Vector3 center = (source.center + target.center) * 0.5f;
            center.x = Mathf.Clamp(center.x + (heavy ? -0.25f : 0.25f), -1.65f, 1.65f);
            center.y -= 0.48f;
            Transform ct = _camera.transform;
            Vector3 baseWorld = ct.parent ? ct.parent.TransformPoint(_cameraPosition) : _cameraPosition;
            Vector3 destination = new Vector3(center.x, center.y, baseWorld.z);
            Vector3 noise = new Vector3(Mathf.Sin(_elapsed * 153f), Mathf.Cos(_elapsed * 117f) * 0.65f, 0f)
                * (shake * (heavy ? 1.65f : 1f) * tremor);
            ct.position = Vector3.Lerp(baseWorld, destination, focus) + noise;
            ct.localRotation = _cameraRotation * Quaternion.Euler(0f, 0f,
                (direction * (heavy ? 0.75f : 0.4f) + Mathf.Sin(_elapsed * 132f) * tremor * 0.4f) * focus);
            float magnification = Mathf.Lerp(1f, Mathf.Clamp(zoom - (heavy ? 0.04f : 0f), 0.55f, 1f), focus);
            _camera.orthographicSize = Mathf.Max(0.01f, _cameraSize * magnification);
            _camera.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(_cameraFov * Mathf.Deg2Rad * 0.5f) * magnification) * Mathf.Rad2Deg;
        }

        private void ApplySprites(SpriteRenderer source, SpriteRenderer target, float focus, float flash, float impact, float healing, bool heal)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                SpriteState state = _sprites[i];
                if (!state.renderer) continue;
                bool foreground = state.renderer == source || state.renderer == target;
                float brightness = foreground ? 1f : Mathf.Lerp(1f, backgroundBrightness, focus);
                Color tint = new Color(brightness, brightness, brightness, 1f);
                Color fill = Color.clear;
                if (state.renderer == target)
                    fill = heal ? new Color(1f, 0.78f, 0.24f, healing * 0.20f)
                        : Color.Lerp(new Color(0.9f, 0.08f, 0.035f, impact * 0.42f),
                            new Color(1f, 0.95f, 0.8f, 0.88f), flash);
                if (heal && state.renderer == source) fill = new Color(1f, 0.78f, 0.3f, healing * 0.045f);
                state.working.SetColor(TintId, tint);
                state.working.SetFloat(BlurId, foreground ? 0f : backgroundBlur * focus);
                state.working.SetColor(FlashId, fill);
                state.renderer.SetPropertyBlock(state.working);
                state.renderer.sortingOrder = foreground ? state.sortingOrder + 100 : state.sortingOrder;
            }
        }

        private void StopAndRestore()
        {
            bool wasRunning = _running;
            _running = false;
            _elapsed = 0f;
            _vfx?.Clear();
            if (!wasRunning) return;
            _hero?.Restore();
            _enemy?.Restore();
            _healer?.Restore();
            if (_sprites != null)
                for (int i = 0; i < _sprites.Length; i++) _sprites[i].Restore();
            if (_camera)
            {
                _camera.transform.localPosition = _cameraPosition;
                _camera.transform.localRotation = _cameraRotation;
                _camera.orthographicSize = _cameraSize;
                _camera.fieldOfView = _cameraFov;
            }
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float EaseOut(float value)
        {
            value = 1f - Mathf.Clamp01(value);
            return 1f - value * value * value;
        }

        private void OnDisable()
        {
            if (_reset) _reset.BeforeReset -= StopAndRestore;
            StopAndRestore();
        }

        private void OnDestroy()
        {
            _vfx?.Dispose();
            _vfx = null;
        }
    }
}
