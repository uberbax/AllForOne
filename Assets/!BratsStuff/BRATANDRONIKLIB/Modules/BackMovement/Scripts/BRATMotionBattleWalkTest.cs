using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATMotionBattleWalkTest : MonoBehaviour
    {
        [SerializeField] private Camera movementCamera;
        [SerializeField] private SpriteRenderer[] heroes;
        [SerializeField, Min(0f)] private float speed = 2.3f;

        public float TravelX { get; private set; }
        private Vector3 cameraHome;
        private Vector3[] heroHomes;

        private void OnEnable()
        {
            if (!Application.isEditor || !movementCamera || heroes == null || heroes.Length == 0)
            {
                enabled = false;
                return;
            }

            cameraHome = movementCamera.transform.position;
            heroHomes = new Vector3[heroes.Length];
            for (int i = 0; i < heroes.Length; i++)
            {
                if (!heroes[i])
                {
                    enabled = false;
                    return;
                }
                heroHomes[i] = heroes[i].transform.position;
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                MoveForward(Time.unscaledDeltaTime);
        }

        private void MoveForward(float deltaTime)
        {
            if (!movementCamera || !movementCamera.isActiveAndEnabled)
                return;

            foreach (var hero in heroes)
            {
                if (!hero || !hero.enabled || !hero.gameObject.activeInHierarchy)
                {
                    enabled = false;
                    return;
                }
            }

            TravelX += speed * deltaTime;
            ApplyTravel();
        }

        public void RestartWalk()
        {
            if (!isActiveAndEnabled || heroHomes == null)
                return;

            TravelX = 0f;
            ApplyTravel();
        }

        private void ApplyTravel()
        {
            Vector3 offset = Vector3.right * TravelX;
            if (movementCamera)
                movementCamera.transform.position = cameraHome + offset;
            for (int i = 0; i < heroHomes.Length; i++)
                if (heroes[i])
                    heroes[i].transform.position = heroHomes[i] + offset;
        }

        private void OnDisable()
        {
            if (TravelX != 0f && heroHomes != null)
            {
                TravelX = 0f;
                ApplyTravel();
            }
            heroHomes = null;
        }
    }
}
