using OLS.Casy.Core.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(AddCommentDialogModel))]
    public class AddCommentDialogModel : DialogModelBase
    {
        private readonly ILocalizationService _localizationService;

        private string _comment;

        [ImportingConstructor]
        public AddCommentDialogModel(
            ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            ButtonResult = ButtonResult.Cancel;
        }

        public ButtonResult ButtonResult { get; private set; }

        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value != _comment)
                {
                    this._comment = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
