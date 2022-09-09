using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Pdb;

using UnityEditor;
using UnityEditor.Compilation;

#if SL_LOG_ENABLED
using UnityEngine;
#endif

using Assembly = System.Reflection.Assembly;

namespace InjectableServiceLocator.Editor.Patchers
{
    [UsedImplicitly]
    public class AssemblyPatcher
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies();

            // todo: should be replaced by assemblies list
            var csharpAssembly = assembly.FirstOrDefault(a => a.ManifestModule.Name.Equals("Assembly-CSharp.dll"));

            if (csharpAssembly == null)
                return;

            Patch(csharpAssembly);
        }

        private static void Patch(Assembly assembly)
        {
            var patchers = new List<IPatcher> {new ServicePatcher(), new InjectPatcher()};

            var readParameters = new ReaderParameters()
            {
                InMemory = true,
                ReadWrite = true,
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider()
            };

            using var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly.Location, readParameters);

            var moduleDefinition = assemblyDefinition.MainModule;

            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.ApplyPatch(moduleDefinition, assembly);
                }

                assemblyDefinition.Write(assembly.Location, new WriterParameters()
                {
                    WriteSymbols = true
                });
#if SL_LOG_ENABLED
                Debug.Log("[Patch applied]");
#endif
            }
            catch (Exception e)
            {
#if SL_LOG_ENABLED
                Debug.LogError("[PATCH FAILED] Stack trace: " + e.StackTrace);
#endif
            }
        }
    }
}