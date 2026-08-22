using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Demo animation & pose controller for item fitting.
    /// </summary>
    [DisallowMultipleComponent]
    public class WardrobeDemoAnimationController : MonoBehaviour
    {
        [Header("Target Character")]
        [SerializeField] private Animator characterAnimator;
        
        [Header("State Settings")]
        [Tooltip("When enabled, freezes character in static A-Pose / Bind pose for exact item fitting.")]
        [SerializeField] private bool isFittingMode = false;
        
        public bool IsFittingMode => isFittingMode;

        private void Start()
        {
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }

            ApplyState();
        }

        /// <summary>
        /// Toggles between static Fitting Mode and active Animation Playback.
        /// </summary>
        public void ToggleFittingMode(bool enableFittingMode)
        {
            isFittingMode = enableFittingMode;
            ApplyState();
        }

        /// <summary>
        /// Plays a specific animation state by name the Animator Controller
        /// </summary>
        public void PlayAnimationState(string stateName)
        {
            if (characterAnimator == null) return;

            if (isFittingMode)
            {
                isFittingMode = false;
                characterAnimator.enabled = true;
            }
            
            characterAnimator.Play(stateName);
        }

        private void ApplyState()
        {
            if (characterAnimator == null) return;

            characterAnimator.enabled = !isFittingMode;
        }
    }
}