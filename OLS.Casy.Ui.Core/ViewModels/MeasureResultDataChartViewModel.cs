using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultDataChartViewModel))]
    public class MeasureResultDataChartViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;

        private IEnumerable<ChartDataItemModel<DateTime, double>> _chartData;

        [ImportingConstructor]
        public MeasureResultDataChartViewModel(ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
        }

        public string XAxisTitle
        {
            get { return _localizationService.GetLocalizedString("MeasureResultDataChartViewModel_AxisX_Title"); }
        }

        public string YAxisTitle
        {
            get { return _localizationService.GetLocalizedString("MeasureResultDataChartViewModel_AxisY_Title"); }
        }

        public IEnumerable<ChartDataItemModel<DateTime, double>> ChartData
        {
            get
            {
                return _chartData;
            }
        }

        public void SetInitialChartData(IEnumerable<ChartDataItemModel<DateTime, double>> chartData)
        {
            this._chartData = chartData;
            NotifyOfPropertyChange("ChartData");
        }

        public void OnImportsSatisfied()
        {
            this._localizationService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            NotifyOfPropertyChange("XAxisTitle");
            NotifyOfPropertyChange("YAxisTitle");
        }
    }
}
