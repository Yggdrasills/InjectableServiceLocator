using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

namespace InjectableServiceLocator.Services
{
    public class ServiceLocator : MonoBehaviour
    {
        public static ServiceLocator Current { get; private set; }

        private readonly Dictionary<string, object> _services = new Dictionary<string, object>();

        private void Awake()
        {
            Current = this;
        }

        public T Get<T>()
        {
            string key = typeof(T).Name;

            Assert.IsTrue(_services.ContainsKey(key), $"{key} not registered with {GetType().Name}");

            return (T)_services[key];
        }

        public void Register<T>(T service)
        {
            string key = typeof(T).Name;

            Assert.IsFalse(_services.ContainsKey(key), $"Attempted to register service of type " +
                    $"{key} which is already registered with the {GetType().Name}.");

            _services.Add(key, service);

            Debug.Log("REGISTERED: " + key);
        }

        public void Unregister<T>()
        {
            string key = typeof(T).Name;

            Assert.IsTrue(_services.ContainsKey(key), $"Attempted to unregister service of type " +
                    $"{key} which is not registered with the {GetType().Name}.");

            _services.Remove(key);
        }
    }
}