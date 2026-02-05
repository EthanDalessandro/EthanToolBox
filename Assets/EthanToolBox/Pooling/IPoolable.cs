namespace EthanToolBox.Core.Pooling
{
    /// <summary>
    /// Implement this interface on MonoBehaviours to receive callbacks when pooled.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is taken from the pool (on Spawn).
        /// Use this to reset state, e.g., health = maxHealth;
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when the object is returned to the pool (on Release).
        /// Use this to cleanup, e.g., stop coroutines, clear references.
        /// </summary>
        void OnRelease();
    }
}
