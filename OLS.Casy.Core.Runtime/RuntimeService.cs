using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Core.Runtime.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models.Enums;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Runtime
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IRuntimeService))]
    public class RuntimeService : IRuntimeService
    {
        private readonly ILogger _logger;
        private readonly IAppService _appService;
        private readonly IConfigService _configService;
        private readonly IDatabaseStorageService _databaseStorageService;

        [ImportingConstructor]
        protected RuntimeService(ILogger logger, IConfigService configService, IAppService appService,
            IDatabaseStorageService databaseStorageService)
        {
            this._logger = logger;
            this._configService = configService;
            this._appService = appService;
            this._databaseStorageService = databaseStorageService;
        }

        public void Initialize(IProgress<string> progress)
        {
            Task.Factory.StartNew(() =>
            {
                _logger.Info(LogCategory.General, string.Format("*** Application starting: Name {0}, Version {1} ***", _appService.ProductName, _appService.Version));

                // the order of Initialize is well defined and must not changed!
                _configService.Initialize(progress);
                _databaseStorageService.Initialize(progress);
                while (!_databaseStorageService.IsDatabaseReady)
                {
                }
                _appService.Initialize(progress);
            });
        }

        public void Deinitialize(IProgress<string> progress)
        {
            _logger.Info(LogCategory.General, "*** Application shut down ***");

            // the order of Deinitialize is well defined and must not changed!
            _appService.Deinitialize(progress);
            _databaseStorageService.Deinitialize(progress);
            _configService.Deinitialize(progress);
        }
    }
}
