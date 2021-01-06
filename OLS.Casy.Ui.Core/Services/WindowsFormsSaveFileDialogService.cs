using OLS.Casy.Core.Config.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ISaveFileDialogService))]
    public class WindowsFormsSaveFileDialogService : ISaveFileDialogService, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IConfigService _configService;

        [ImportingConstructor]
        public WindowsFormsSaveFileDialogService(IConfigService configService)
        {
            this._configService = configService;
        }

        public string Filter { get; set; }
        public string Title { get; set; }
        public string InitialDirectory { get; set; }
        public string FileName { get; set; }

        [ConfigItem("")]
        public string DefaultBrowseLocation { get; set; }

        public void OnImportsSatisfied()
        {
            this._configService.InitializeByConfiguration(this);
            this._configService.ConfigurationChangedEvent += OnConfigurationChanges;
            OnConfigurationChanges(null, null);
        }

        private void OnConfigurationChanges(object sender, ConfigurationChangedEventArgs e)
        {
            InitialDirectory = string.IsNullOrEmpty(DefaultBrowseLocation) ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) : DefaultBrowseLocation;
        }

        public bool? ShowDialog()
        {
            using (var dialog = CreateDialog())
            {
                var result = dialog.ShowDialog() == DialogResult.OK;
                if (result)
                {
                    FileName = dialog.FileName;
                }
                return result;
            }
        }

        public bool? ShowDialog(Window owner)
        {
            using (var dialog = CreateDialog())
            {
                var result = dialog.ShowDialog(owner.AsWin32Window()) == DialogResult.OK;
                if (result)
                {
                    FileName = dialog.FileName;
                }
                return result;
            }
        }

        private SaveFileDialog CreateDialog()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = this.Title;
            dialog.InitialDirectory = this.InitialDirectory;
            dialog.FileName = this.FileName;
            dialog.Filter = this.Filter;
            return dialog;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_configService != null)
                    {
                        this._configService.ConfigurationChangedEvent -= OnConfigurationChanges;
                        this._configService.ReleaseConfiguration(this);
                    }
                    //if(_minTimer != null)
                    //{
                    //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
        }

        ~WindowsFormsSaveFileDialogService()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
