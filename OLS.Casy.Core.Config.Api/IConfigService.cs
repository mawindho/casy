using OLS.Casy.Core.Api;
using System;
using System.Collections.Generic;

namespace OLS.Casy.Core.Config.Api
{
    /// <summary>
    /// Interface for a service providing functionality to store and load configuration entries.
    /// Configuration entries can be defined as properties in classes.
    /// </summary>
    public interface IConfigService : ICoreService
    {
        /// <summary>
        /// Stores the configuration entries of the passed object.
        /// </summary>
        /// <typeparam name="T">Type of the object containing configuration properties</typeparam>
        /// <param name="instanceWithConfigItemAttributes">Object containing configuration properties</param>
        void StoreToConfiguration<T>(T instanceWithConfigItemAttributes);

        /// <summary>
        /// Stores the configuration entries of the passed list of objects.
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instancesWithConfigItemAttributes">List of objects containing configuration properties</param>
        void StoreToConfiguration<T>(IEnumerable<T> instancesWithConfigItemAttributes);

        /// <summary>
        /// Initializes the configuration properties of the passed object with values from currently loaded configuration
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instanceWithConfigItemAttributes">Object containing configuration properties</param>
        void InitializeByConfiguration<T>(T instanceWithConfigItemAttributes);

        void ReleaseConfiguration<T>(T initializedByConfiguration);

        /// <summary>
        /// Initializes the configuration properties of the passed list of object with values from currently loaded configuration
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instancesWithConfigItemAttributes">List of object containing configuration properties</param>
        void InitializeByConfiguration<T>(IEnumerable<T> instancesWithConfigItemAttributes);

        /// <summary>
        /// Updates the currently loaded configuration with values of passed config item models
        /// </summary>
        /// <param name="configItems"></param>
        void UpdateConfiguration(IEnumerable<ConfigItemModel> configItems);

        /// <summary>
        /// Event is raised when currently loaded configuration has been changed
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChangedEvent;

        /// <summary>
        /// Method to try to get the configuration entry with passed config item name
        /// </summary>
        /// <typeparam name="TValue">Type of the searched configuration entry</typeparam>
        /// <param name="configItemName">Name of the config item the value is requested for</param>
        /// <returns>Configuration value</returns>
        TValue GetConfigItemValue<TValue>(string configItemName);

        //bool IsCounterVersion { get; }
    }
}
