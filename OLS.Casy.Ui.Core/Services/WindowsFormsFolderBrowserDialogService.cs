using OLS.Casy.Core.Config.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IFolderBrowserDialogService))]
    public class WindowsFormsFolderBrowserDialogService : IFolderBrowserDialogService, IPartImportsSatisfiedNotification
    {
        private readonly IConfigService _configService;

        private string _description;
        private string _selectedPath;

        [ImportingConstructor]
        public WindowsFormsFolderBrowserDialogService(IConfigService configService)
        {
            this._configService = configService;

            RootFolder = System.Environment.SpecialFolder.MyComputer;
            ShowNewFolderButton = false;
        }

        [ConfigItem("")]
        public string DefaultBrowseLocation { get; set; }

        public string Description
        {
            get { return _description ?? string.Empty; }
            set { _description = value; }
        }

        public System.Environment.SpecialFolder RootFolder { get; set; }

        public string SelectedPath
        {
            get { return _selectedPath ?? string.Empty; }
            set { _selectedPath = value; }
        }

        public bool ShowNewFolderButton { get; set; }

        public void OnImportsSatisfied()
        {
            this._configService.InitializeByConfiguration(this);
            this._configService.ConfigurationChangedEvent += OnConfigurationChanges;
            OnConfigurationChanges(null, null);
        }

        private void OnConfigurationChanges(object sender, ConfigurationChangedEventArgs e)
        {
            this.SelectedPath = string.IsNullOrEmpty(DefaultBrowseLocation) ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) : DefaultBrowseLocation;
        }

        public bool? ShowDialog()
        {
            using (var dialog = CreateDialog())
            {
                var result = dialog.ShowDialog() == DialogResult.OK;
                if (result)
                {
                    SelectedPath = dialog.SelectedPath;
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
                    SelectedPath = dialog.SelectedPath;
                }
                return result;
            }
        }

        private FolderBrowserDialog CreateDialog()
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = this.Description;
            dialog.RootFolder = this.RootFolder;
            dialog.SelectedPath = this.SelectedPath;
            dialog.ShowNewFolderButton = this.ShowNewFolderButton;
            return dialog;
        }
    }

    internal static class WindowExtensions
    {
        public static System.Windows.Forms.IWin32Window AsWin32Window(this Window window)
        {
            return new Wpf32Window(window);
        }
    }

    internal class Wpf32Window : System.Windows.Forms.IWin32Window
    {
        public Wpf32Window(Window window)
        {
            Handle = new WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle { get; private set; }
    }
}
