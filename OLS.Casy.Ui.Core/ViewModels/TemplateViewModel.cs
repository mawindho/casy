using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Core.ViewModels
{
    public class TemplateViewModel : ViewModelBase, ITemplateViewModel
    {
        private readonly IMeasureController _measureController;
        private readonly ILocalizationService _localizationService;
        private MeasureSetup _template;
        private bool _isSelected;
        private bool _isFavorite;
        private int _order;

        public TemplateViewModel(MeasureSetup template, IMeasureController measureController, ILocalizationService localizationService)
        {
            this._template = template;
            this._measureController = measureController;
            this._localizationService = localizationService;
        }

        public MeasureSetup Template
        {
            get { return _template; }
        }

        public string Name
        {
            get { return this._template.Name; }
        }

        public string Capillary
        {
            get { return this._template.CapillarySize.ToString(); }
        }

        public string ToDiameter
        {
            get { return this._template.ToDiameter.ToString(); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if(value != _isSelected)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange();

                    if(this._isSelected)
                    {
                        this._measureController.SelectedTemplate = this._template;
                    }
                }
            }
        }

        public bool IsFavorite
        {
            get { return _isFavorite; }
            set
            {
                if(value != _isFavorite)
                {
                    this._isFavorite = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int TemplateId
        {
            get { return _template.MeasureSetupId; }
        }

        public string FirstRow
        {
            get
            {
                return string.Format("{0} {1} \u00b5m - {2} {3} \u00b5m",
              this._localizationService.GetLocalizedString("SelectTemplateView_Label_Capillary"),
              this._template.CapillarySize.ToString(),
              this._localizationService.GetLocalizedString("SelectTemplateView_Label_Range"),
              this._template.ToDiameter.ToString());
            }
        }

        public string SecondRow
        {
            get
            {
                return string.Format("{0} x {1} \u00b5l - {2}",
              this._template.Repeats.ToString(),
              ((int)this._template.Volume).ToString(),
              this._template.MeasureMode == Casy.Models.Enums.MeasureModes.Viability ? _localizationService.GetLocalizedString("MeasureMode_Viability_Name") : string.Format("{0} {1}", _template.Cursors.Count.ToString(), _localizationService.GetLocalizedString("SelectTemplateView_Label_Ranges")));
            }
        }

        public string ThirdRow
        {
            get
            {
                return string.Format("{0}µl / {1}ml ({2} {3})",
                    _template.DilutionSampleVolume.ToString(),
                    _template.DilutionCasyTonVolume.ToString(),
                    _localizationService.GetLocalizedString("SelectTemplateView_Label_Dilution"),
                    _template.DilutionFactor.ToString());
            }
        }

        public int Order
        {
            get { return _order; }
            set
            {
                if(value != _order)
                {
                    this._order = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
