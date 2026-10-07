using UnityEngine;

namespace Paperbound.Traveller
{
    /// <summary>Paper animation, facing, lantern and audio. Independent of the movement controller.</summary>
    [DisallowMultipleComponent]
    public sealed class TravellerPresentation : MonoBehaviour
    {
        public Animator animator;
        public Transform facing;
        public Light lantern;
        public AudioSource audioSource;
        public AudioClip stepSound;
        public AudioClip foldSound;
        public bool soundEnabled = true;
        [Min(0)] public float lightIntensity = .8f;
        [Min(0)] public float lightFlicker = .08f;
        [Min(0)] public float lightFrequency = 9f;
        public bool IsFolded { get; private set; }
        public bool IsWalking { get; private set; }
        public bool CanMove => !IsFolded && Time.time >= unlockAt;
        float unlockAt;
        Vector3 facingScale = Vector3.one;
        static readonly int Walking = Animator.StringToHash("Walking");
        static readonly int FoldTrigger = Animator.StringToHash("Fold");
        static readonly int UnfoldTrigger = Animator.StringToHash("Unfold");

        void Awake()
        {
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (facing) facingScale = new Vector3(Mathf.Abs(facing.localScale.x), facing.localScale.y, facing.localScale.z);
        }
        void Update()
        {
            if (lantern) lantern.intensity = Mathf.Max(0, lightIntensity + Mathf.Sin(Time.time * lightFrequency) * lightFlicker);
        }
        public void SetWalking(bool walking)
        {
            IsWalking = walking && CanMove;
            if (animator && animator.runtimeAnimatorController) animator.SetBool(Walking, IsWalking);
        }
        public void Face(float horizontalDirection)
        {
            if (facing && Mathf.Abs(horizontalDirection) > .001f)
                facing.localScale = new Vector3(facingScale.x * Mathf.Sign(horizontalDirection), facingScale.y, facingScale.z);
        }
        public void SetFolded(bool folded)
        {
            if (IsFolded == folded) return;
            IsFolded = folded;
            unlockAt = Time.time + .60f;
            SetWalking(false);
            if (animator && animator.runtimeAnimatorController)
            {
                animator.ResetTrigger(folded ? UnfoldTrigger : FoldTrigger);
                animator.SetTrigger(folded ? FoldTrigger : UnfoldTrigger);
            }
            if (soundEnabled && audioSource && foldSound) audioSource.PlayOneShot(foldSound);
        }
        public void PlayStep()
        {
            if (soundEnabled && audioSource && stepSound && CanMove) audioSource.PlayOneShot(stepSound);
        }
        void OnDisable()
        {
            IsWalking = false;
            if (audioSource) audioSource.Stop();
        }
    }
}
