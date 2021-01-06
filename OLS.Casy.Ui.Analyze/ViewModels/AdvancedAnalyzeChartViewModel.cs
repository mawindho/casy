using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.Models;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    //[PartCreationPolicy(CreationPolicy.NonShared)]
    //[Export(typeof(AdvancedAnalyzeChartViewModel))]
    //public class AdvancedAnalyzeChartViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    //{
    //    private readonly ILocalizationService _localizationService;
    //    private readonly IMeasureResultManager _measureResultManager;
    //    private readonly MeasureResultDataChartViewModel _measureResultDataChartViewModel;
    //    private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;

    //    private readonly Dictionary<MeasureResultItemTypes, ComboBoxItemWrapperViewModel<MeasureResultItemTypes>> _availableResultItemTypes;
    //    private List<MeasureResultItemTypes> _selectedResultItemTypes;

    //    [ImportingConstructor]
    //    public AdvancedAnalyzeChartViewModel(
    //        MeasureResultDataChartViewModel measureResultDataChartViewModel,
    //        ILocalizationService localizationService,
    //        IMeasureResultManager measureResultManager,
    //        IMeasureResultDataCalculationService measureResultDataCalculationService
    //        )
    //    {
    //        this._measureResultDataChartViewModel = measureResultDataChartViewModel;
    //        this._localizationService = localizationService;
    //        this._measureResultManager = measureResultManager;
    //        this._measureResultDataCalculationService = measureResultDataCalculationService;

    //        this._availableResultItemTypes = new Dictionary<MeasureResultItemTypes, ComboBoxItemWrapperViewModel<MeasureResultItemTypes>>();
    //    }

    //    public MeasureResultDataChartViewModel MeasureResultDataChartViewModel
    //    {
    //        get { return this._measureResultDataChartViewModel; }
    //    }

    //    public IEnumerable<ComboBoxItemWrapperViewModel<MeasureResultItemTypes>> AvailableResultItemTypes
    //    {
    //        get { return _availableResultItemTypes.Values.ToList(); }
    //    }

    //    public List<object> ResultItemTypes
    //    {
    //        get
    //        {
    //            return this._selectedResultItemTypes == null ? new List<object>() : this._selectedResultItemTypes.Select(i => (object)i).ToList();
    //        }
    //        set
    //        {
    //            this._selectedResultItemTypes = value.Select(i => (MeasureResultItemTypes)i).OrderBy(i => i).ToList();
    //            NotifyOfPropertyChange();
    //            FillData();
    //        }
    //    }

    //    public void OnImportsSatisfied()
    //    {
    //        this._localizationService.LanguageChanged += OnLanguageChanged;

    //        var resultItemTypes = Enum.GetNames(typeof(MeasureResultItemTypes));
    //        foreach (var resultItemType in resultItemTypes)
    //        {
    //            var type = (MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), resultItemType);
    //            var comboBoxWrapperItem = new ComboBoxItemWrapperViewModel<MeasureResultItemTypes>(type);

    //            comboBoxWrapperItem.DisplayItem = _localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", resultItemType));

    //            this._availableResultItemTypes.Add(type, comboBoxWrapperItem);
    //        }

    //        FillAvailableResultItemTypes();
    //    }

    //    private void OnLanguageChanged(object sender, LocalizationEventArgs e)
    //    {
    //        FillAvailableResultItemTypes();
    //    }

    //    private void FillAvailableResultItemTypes()
    //    {
    //        foreach (var resultItemType in _availableResultItemTypes.Values)
    //        {
    //            /*
    //            var toDiameters = _measureResults.Select(mr => mr.MeasureSetup.ToDiameter).Distinct().ToList();
    //            var measureModes = _measureResults.Select(mr => mr.MeasureSetup.MeasureMode).Distinct().ToList();

    //            if (resultItemType.ValueItem == MeasureResultItemTypes.CountsAboveDiameter && toDiameters.Count == 1)
    //            {
    //                resultItemType.DisplayItem = string.Format(_localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", Enum.GetName(typeof(MeasureResultItemTypes), resultItemType.ValueItem))), toDiameters[0].ToString());
    //            }
    //            else 
    //            if (resultItemType.ValueItem == MeasureResultItemTypes.CountsPercentage)
    //            {
    //                if (measureModes.Count == 1 && measureModes[0] == MeasureModes.Viability)
    //                {
    //                    resultItemType.DisplayItem = _localizationService.GetLocalizedString("ResultItemType_Viability_Name");
    //                }
    //                else if (measureModes.Count > 1)
    //                {
    //                    resultItemType.DisplayItem = string.Format("{0} / {1}", _localizationService.GetLocalizedString("ResultItemType_CountsPercentage_Name"), _localizationService.GetLocalizedString("ResultItemType_Viability_Name"));
    //                }
    //                else
    //                {
    //                    resultItemType.DisplayItem = _localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", Enum.GetName(typeof(MeasureResultItemTypes), resultItemType.ValueItem)));
    //                }
    //            }
    //            else if (resultItemType.ValueItem == MeasureResultItemTypes.CountsPerMl)
    //            {
    //                if (measureModes.Count == 1 && measureModes[0] == MeasureModes.Viability)
    //                {
    //                    resultItemType.DisplayItem = _localizationService.GetLocalizedString("ResultItemType_ViableCellsPerMl_Name");
    //                }
    //                else if (measureModes.Count > 1)
    //                {
    //                    resultItemType.DisplayItem = string.Format("{0} / {1}", _localizationService.GetLocalizedString("ResultItemType_CountsPerMl_Name"), _localizationService.GetLocalizedString("ResultItemType_ViableCellsPerMl_Name"));
    //                }
    //                else
    //                {
    //                    resultItemType.DisplayItem = _localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", Enum.GetName(typeof(MeasureResultItemTypes), resultItemType.ValueItem)));
    //                }
    //            }
    //            else
    //            {
    //               resultItemType.DisplayItem = _localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", Enum.GetName(typeof(MeasureResultItemTypes), resultItemType.ValueItem)));
    //            }*/

    //            resultItemType.DisplayItem = _localizationService.GetLocalizedString(string.Format("ResultItemType_{0}_Name", Enum.GetName(typeof(MeasureResultItemTypes), resultItemType.ValueItem)));
    //        }
    //    }

    //    private void FillData()
    //    {
    //        List<ChartDataItemModel<DateTime, double>> chartData = new List<ChartDataItemModel<DateTime, double>>();

    //        int count = 1;
    //        foreach(var resultItemType in this._selectedResultItemTypes)
    //        {
    //            string seriesDescription = Enum.GetName(typeof(MeasureResultItemTypes), resultItemType);

    //            foreach (var measureResult in this._measureResultManager.SelectedMeasureResults)
    //            {
    //                if(count == 1)
    //                {
    //                    //_measureResultDataCalculationService.UpdateMeasureResultData(measureResult);
    //                }

    //                var measureResultItem = measureResult.MeasureResultItems.FirstOrDefault(item => item.MeasureResultItemType == resultItemType);

    //                if(measureResultItem != null)
    //                {
    //                    chartData.Add(new ChartDataItemModel<DateTime, double>(seriesDescription, measureResult.CreatedAt, measureResultItem.ResultItemValue, ((SolidColorBrush)Application.Current.Resources[string.Format("ChartColor{0}", count)]).Color.ToString()));
    //                }
    //            }
    //            count++;
    //        }

    //        this._measureResultDataChartViewModel.SetInitialChartData(chartData);
    //    }
    //}
}
