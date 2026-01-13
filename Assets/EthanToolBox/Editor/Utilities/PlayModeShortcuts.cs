using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor.Utilities
{
    public static class PlayModeShortcuts
    {
        // Default shortcut is F5. Users can rebind this in Edit > Shortcuts.
        [MenuItem("EthanToolBox/Shortcuts/Play and Maximize _F1")]
        public static void PlayAndMaximize()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;

                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView != null)
                {
                    gameView.maximized = false;
                }
            }
            else
            {
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);

                gameView.Focus();

                gameView.maximized = true;

                EditorApplication.isPlaying = true;
            }
        }
    }
}
