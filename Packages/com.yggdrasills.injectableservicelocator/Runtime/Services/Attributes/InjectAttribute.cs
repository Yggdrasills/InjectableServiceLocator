using System;

namespace InjectableServiceLocator.Services.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class InjectAttribute : Attribute { }
}