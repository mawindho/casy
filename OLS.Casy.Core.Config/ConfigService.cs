using Newtonsoft.Json;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Config
{
    /// <summary>
    /// Implementation of <see cref="IConfigService"/>.
    /// Stores configuration settings in configuration file on local fie system.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IConfigService))]
    public class ConfigService : IConfigService
    {
        //private const bool COUNTERVERSION = false;

        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;

        private readonly IList<object> _configuredObjects;
        private readonly Dictionary<string, ConfigItemModel> _configItems;

        private object _lock = new object();

        //public bool IsCounterVersion
        //{
        //    get
        //    {
        //        return COUNTERVERSION;
        //    }
        //}

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="fileSystemStorageService">Implementation of <see cref="IFileSystemStorageService"/> </param>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        [ImportingConstructor]
        public ConfigService(IFileSystemStorageService fileSystemStorageService, ILogger logger, ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            this._fileSystemStorageService = fileSystemStorageService;
            this._logger = logger;

            this._configItems = new Dictionary<string, ConfigItemModel>();
            this._configuredObjects = new List<object>();
        }

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     The Initialize phase is for preparations.
        /// </summary>
        public void Initialize(IProgress<string> progress)
        {
            progress.Report(_localizationService.GetLocalizedString("SplashScreen_Message_LoadingConfiguration"));
            this.LoadConfiguration();
        }

        /// <summary>
        ///     Pre-condition: Operational phase is done.
        ///     The Deinitialize phase is for cleaning up, storing and closing resouces.
        /// </summary>
        public void Deinitialize(IProgress<string> progress)
        {
            this.SaveConfiguration();
        }

        /// <summary>
        /// Stores the configuration entries of the passed object.
        /// </summary>
        /// <typeparam name="T">Type of the object containing configuration properties</typeparam>
        /// <param name="instanceWithConfigItemAttributes">Object containing configuration properties</param>
        public void StoreToConfiguration<T>(T instanceWithConfigItemAttributes)
        {
            this.StoreToConfiguration<T>(new[] { instanceWithConfigItemAttributes });
        }

        /// <summary>
        /// Stores the configuration entries of the passed list of objects.
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instancesWithConfigItemAttributes">List of objects containing configuration properties</param>
        public void StoreToConfiguration<T>(IEnumerable<T> instancesWithConfigItemAttributes)
        {
            var withConfigItemAttributes = instancesWithConfigItemAttributes as T[] ?? instancesWithConfigItemAttributes.ToArray();

            if (withConfigItemAttributes.Length > 0)
            {
                foreach (var configItem in withConfigItemAttributes)
                {
                    if (!configItem.ContainsConfigItemAttributes())
                    {
                        //throw new ArgumentException("Config item stored without config attributes: " + configItem.ToString());
                    }

                    lock (_lock)
                    {
                        if (!_configuredObjects.Contains(configItem))
                        {
                            _configuredObjects.Add(configItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the configuration properties of the passed object with values from currently loaded configuration
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instanceWithConfigItemAttributes">Object containing configuration properties</param>
        public void InitializeByConfiguration<T>(T InitializeByConfiguration)
        {
            this.InitializeByConfiguration<T>(new[] { InitializeByConfiguration });
        }

        public void ReleaseConfiguration<T>(T initializedByConfiguration)
        {
            if (initializedByConfiguration.ContainsConfigItemAttributes())
            {
                _configuredObjects.Remove(initializedByConfiguration);
            }
        }

        /// <summary>
        /// Initializes the configuration properties of the passed list of object with values from currently loaded configuration
        /// </summary>
        /// <typeparam name="T">Type of the objects containing configuration properties</typeparam>
        /// <param name="instancesWithConfigItemAttributes">List of object containing configuration properties</param>
        public void InitializeByConfiguration<T>(IEnumerable<T> instancesWithConfigItemAttributes)
        {
            var instanceWithConfigItemAttributes = instancesWithConfigItemAttributes as T[] ?? instancesWithConfigItemAttributes.ToArray();

            int numInstances = instanceWithConfigItemAttributes.Length;
            if (numInstances > 0)
            {
                lock (_lock)
                {
                    for (int counter = 0; counter < numInstances; counter++)
                    {
                        var instance = instanceWithConfigItemAttributes[counter];

                        if (instance.ContainsConfigItemAttributes())
                        {
                            PropertyInfo[] props = instance.GetType().GetProperties();
                            foreach (PropertyInfo prop in props)
                            {
                                object[] attrs = prop.GetCustomAttributes(true);
                                foreach (object attr in attrs)
                                {
                                    var configItemAttr = attr as ConfigItemAttribute;
                                    if (configItemAttr != null)
                                    {
                                        ConfigItemModel configItem;
                                        _configItems.TryGetValue(prop.Name, out configItem);

                                        if (configItem == null)
                                        {
                                            configItem = new ConfigItemModel();
                                            configItem.Name = prop.Name;
                                            configItem.Value = configItemAttr.DefaultValue;
                                            _configItems.Add(configItem.Name, configItem);
                                        }
                                        prop.SetValue(instance, configItem.Value);
                                    }
                                }
                            }

                            if (!_configuredObjects.Contains(instance))
                            {
                                _configuredObjects.Add(instance);
                            }
                        }
                    }
                }
            }
        }

        private void SaveConfiguration()
        {
                var configFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                configFilePath = Path.Combine(configFilePath, "settings.casy");

                var json = JsonConvert.SerializeObject(_configItems.Values.ToArray(), Formatting.Indented);
                Task.Run(async () =>
                {
                    try
                    {
                        await _fileSystemStorageService.CreateFileAsync(configFilePath, Encoding.UTF8.GetBytes(json));
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                });
        }

        private void LoadConfiguration()
        {
            var configFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            configFilePath = Path.Combine(configFilePath, "settings.casy");

            IEnumerable<ConfigItemModel> newConfigItems = null;

            if (!string.IsNullOrEmpty(configFilePath))
            {
                if (!File.Exists(configFilePath))
                {
                    _fileSystemStorageService.CreateFileAsync(configFilePath, new byte[0]);
                }

                var task = Task.Run(async () => await _fileSystemStorageService.ReadFileAsync(configFilePath));
                var fileContent = task.Result;
                var contentString = Encoding.UTF8.GetString(fileContent);

                newConfigItems = JsonConvert.DeserializeObject<ConfigItemModel[]>(contentString);
            }

            UpdateConfiguration(newConfigItems);
        }

        /// <summary>
        /// Event is raised when currently loaded configuration has been changed
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChangedEvent;

        private void RaiseConfigurationChangedEvent(IEnumerable<string> changedItemNames)
        {
            if (ConfigurationChangedEvent != null)
            {
                var args = new ConfigurationChangedEventArgs(changedItemNames);
                foreach (EventHandler<ConfigurationChangedEventArgs> receiver in ConfigurationChangedEvent.GetInvocationList())
                {
                    receiver.BeginInvoke(this, args, null, null);
                }

            }
        }

        /// <summary>
        /// Updates the currently loaded configuration with values of passed config item models
        /// </summary>
        /// <param name="configItems"></param>
        public void UpdateConfiguration(IEnumerable<ConfigItemModel> configItems)
        {
            List<string> changedItemNames = new List<string>();

            if (configItems != null)
            {
                foreach (var item in configItems)
                {
                    ConfigItemModel configItem;
                    if (!this._configItems.TryGetValue(item.Name, out configItem))
                    {
                        configItem = new ConfigItemModel() { Name = item.Name };
                        this._configItems.Add(configItem.Name, configItem);
                    }

                    if (configItem.Value != item.Value)
                    {
                        configItem.Value = item.Value;
                        changedItemNames.Add(configItem.Name);
                    }
                }
            }

            foreach (var configuredObject in _configuredObjects)
            {
                this.InitializeByConfiguration(configuredObject);
            }

            RaiseConfigurationChangedEvent(changedItemNames);

            SaveConfiguration();
        }

        /// <summary>
        /// Method to try to get the configuration entry with passed config item name
        /// </summary>
        /// <typeparam name="TValue">Type of the searched configuration entry</typeparam>
        /// <param name="configItemName">Name of the config item the value is requested for</param>
        /// <returns>Configuration value</returns>
        public TValue GetConfigItemValue<TValue>(string configItemName)
        {
            ConfigItemModel configItemModel;
            if (this._configItems.TryGetValue(configItemName, out configItemModel))
            {
                return (TValue)Convert.ChangeType(configItemModel.Value, typeof(TValue), CultureInfo.InvariantCulture);
            }
            return default(TValue);
        }
    }
}
