using System.Reflection;

using Mono.Cecil;

namespace InjectableServiceLocator.Editor.Patchers
{
    public interface IPatcher
    {
        void ApplyPatch(ModuleDefinition moduleDefinition, Assembly assemblyDefinition);
    }
}