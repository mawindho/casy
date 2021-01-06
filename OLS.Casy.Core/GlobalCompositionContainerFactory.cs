using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Static container class holding the MEF IoC-container
    /// </summary>
    public static class GlobalCompositionContainerFactory
    {
        private static CompositionContainer _compositionContainer;
        private static List<DirectoryCatalog> _directoryCatalogs;

        /// <summary>
        /// Property for the MEF IoC-container
        /// </summary>
        private static CompositionContainer CompositionContainer
        {
            get { return _compositionContainer; }
        }

        /// <summary>
        /// Assembley all components at startup.
        /// </summary>
        /// <param name="directoryCatalogPaths">Path, where the assemblies for the catalouge located at</param>
        /// <param name="excludedFiles">List of files to exclude from the container</param>
        public static void AssembleComponents(List<string> directoryCatalogPaths = null, IList<string> excludedFiles = null)
        {
            //Step 1: Load assemblies
            List<string> assemblyList = new List<string>(Directory.GetFiles(@".", "OLS.Casy.*.exe"));
            assemblyList.AddRange(Directory.GetFiles(@".", "OLS.Casy.*.dll"));

            //var net47Assemblies = Directory.GetFiles(@".\net47", "OLS.Casy.*.dll");
            //foreach(var assembly in net47Assemblies)
            //{
                //if(!assemblyList.Any(x => x.EndsWith(assembly.Substring(assembly.LastIndexOf(@"\")))))
                //{
                    //assemblyList.Add(assembly);
                //}
            //}

            //assemblyList.AddRange(Directory.GetFiles(@".\netstandard2.0", "OLS.Casy.*.dll"));

            if (excludedFiles != null && excludedFiles.Any())
            {
                //Step 2: Filter assemblies
                assemblyList = assemblyList.FindAll(assembly => !excludedFiles.Any(assembly.Contains));
            }

            //Step 3: Create components
            List<ComposablePartCatalog> catalogs = assemblyList.Select(assembly => new AssemblyCatalog(assembly)).Cast<ComposablePartCatalog>().ToList();

            var catalog = new AggregateCatalog(catalogs);

            if (directoryCatalogPaths != null)
            {
                _directoryCatalogs = new List<DirectoryCatalog>();
                foreach (var path in directoryCatalogPaths)
                {
                    var directoryCatalog = new DirectoryCatalog(path);
                    _directoryCatalogs.Add(directoryCatalog);
                    catalog.Catalogs.Add(directoryCatalog);
                }
            }

            //Step 3: The assemblies obtained in step 1 are added to the CompositionContainer
            _compositionContainer = new CompositionContainer(catalog);

            _compositionContainer.ComposeExportedValue(_compositionContainer);
        }

        /// <summary>
        /// Returns an type with the aid of reflections when the geenric way is not possible at runtime.
        /// </summary>
        /// <param name="type">Type of the value</param>
        /// <returns>Instance of the passed type in the IoC-container</returns>
        public static object GetExport(Type type)
        {
            MethodInfo methodInfo = CompositionContainer.GetType().GetMethods().Where(d => d.Name == "GetExport" && d.GetParameters().Length == 0 && d.GetGenericArguments().Length == 1).First();
            Type[] genericTypeArray = new Type[] { type };
            methodInfo = methodInfo.MakeGenericMethod(genericTypeArray);
            return methodInfo.Invoke(CompositionContainer, null);
        }

        public static object GetExportedValue(Type type)
        {
            MethodInfo methodInfo = CompositionContainer.GetType().GetMethods().Where(d => d.Name == "GetExportedValue" && d.GetParameters().Length == 0).First();
            Type[] genericTypeArray = new Type[] { type };
            methodInfo = methodInfo.MakeGenericMethod(genericTypeArray);
            return methodInfo.Invoke(CompositionContainer, null);
        }

        public static Lazy<T> GetExport<T>()
        {
            return CompositionContainer.GetExport<T>();
        }

        public static IEnumerable<Lazy<T>> GetExports<T>()
        {
            return CompositionContainer.GetExports<T>();
        }

        public static Lazy<T> GetExport<T>(string contractName)
        {
            return CompositionContainer.GetExport<T>(contractName);
        }

        public static void ReleaseExport<T>(Lazy<T> export)
        {
            CompositionContainer.ReleaseExport(export);
        }

        public static void ReleaseExport(Export export)
        {
            CompositionContainer.ReleaseExport(export);
        }

        public static void Dispose()
        {
            CompositionContainer?.Dispose();
        }
    }
}
