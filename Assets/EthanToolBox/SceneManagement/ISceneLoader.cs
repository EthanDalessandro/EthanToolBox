using System.Threading.Tasks;

namespace EthanToolBox.Core.SceneManagement
{
    public interface ISceneLoader
    {
        /// <summary>
        /// Loads a single scene by name.
        /// </summary>
        Task LoadSceneAsync(string sceneName, bool showLoadingScreen = false);

        /// <summary>
        /// Loads a scene additively (without unloading the current one).
        /// </summary>
        Task LoadSceneAdditiveAsync(string sceneName);

        /// <summary>
        /// Unloads a specific scene.
        /// </summary>
        Task UnloadSceneAsync(string sceneName);

        /// <summary>
        /// Loads a group of scenes defined in a SceneGroup ScriptableObject.
        /// </summary>
        Task LoadSceneGroupAsync(SceneGroup group, bool showLoadingScreen = false);
    }
}
