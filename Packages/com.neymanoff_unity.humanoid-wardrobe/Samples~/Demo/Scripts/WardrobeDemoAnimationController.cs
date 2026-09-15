using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Preview controller for toggling between static fitting pose (T-Pose / A-Pose)
    /// and active gameplay animations directly from the UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class WardrobeDemoAnimationController : MonoBehaviour
    {
        [Header("Target Character")]
        [Tooltip("The character Animator component.")]
        [SerializeField] private Animator characterAnimator;

        [Header("Animation State Names")]
        [Tooltip("Name of the static fitting state in the Animator Controller (e.g. 'TPose').")]
        [SerializeField] private string fittingStateName = "TPose";

        [Tooltip("Name of the active motion state in the Animator Controller (e.g. 'Idle').")]
        [SerializeField] private string idleStateName = "Idle_2";

        private void Start()
        {
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }
        }

        /// <summary>
        /// Switches character to the static fitting pose (T-Pose / A-Pose).
        /// </summary>
        public void PlayFittingPose()
        {
            if (characterAnimator == null) return;
            characterAnimator.Play(fittingStateName);
        }

        /// <summary>
        /// Switches character to the active idle animation.
        /// </summary>
        public void PlayIdleAnimation()
        {
            if (characterAnimator == null) return;
            characterAnimator.Play(idleStateName);
        }

        /// <summary>
        /// Plays an arbitrary state by its name in the Animator Controller.
        /// </summary>
        public void PlayState(string stateName)
        {
            if (characterAnimator == null || string.IsNullOrEmpty(stateName)) return;
            characterAnimator.Play(stateName);
        }
    }
}