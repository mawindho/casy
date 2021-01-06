using System;

namespace OLS.Casy.Core.Api
{
    /// <summary>
    /// Interface for a service to perform dependency injection
    /// </summary>
    public interface ICompositionFactory
    {
        /// <summary>
        /// Resolves and returns an instance of the passed type in the IoC-container
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <returns>An instance of the passed type</returns>
        Lazy<T> GetExport<T>();

        /// <summary>
        /// Resolves and returns an instance of the passed type with the passed contract name in the IoC-container
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="contractName">Contract name</param>
        /// <returns>An instance of the passed type with the passed contract name</returns>
        Lazy<T> GetExport<T>(string contractName);

        /// <summary>
        /// Non-generic way to resolve and return an instance of the passed type in the IoC-container
        /// </summary>
        /// <param name="compositeType">Type of the composite</param>
        /// <returns>An instance of the passed type</returns>
        object GetExport(Type compositeType);

        object GetExportedValue(Type compositeType);

        void ReleaseExport<T>(Lazy<T> exportToRelease);
    }
}
