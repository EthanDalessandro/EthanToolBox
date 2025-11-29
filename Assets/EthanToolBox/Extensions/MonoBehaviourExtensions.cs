using System;
using System.Collections;
using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Executes an action after a specified delay (in seconds).
        /// Usage: this.Delay(2f, () => Debug.Log("Done"));
        /// </summary>
        public static void Delay(this MonoBehaviour mono, float delay, Action action)
        {
            if (mono.gameObject.activeInHierarchy)
            {
                mono.StartCoroutine(DelayCoroutine(delay, action));
            }
        }

        private static IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
