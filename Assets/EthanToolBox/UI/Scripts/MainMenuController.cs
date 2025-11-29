using UnityEngine;
using UnityEngine.SceneManagement;

namespace EthanToolBox.UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("The name of the scene to load when Play is clicked.")]
        public string gameSceneName = "Game";

        public void OnPlayClicked()
        {
            if (LoadingScreenController.Instance != null)
            {
                // Use the Loading Screen if it exists
                AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
                LoadingScreenController.Instance.Show(operation);
            }
            else
            {
                // Fallback to simple load
                SceneManager.LoadScene(gameSceneName);
            }
        }

        public void OnQuitClicked()
        {
            Debug.Log("Quit Game");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
