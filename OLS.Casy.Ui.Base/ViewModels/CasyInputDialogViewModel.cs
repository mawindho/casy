using OLS.Casy.Core.Localization.Api;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CasyInputDialogViewModel))]
    public class CasyInputDialogViewModel : CasyMessageDialogViewModel
    {
        private string _input;
        private string _inputWatermark;
        private bool _canOk = false;

        [ImportingConstructor]
        public CasyInputDialogViewModel(ILocalizationService localizationService)
            :base(localizationService)
        {
        }

        public override bool CanOk
        {
            get
            {
                return _canOk;
            }
        }

        public string Input
        {
            get { return _input; }
            set
            {
                if(value != this._input)
                {
                    this._input = value;
                    NotifyOfPropertyChange();

                    CheckCanOk();
                }
            }
        }

        public string InputWatermark
        {
            get { return LocalizationService.GetLocalizedString(_inputWatermark); }
            set
            {
                if(value != _inputWatermark)
                {
                    this._inputWatermark = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public Func<string, bool> CanOkDelegate { get; set; }

        private void CheckCanOk()
        {
            Task.Factory.StartNew(() =>
            {
                _canOk = CanOkDelegate == null || CanOkDelegate.Invoke(this.Input);
                NotifyOfPropertyChange("CanOk");
            });
        }
    }
}
