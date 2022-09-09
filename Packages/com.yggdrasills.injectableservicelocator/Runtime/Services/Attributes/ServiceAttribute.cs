using System;

namespace InjectableServiceLocator.Services.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public Type Type { get; }

        public ServiceAttribute(Type type)
        {
            Type = type;
        }
    }
}