using System;
using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATMotionBattleParallax : MonoBehaviour
    {
        private enum Plane { Sky, DistantCity, Cemetery, Ground, Stone, Soil }

        [Serializable]
        private sealed class Layer
        {
            public Transform target;
            public Plane plane;
            [NonSerialized] public Vector3 home;
        }

        [Header("Relative speed  (0 = fixed on screen, 1 = ground)")]
        [Range(-0.10f, 0.10f)]
        [SerializeField] private float layer01SkyAndClouds = 0f;
        [Range(0f, 0.32f)]
        [SerializeField] private float layer02DistantCity = 0.16f;
        [Range(0.2f, 0.8f)]
        [SerializeField] private float layer03Cemetery = 0.5f;
        [Range(0.85f, 1.15f)]
        [SerializeField] private float layer04PathAndFinalCrypt = 1f;
        [Range(0.90f, 1.14f)]
        [SerializeField] private float layer05StoneLedge = 1.02f;
        [Range(0.90f, 1.18f)]
        [SerializeField] private float layer06ForegroundSoil = 1.04f;

        [Header("Battle background references")]
        [SerializeField] private Camera battleCamera;
        [SerializeField] private Layer[] layers = Array.Empty<Layer>();
        private float cameraStartX;

        private void Awake()
        {
            if (!battleCamera)
                battleCamera = Camera.main;
            if (battleCamera)
                cameraStartX = battleCamera.transform.position.x;
        }

        private void OnEnable()
        {
            foreach (var layer in layers)
                if (layer?.target)
                    layer.home = layer.target.localPosition;

            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!battleCamera)
                return;

            float travel = battleCamera.transform.position.x - cameraStartX;
            foreach (var layer in layers)
            {
                if (layer == null || !layer.target)
                    continue;

                // The camera already moves one unit across a stationary world layer.
                Vector3 offset = Vector3.right * (travel * (1f - Speed(layer.plane)));
                var parent = layer.target.parent;
                Vector3 position = layer.home + (parent ? parent.InverseTransformVector(offset) : offset);
                if (layer.target.localPosition != position)
                    layer.target.localPosition = position;
            }
        }

        private float Speed(Plane plane)
        {
            switch (plane)
            {
                case Plane.Sky: return layer01SkyAndClouds;
                case Plane.DistantCity: return layer02DistantCity;
                case Plane.Cemetery: return layer03Cemetery;
                case Plane.Ground: return layer04PathAndFinalCrypt;
                case Plane.Stone: return layer05StoneLedge;
                case Plane.Soil: return layer06ForegroundSoil;
                default: throw new ArgumentOutOfRangeException(nameof(plane));
            }
        }

        private void OnDisable()
        {
            foreach (var layer in layers)
                if (layer?.target)
                    layer.target.localPosition = layer.home;
        }
    }
}
