using OLS.Casy.Core;
using System.Linq;

namespace OLS.Casy.Ui.Base.ViewModels.Wizard
{
    public class SelectCapillaryWizardStepViewModel : WizardStepViewModelBase
    {
        private readonly SmartCollection<string> _knownCappillarySizes;
        private string _selectedCapillarySize;
        private string _header;
        private string _text;

        public SelectCapillaryWizardStepViewModel(int[] knownCapillarySizes, int selectedCapillary)
        {
            this._knownCappillarySizes = new SmartCollection<string>(knownCapillarySizes.Select(capillary => capillary.ToString()));
            this._selectedCapillarySize = selectedCapillary == 0 ? null : selectedCapillary.ToString();
        }

        public override bool CanNextButtonCommand
        {
            get { return !string.IsNullOrEmpty(this._selectedCapillarySize); }
        }

        public string Header
        {
            get { return _header; }
            set
            {
                if (value != _header)
                {
                    _header = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public SmartCollection<string> KnownCappillarySizes
        {
            get
            {
                return this._knownCappillarySizes;
            }
        }

        public string SelectedCapillarySize
        {
            get { return _selectedCapillarySize; }
            set
            {
                if (value != _selectedCapillarySize)
                {
                    this._selectedCapillarySize = string.IsNullOrEmpty(value) ? _knownCappillarySizes.First() : value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanNextButtonCommand");
                }
            }
        }
    }
}
