using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EthanToolBox.Core.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        public event Action<float> OnProgress;

        public async Task LoadSceneAsync(string sceneName, bool showLoadingScreen = false)
        {
            if (showLoadingScreen)
            {
                // Placeholder for loading screen logic
                Debug.Log($"[SceneLoader] Starting load of {sceneName} with loading screen...");
            }

            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                OnProgress?.Invoke(operation.progress);
                await Task.Yield();
            }

            Debug.Log($"[SceneLoader] Loaded {sceneName}");
        }

        public async Task LoadSceneAdditiveAsync(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        public async Task UnloadSceneAsync(string sceneName)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation != null)
            {
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
            }
        }

        public async Task LoadSceneGroupAsync(SceneGroup group, bool showLoadingScreen = false)
        {
            if (group.Scenes.Count == 0)
            {
                Debug.LogWarning("[SceneLoader] SceneGroup is empty!");
                return;
            }

            // 1. Load the first scene (Active Scene) normally to wipe previous state
            // Or we could UnloadAll manually, but LoadSceneSingle is safer to clear memory.
            // We assume the first scene in the list or the ActiveSceneName is the 'base' scene.
            
            string baseScene = !string.IsNullOrEmpty(group.ActiveSceneName) ? group.ActiveSceneName : group.Scenes[0];

            await LoadSceneAsync(baseScene, showLoadingScreen);

            // 2. Load the rest additively
            foreach (var sceneName in group.Scenes)
            {
                if (sceneName == baseScene) continue;
                await LoadSceneAdditiveAsync(sceneName);
            }

            // 3. Set Active Scene
            Scene activeScene = SceneManager.GetSceneByName(baseScene);
            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
        }
    }
}
