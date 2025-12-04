namespace EthanToolBox.Core.SceneManagement
{
    public interface ISceneLoader
    {
        void LoadSceneGroup(SceneGroup group);
        void UnloadScene(string sceneName);
    }
}
