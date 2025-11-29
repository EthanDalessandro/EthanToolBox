using EthanToolBox.Core.DependencyInjection;
using UnityEngine;

namespace EthanToolBox.Demo
{
    public class DemoConsumer : MonoBehaviour
    {
        [Inject]
        private IDemoService _demoService;

        private void Start()
        {
            if (_demoService != null)
            {
                _demoService.SayHello();
            }
            else
            {
                Debug.LogError("DemoService was NOT injected!");
            }
        }
    }
}
