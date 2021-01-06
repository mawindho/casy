using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Timers;

namespace OLS.Casy.IO.Backup
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    [Export(typeof(IBackupService))]
    public class BackupService : AbstractService, IBackupService, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ILogger _logger;
        private Timer _backupTimer;

        [ImportingConstructor]
        public BackupService(IConfigService configService, IDatabaseStorageService databaseStorageService,
            ILogger logger)
            : base(configService)
        {
            _databaseStorageService = databaseStorageService;
            _logger = logger;
        }

        [ConfigItem(false)]
        public bool IsBackupEnabled
        {
            get; set;
        }

        [ConfigItem("")]
        public string BackupPath
        {
            get; set;
        }

        [ConfigItem(0)]
        public long BackupInterval
        {
            get; set;
        }

        [ConfigItem(0)]
        public long LastBackupTime
        {
            get; set;
        }

        public bool RestoreBackup(string backupPath)
        {
            if(File.Exists(backupPath))
            {
                return _databaseStorageService.RestoreBackupFile(backupPath);
            }
            return false;
        }

        public void OnImportsSatisfied()
        {
            this.ConfigService.InitializeByConfiguration(this);
        }

        private void OnConfigChanged(object sender, ConfigurationChangedEventArgs e)
        {
            if(e.ChangedItemNames.Contains("IsBackupEnabled"))
            {
                if(this.IsBackupEnabled)
                {
                    this.LastBackupTime = 0;
                    OnBackupTimerElapsed(null, null);
                }
            }
        }

        private void OnBackupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if(this.IsBackupEnabled)
            {
                var utcNowTicks = DateTime.UtcNow.Ticks;

                DateTime toCheckTime = new DateTime(this.LastBackupTime);
                switch((BackupInterval)this.BackupInterval)
                {
                    case Core.Config.Api.BackupInterval.Daily:
                        toCheckTime = toCheckTime.AddDays(1);
                        break;
                    case Core.Config.Api.BackupInterval.Monthly:
                        toCheckTime = toCheckTime.AddMonths(1);
                        break;
                    case Core.Config.Api.BackupInterval.Weekly:
                        toCheckTime = toCheckTime.AddDays(7);
                        break;
                }

                if(toCheckTime.Ticks < utcNowTicks)
                {
                    LastBackupTime = utcNowTicks;

                    ConfigItemModel model = new ConfigItemModel()
                    {
                        Name = "LastBackupTime",
                        Value = this.LastBackupTime
                    };
                    ConfigService.UpdateConfiguration(new[] { model });

                    if(!Directory.Exists(this.BackupPath))
                    {
                        Directory.CreateDirectory(this.BackupPath);
                    }

                    var destFilePath = Path.Combine(this.BackupPath, "casy.db.backup" + utcNowTicks.ToString());
                    _databaseStorageService.CreateBackupFile(destFilePath);

                    //var destLogFilePath = Path.Combine(this.BackupPath, "log.db.backup" + utcNowTicks.ToString());
                    //_logger.CreateBackupFile(destLogFilePath);
                }
            }
        }

        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);
            var interval60Minutes = 60 * 60 * 1000;
            _backupTimer = new Timer(interval60Minutes);
            _backupTimer.Elapsed += new ElapsedEventHandler(OnBackupTimerElapsed);
            _backupTimer.Enabled = true;

            OnBackupTimerElapsed(null, null);

            this.ConfigService.ConfigurationChangedEvent += OnConfigChanged;
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            _backupTimer.Enabled = false;
            _backupTimer.Dispose();

            base.Deinitialize(progress);

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._backupTimer.Dispose();
                }

                disposedValue = true;
            }
        }

         ~BackupService() {
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
