﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

using Mono.Cecil;

using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace InjectableServiceLocator.Editor.PostProcessing
{
    internal sealed class PostProcessorAssemblyResolver : IAssemblyResolver
    {
        private readonly string[] _references;
        private readonly IDictionary<string, AssemblyDefinition> _cache = new Dictionary<string, AssemblyDefinition>();

        private readonly ICompiledAssembly _compiledAssembly;
        private AssemblyDefinition _selfAssembly;

        public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
        {
            _compiledAssembly = compiledAssembly;
            _references = compiledAssembly.References;
        }

        public void Dispose()
        {
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            lock (_cache)
            {
                if (name.Name == _compiledAssembly.Name)
                    return _selfAssembly;

                var fileName = FindFile(name);
                if (fileName == null)
                    return null;

                var lastWriteTime = File.GetLastWriteTime(fileName);

                var cacheKey = fileName + lastWriteTime.ToString(CultureInfo.InvariantCulture);

                if (_cache.TryGetValue(cacheKey, out var result))
                    return result;

                parameters.AssemblyResolver = this;

                var ms = MemoryStreamFor(fileName);

                var pdb = fileName + ".pdb";
                if (File.Exists(pdb))
                    parameters.SymbolStream = MemoryStreamFor(pdb);

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
                _cache.Add(cacheKey, assemblyDefinition);
                return assemblyDefinition;
            }
        }

        private string FindFile(AssemblyNameReference name)
        {
            var fileName = _references.FirstOrDefault(r => Path.GetFileName(r) == name.Name + ".dll");
            if (fileName != null)
                return fileName;

            // perhaps the type comes from an exe instead
            fileName = _references.FirstOrDefault(r => Path.GetFileName(r) == name.Name + ".exe");
            if (fileName != null)
                return fileName;

            //Unfortunately the current ICompiledAssembly API only provides direct references.
            //It is very much possible that a postprocessor ends up investigating a type in a directly
            //referenced assembly, that contains a field that is not in a directly referenced assembly.
            //if we don't do anything special for that situation, it will fail to resolve.  We should fix this
            //in the ILPostProcessing api. As a workaround, we rely on the fact here that the indirect references
            //are always located next to direct references, so we search in all directories of direct references we
            //got passed, and if we find the file in there, we resolve to it.
            foreach (var parentDir in _references.Select(Path.GetDirectoryName).Distinct())
            {
                var candidate = Path.Combine(parentDir, name.Name + ".dll");
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static MemoryStream MemoryStreamFor(string fileName)
        {
            return Retry(10, TimeSpan.FromSeconds(1), () => {
                byte[] byteArray;
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byteArray = new byte[fs.Length];
                    var readLength = fs.Read(byteArray, 0, (int)fs.Length);
                    if (readLength != fs.Length)
                        throw new InvalidOperationException("File read length is not full length of file.");
                }

                return new MemoryStream(byteArray);
            });
        }

        private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
        {
            try
            {
                return func();
            }
            catch (IOException)
            {
                if (retryCount == 0)
                    throw;
                Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
                Thread.Sleep(waitTime);
                return Retry(retryCount - 1, waitTime, func);
            }
        }

        public void AddAssemblyDefinitionBeingOperatedOn(AssemblyDefinition assemblyDefinition)
        {
            _selfAssembly = assemblyDefinition;
        }
    }
}