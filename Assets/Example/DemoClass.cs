using InjectableServiceLocator.Services.Attributes;

using UnityEngine;

namespace InjectableServiceLocator.Demo
{
    public class DemoClass : MonoBehaviour
    {
        [Inject]
        private DemoService _service;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _service.Display();
            }
        }
    }
}