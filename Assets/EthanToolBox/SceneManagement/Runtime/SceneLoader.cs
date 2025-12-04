using EthanToolBox.Core.DependencyInjection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EthanToolBox.Core.SceneManagement
{
    [Service]
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        public void LoadSceneGroup(SceneGroup group)
        {
            if (group == null || group.Scenes == null || group.Scenes.Count == 0)
            {
                Debug.LogWarning("SceneLoader: SceneGroup is empty or null.");
                return;
            }

            SceneManager.LoadScene(group.Scenes[0].ScenePath, LoadSceneMode.Single);

            for (int i = 1; i < group.Scenes.Count; i++)
            {
                SceneManager.LoadScene(group.Scenes[i].ScenePath, LoadSceneMode.Additive);
            }
        }

        public void UnloadScene(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
