using OLS.Casy.Core.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace OLS.Casy.AppService
{
    /// <summary>
    /// Implementation of <see cref="IAppService"/> for the Casy application.
    /// Provides product information and initialized all implementations of <see cref="IService"/>
    /// </summary>
    [Export(typeof(IAppService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CasyAppService : IAppService, IPartImportsSatisfiedNotification
    {
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;
        private readonly IEnumerable<IService> _serviceList;

        private string _version;

        /// <summary>
        /// Importing constructor
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="serviceList">An instance of all implementations of <see cref="IService"/></param>
        [ImportingConstructor]
        public CasyAppService(ILogger logger, [ImportMany] IEnumerable<IService> serviceList, IEnvironmentService environmentService)
        {
            _logger = logger;
            _serviceList = serviceList;
            _environmentService = environmentService;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes all implementations <see cref="T:OLS.Casy.Core.Api.IService" /> at startup
        /// </summary>
        /// <param name="progress">Implementation of <see cref="T:System.IProgress`1" /> to display progress information on splash screen</param>
        public void Initialize(IProgress<string> progress)
        {
            if (null == _serviceList) return;
            foreach (var serviceObj in _serviceList)
            {
                if (null != serviceObj)
                {
                    CallInterfaceFunction(serviceObj.Prepare, progress, serviceObj.GetType());
                }
            }
        }

        /// <summary>
        /// Deinitializes all implementations <see cref="IService"/> at shutdown
        /// </summary>
        /// <param name="progress">>Implementation of <see cref="IProgress{T}"/> to display shutdown progress information</param>
        public void Deinitialize(IProgress<string> progress)
        {
            if (null == _serviceList) return;
            foreach (var serviceObj in _serviceList)
            {
                if (null != serviceObj)
                {
                    CallInterfaceFunction(serviceObj.Deinitialize, progress, serviceObj.GetType());
                }
            }
        }

        /// <summary>
        /// Property returning the name of the app service
        /// </summary>
        public string ProductName => "OLS CASY 2.5";

        /// <summary>
        /// Property returning the current version of the app service
        /// </summary>
        public string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    try
                    {
                        _version = File.ReadAllText("version");
                    }
                    catch
                    {
                    }
                }
                return _version;
            }
        }

        private void CallInterfaceFunction(InterfaceFunction function, IProgress<string> progress, Type objType)
        {
            Task.Factory.StartNew(() => function(progress))
                //.ContinueWith(
                    //t => _logger.Debug($"{function.Method.Name} method on service {objType} called.",
                      //                      () => CallInterfaceFunction(function, progress, objType)), TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.Error(LogCategory.General,
                            $"{function.Method.Name} method call on service {objType} is failed with an exception: {t.Exception.ToString()}",
                            () => CallInterfaceFunction(function, progress, objType));
                    }
                    else
                    {
                        _logger.Error(LogCategory.General,
                            $"{function.Method.Name} method call on service {objType} is failed with an exception",
                            () => CallInterfaceFunction(function, progress, objType));
                    }
                }, TaskContinuationOptions.NotOnRanToCompletion);
        }

        public void OnImportsSatisfied()
        {
            _environmentService.SetEnvironmentInfo("SoftwareVersion", Version);
        }

        private delegate void InterfaceFunction(IProgress<string> progress);
    }
}
