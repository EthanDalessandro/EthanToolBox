using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor.Utilities
{
    public static class PlayModeShortcuts
    {
        // Default shortcut is F5. Users can rebind this in Edit > Shortcuts.
        [MenuItem("EthanToolBox/Shortcuts/Play and Maximize _F5")]
        public static void PlayAndMaximize()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;

                // Un-maximize the window when stopping
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView != null)
                {
                    gameView.maximized = false;
                }
            }
            else
            {
                // Enable "Maximize On Play" to ensure it opens full screen
                // We can't easily force "Maximize" state of the window itself without reflection or "Maximize On Play"
                // But "Maximize On Play" is the standard way.

                // Option 1: Toggle "Maximize On Play"
                // EditorWindow gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                // gameView.maximized = true; // This maximizes the window immediately, not just on play.

                // Better approach: Just start playing. If the user wants maximize, we can enforce "Maximize On Play" setting.
                // But the user specifically asked "se mette automatiquement en pleine écran".

                // Let's force "Maximize On Play" to be true before starting.
                // Note: This is an internal setting, but we can try to find the GameView and set it.

                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);

                // Focus the game view so input goes there
                gameView.Focus();

                // Set MaximizeOnPlay
                // There isn't a simple public API for "Maximize On Play" checkbox, 
                // but maximizing the window itself works.
                gameView.maximized = true;

                EditorApplication.isPlaying = true;
            }
        }
    }
}
