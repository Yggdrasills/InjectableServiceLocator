using InjectableServiceLocator.Services.Attributes;

using UnityEngine;

namespace InjectableServiceLocator.Demo
{
	[Service(typeof(DemoService))]
	public class DemoService : MonoBehaviour
	{
		public void Display()
        {
			Debug.Log("Demo service");
        }
    }
}