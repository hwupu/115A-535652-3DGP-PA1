using BoomerangGuardian.Player;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Application-level controls (PB-17): ESC quits the game.
    /// In the Editor it exits Play Mode instead, because Application.Quit() is ignored there.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [Tooltip("Input source (on the Player).")]
        [SerializeField] private PlayerInputReader input;

        private void OnEnable()
        {
            if (input != null) input.QuitPressed += Quit;
        }

        private void OnDisable()
        {
            if (input != null) input.QuitPressed -= Quit;
        }

        public void Quit()
        {
            Debug.Log("Quit requested (ESC).");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
