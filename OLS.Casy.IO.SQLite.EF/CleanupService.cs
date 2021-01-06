using System;
using System.Collections.Generic;
#if NET47
using System.ComponentModel.Composition;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using OLS.Casy.Base;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;

namespace OLS.Casy.IO.SQLite.EF
{
#if NET47
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]

    public class CleanupService : AbstractService, IPartImportsSatisfiedNotification, IDisposable
    {
#else
    public class CleanupService : AbstractService, IDisposable
    {
#endif

        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ILogger _logger;
        private Timer _cleanupTimer;

#if NET47
        [ImportingConstructor]
#endif
        public CleanupService(IConfigService configService, IDatabaseStorageService databaseStorageService,
            ILogger logger)
            : base(configService)
        {
            _databaseStorageService = databaseStorageService;
            _logger = logger;
        }

        [ConfigItem(90)]
        public long CleanupInterval
        {
            get; set;
        }

        public void OnImportsSatisfied()
        {
            ConfigService.InitializeByConfiguration(this);
        }

        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);
            var interval120Minutes = 2 * 60 * 60 * 1000;
            _cleanupTimer = new Timer(interval120Minutes);
            _cleanupTimer.Elapsed += OnCleanupTimerElapsed;
            _cleanupTimer.Enabled = true;

            _databaseStorageService.OnDatabaseReadyEvent += (sender, args) => OnCleanupTimerElapsed(null, null);
            if (_databaseStorageService.IsDatabaseReady)
            {
                OnCleanupTimerElapsed(null, null);
            }
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            _cleanupTimer.Enabled = false;
            _cleanupTimer.Dispose();

            base.Deinitialize(progress);
        }

        private void OnCleanupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var utcNowTicks = DateTimeOffset.UtcNow.UtcTicks;

            var timeToCheck = DateTimeOffset.MinValue;
            if (_databaseStorageService.GetSettings()
                .TryGetValue("LastCleanupCycle", out var lastCleanupCycleSetting))
            {
                timeToCheck = DateTimeOffsetExtensions.ParseAny(lastCleanupCycleSetting.Value);
                timeToCheck = timeToCheck.AddDays(1);
            }

            if (timeToCheck.UtcTicks < utcNowTicks)
            {
                _databaseStorageService.Cleanup(unchecked((int) CleanupInterval));
                _databaseStorageService.SaveSetting("LastCleanupCycle", DateTimeOffset.UtcNow.ToString());
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _cleanupTimer.Dispose();
            }

            _disposedValue = true;
        }

        ~CleanupService()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
