using OLS.Casy.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Implementation of <see cref="ICompositionFactory"/>.
    /// Uses MEF container to resolve dependencies.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ICompositionFactory))]
    public class CompositionFactory : ICompositionFactory
    {
        /// <summary>
        /// MEF importing costructor
        /// </summary>
        [ImportingConstructor]
        public CompositionFactory()
        {
        }

        /// <summary>
        /// Non-generic way to resolve and return an instance of the passed type in the IoC-container
        /// </summary>
        /// <param name="compositeType">Type of the composite</param>
        /// <returns>An instance of the passed type</returns>
        public object GetExport(Type compositeType)
        {
            return GlobalCompositionContainerFactory.GetExport(compositeType);
        }

        public object GetExportedValue(Type compositeType)
        {
            return GlobalCompositionContainerFactory.GetExportedValue(compositeType);
        }

        /// <summary>
        /// Resolves and returns an instance of the passed type in the IoC-container
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <returns>An instance of the passed type</returns>
        public Lazy<T> GetExport<T>()
        {
            return GlobalCompositionContainerFactory.GetExport<T>();
        }

        /// <summary>
        /// Resolves and returns an instance of the passed type with the passed contract name in the IoC-container
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="contractName">Contract name</param>
        /// <returns>An instance of the passed type with the passed contract name</returns>
        public Lazy<T> GetExport<T>(string contractName)
        {
            return GlobalCompositionContainerFactory.GetExport<T>(contractName);
        }

        public void ReleaseExport<T>(Lazy<T> exportToRelease)
        {
            GlobalCompositionContainerFactory.ReleaseExport(exportToRelease);
        }
    }
}
