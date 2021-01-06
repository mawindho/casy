using OLS.Casy.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.Api
{
    public interface ISelectTemplateViewModel
    {
        bool ShowSettings { get; set; }
        ICommand EditTemplateCommand { get; set; }
        ICommand DeleteTemplateCommand { get; set; }
        ObservableCollection<ITemplateViewModel> TemplateViewModels { get; }
        void LoadTemplates();
    }
}
