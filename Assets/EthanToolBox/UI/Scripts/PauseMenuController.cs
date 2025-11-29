using UnityEngine;
using UnityEngine.SceneManagement;

namespace EthanToolBox.UI.Scripts
{
    public class PauseMenuController : MonoBehaviour
    {
        [Tooltip("The UI Panel to show/hide when pausing.")]
        public GameObject pauseMenuUI;

        [Tooltip("The name of the Main Menu scene.")]
        public string mainMenuSceneName = "MainMenu";

        private bool isPaused = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Resume()
        {
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
        }

        public void Pause()
        {
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            Debug.Log("Quit Game");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
