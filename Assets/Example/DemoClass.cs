using InjectableServiceLocator.Services.Attributes;

using UnityEngine;
using UnityEngine.UI;

namespace InjectableServiceLocator.Demo
{
    public class DemoClass : MonoBehaviour
    {
        [Inject]
        private DemoService _service;

        [SerializeField]
        private Button _tapButton;

        private int _tapCount;

        private void Start()
        {
            _tapButton.onClick.AddListener(Display);
        }

        private void Display()
        {
            _tapCount++;

            _tapButton.GetComponentInChildren<Text>().text += _tapCount + " ";
        }
    }
}