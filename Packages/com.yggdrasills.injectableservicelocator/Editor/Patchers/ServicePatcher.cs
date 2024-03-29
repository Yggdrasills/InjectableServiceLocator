using System;
using System.Linq;
using System.Reflection;

using InjectableServiceLocator.Editor.Utils;
using InjectableServiceLocator.Services;
using InjectableServiceLocator.Services.Attributes;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace InjectableServiceLocator.Editor.Patchers
{
    internal class ServicePatcher : IPatcher
    {
        private readonly ModuleDefinition _module;
        private readonly ICompiledAssembly _compiledAssembly;

        internal ServicePatcher(ModuleDefinition module, ICompiledAssembly compiledAssembly)
        {
            _module = module;
            _compiledAssembly = compiledAssembly;
        }

        void IPatcher.ApplyPatch()
        {
            var assemblyDefinition = Assembly.Load(_compiledAssembly.InMemoryAssembly.PeData);

            foreach (var type in _module.Types)
            {
                var assemblyType = assemblyDefinition.GetType(type.FullName);

                if (!HasServiceAttribute(type, assemblyType))
                    continue;

                ApplyPatch(type, _module, assemblyType);
            }
        }

        private bool HasServiceAttribute(TypeDefinition type, Type assemblyType)
        {
            if (!type.HasCustomAttributes)
                return false;

            var hasServiceAttribute = type.CustomAttributes
                .Any(attribute => attribute.AttributeType.Name.Contains(nameof(ServiceAttribute)));

            return hasServiceAttribute && assemblyType.BaseType != typeof(Attribute);
        }

        private void ApplyPatch(TypeDefinition type, ModuleDefinition moduleDefinition, Type assemblyType)
        {
            InsertInstruction("Awake", moduleDefinition, type.Methods, "Register", assemblyType,
                Instruction.Create(OpCodes.Ldarg_0));
            InsertInstruction("OnDestroy", moduleDefinition, type.Methods, "Unregister", assemblyType);
        }

        private void InsertInstruction(string methodName, ModuleDefinition moduleDefinition,
            Collection<MethodDefinition> methods, string serviceLocatorMethod,
            Type assemblyType, params Instruction[] additionalInstructions)
        {
            var getSingletonMethod = typeof(ServiceLocator).GetMethod("get_Current");
            var targetMethod = typeof(ServiceLocator).GetMethod(serviceLocatorMethod);

            var ilProcessor = Utilities.GetOrCreateMethodProcessor(methodName,
                    moduleDefinition.TypeSystem.Void, methods)
                .Body.GetILProcessor();

            var processorInstruction = ilProcessor.Body.Instructions[0]; // set to [1] to pass NOP instruction
            var genericMethod = targetMethod?.MakeGenericMethod(assemblyType);

            var callInstanceInstruction =
                Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(getSingletonMethod));
            var virtualInstruction =
                Instruction.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(genericMethod));

            ilProcessor.InsertBefore(processorInstruction, callInstanceInstruction);

            foreach (var instruction in additionalInstructions)
            {
                ilProcessor.InsertBefore(processorInstruction, instruction);
            }

            ilProcessor.InsertBefore(processorInstruction, virtualInstruction);
        }
    }
}