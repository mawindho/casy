using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System.Windows.Input;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.Api
{
    public abstract class CommandViewModel : ViewModelBase
    {
        private string _displayName;

        private string _glyph;
        private bool _isEnabled;
        private UserRole _minRequiredRole;
        private bool _isVisible = true;
        private PackIconFontAwesomeKind _awesomeGlyph = PackIconFontAwesomeKind.None;

        public CommandViewModel()
        {
            IsEnabled = true;
        }

        public ICommand Command { get; set; }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public UserRole MinRequiredRole
        {
            get { return this._minRequiredRole; }
            set
            {
                this._minRequiredRole = value;
                NotifyOfPropertyChange();
            }
        }
      
        public int Order { get; set; }

        public string DisplayName
        {
            get
            {
                return this._displayName;
            }

            protected set
            {
                this._displayName = value;
                NotifyOfPropertyChange();
            }
        }

        public string Glyph
        {
            get
            {
                return this._glyph;
            }

            set
            {
                this._glyph = value;
                NotifyOfPropertyChange();
            }
        }

        public PackIconFontAwesomeKind AwesomeGlyph
        {
            get => _awesomeGlyph;
            set
            {
                if (value == _awesomeGlyph) return;
                _awesomeGlyph = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
