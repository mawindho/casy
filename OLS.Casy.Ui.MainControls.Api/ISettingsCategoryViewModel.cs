using OLS.Casy.Models;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.MainControls.Api
{
    public interface ISettingsCategoryViewModel
    {
        int Order { get; }
        bool IsVisible { get; }
        UserRole MinRequiredRole { get; }
        PackIconFontAwesomeKind Glyph { get; }
        string Name { get; }
        ICommand SelectCommand { get; }
        bool IsActive { get; set; }
        bool IsSelectedState { get; set; }
        ChevronState ChevronState { get; set; }
        bool CanOk { get; }
        void OnOk();
        void OnCancel();
    }
}
