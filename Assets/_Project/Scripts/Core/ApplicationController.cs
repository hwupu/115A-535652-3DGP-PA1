using BoomerangGuardian.Player;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Application-level controls (PB-17): ESC quits the game.
    /// In the Editor it exits Play Mode instead, because Application.Quit() is ignored there.
    /// It does not read the keyboard itself: PlayerInputReader (Input System) raises QuitPressed.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [Tooltip("Input source (on the Player).")]
        [SerializeField] private PlayerInputReader input;

        // Subscribe / unsubscribe in the OnEnable / OnDisable pair to avoid dangling event handlers.
        private void OnEnable()
        {
            if (input != null) input.QuitPressed += Quit;
        }

        private void OnDisable()
        {
            if (input != null) input.QuitPressed -= Quit;
        }

        /// <summary>Quits the built game, or stops Play Mode in the Editor.</summary>
        public void Quit()
        {
            Debug.Log("Quit requested (ESC).");
            // Conditional compilation: UnityEditor code is stripped from builds, so it must be guarded.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
