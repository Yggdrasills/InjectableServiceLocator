#if UNITY_EDITOR
using JetBrains.Annotations;

using UnityEditor;
using UnityEditor.Compilation;

namespace InjectableServiceLocator.Editor.Utils
{
    [UsedImplicitly]
    public class ForceRecompileUtility
    {
        [MenuItem("Tools/CompilationPipeline/Compile", false, 100)]
        public static void Refresh()
        {
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
#endif