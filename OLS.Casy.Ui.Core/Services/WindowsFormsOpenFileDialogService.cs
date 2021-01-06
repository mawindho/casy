using OLS.Casy.Core.Config.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IOpenFileDialogService))]
    public class WindowsFormsOpenFileDialogService : IOpenFileDialogService, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IConfigService _configService;

        [ImportingConstructor]
        public WindowsFormsOpenFileDialogService(IConfigService configService)
        {
            this._configService = configService;
            Multiselect = false;
        }

        public string Filter { get; set; }
        public string Title { get; set; }
        public bool Multiselect { get; set; }
        public string InitialDirectory { get; set; }
        public string[] FileNames { get; set; }

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
                    if (!Multiselect)
                    {
                        FileNames = new[] { dialog.FileName };
                    }
                    else
                    {
                        FileNames = dialog.FileNames;
                    }
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
                    if (!Multiselect)
                    {
                        FileNames = new[] { dialog.FileName };
                    }
                    else
                    {
                        FileNames = dialog.FileNames;
                    }
                }
                return result;
            }
        }

        private OpenFileDialog CreateDialog()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = this.Title;
            dialog.Multiselect = this.Multiselect;
            dialog.InitialDirectory = this.InitialDirectory;
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
                    if(_configService != null)
                    {
                        this._configService.ConfigurationChangedEvent -= OnConfigurationChanges;
                    }
                    //if(_minTimer != null)
                    //{
                    //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
        }

        ~WindowsFormsOpenFileDialogService()
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
