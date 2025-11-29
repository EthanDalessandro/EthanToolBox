using UnityEngine;

namespace EthanToolBox.Demo
{
    public class DemoService : IDemoService
    {
        public void SayHello()
        {
            Debug.Log("Hello from DemoService! Dependency Injection is working!");
        }
    }
}
