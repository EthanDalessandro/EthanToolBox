using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace EthanToolBox.UI.Scripts
{
    public class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }

        [Header("UI References")]
        public GameObject panel;
        public Slider progressBar;
        public TMP_Text progressText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            // Hide by default
            if (panel != null) panel.SetActive(false);
        }

        public void Show(AsyncOperation operation)
        {
            if (panel != null) panel.SetActive(true);
            StartCoroutine(UpdateProgress(operation));
        }

        private IEnumerator UpdateProgress(AsyncOperation operation)
        {
            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                
                if (progressBar != null) progressBar.value = progress;
                if (progressText != null) progressText.text = $"{(progress * 100):0}%";

                yield return null;
            }

            if (panel != null) panel.SetActive(false);
        }
    }
}
