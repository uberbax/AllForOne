using UnityEngine;
namespace IndieImpulseAssets
{
    public class ParticleMaskController : MonoBehaviour
    {
        public GameObject maskObject;
        private RectTransform maskRect;
        private RectTransform particleRect;
        private ParticleSystem particles;
        private bool isPlaying;

        void Start()
        {
            maskRect = maskObject.GetComponent<RectTransform>();
            particleRect = GetComponent<RectTransform>();
            particles = GetComponent<ParticleSystem>();

            // Initial play/stop check
            isPlaying = IsRectTransformInsideMask();
            if (isPlaying)
                particles.Play();
            else
                particles.Stop();
        }

        void Update()
        {
            bool isFullyInside = IsRectTransformInsideMask();
            if (isFullyInside && !isPlaying)
            {
                particles.Play();
                isPlaying = true;
            }
            else if (!isFullyInside && isPlaying)
            {
                particles.Stop();
                particles.Clear(); // Force clear the particles to stop immediately
                isPlaying = false;
            }
        }

        // Check if the entire RectTransform of the Particle System is inside the mask area
        private bool IsRectTransformInsideMask()
        {
            Vector3[] maskCorners = new Vector3[4];
            Vector3[] particleCorners = new Vector3[4];

            maskRect.GetWorldCorners(maskCorners);
            particleRect.GetWorldCorners(particleCorners);

            Vector3 maskBottomLeft = maskCorners[0];
            Vector3 maskTopRight = maskCorners[2];

            Vector3 particleBottomLeft = particleCorners[0];
            Vector3 particleTopRight = particleCorners[2];

            // Check if the Particle RectTransform is entirely inside the Mask RectTransform
            return particleBottomLeft.x >= maskBottomLeft.x &&
                   particleTopRight.x <= maskTopRight.x &&
                   particleBottomLeft.y >= maskBottomLeft.y &&
                   particleTopRight.y <= maskTopRight.y;
        }
    }
}