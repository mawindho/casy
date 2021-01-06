using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microcharts;
using OLS.Casy.App.Helpers;
using OLS.Casy.App.Models;
using OLS.Casy.App.Models.Enums;
using OLS.Casy.App.Services.MeasureResults;
using OLS.Casy.App.ViewModels.Base;
using SkiaSharp;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace OLS.Casy.App.ViewModels
{
    public class OverlayViewModel : ViewModelBase
    {
        private readonly IMeasureResultsService _measureResultsService;
        private IEnumerable<MeasureResult> _overlayMeasureResults;

        public OverlayViewModel(IMeasureResultsService measureResultsService)
        {
            _measureResultsService = measureResultsService;
            _measureResultsService.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
        }

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;

            _overlayMeasureResults = await _measureResultsService.GetOverlay();
            RaisePropertyChanged(() => ChartViewModels);

            IsBusy = false;
        }

        public IEnumerable<ChartViewModel> ChartViewModels
        {
            get
            {
                var result = new List<ChartViewModel>();

                if (_overlayMeasureResults == null || !_overlayMeasureResults.Any())
                    return result;

                IsBusy = true;

                var firstMeasureResult = _overlayMeasureResults.First();
                var length = firstMeasureResult.MeasureResultDataItems.First().DataBlock.Length;
                var dLength = (double)length;
                var rangeViewModels = new List<RangeViewModel>();
                if (firstMeasureResult.Ranges.Any())
                {
                    var ranges = firstMeasureResult.Ranges.OrderBy(x => x.MinLimit);

                    var firstRange = ranges.First();

                    var firstMinChannel = Calculations.CalcChannel(0, firstMeasureResult.ToDiameter,
                                              firstRange.MinLimit, length) - 1;

                    if (firstMinChannel > 0)
                    {
                        var firstRangeViewModel = new RangeViewModel()
                        {
                            IsTransparent = false,
                            WidthPercentage = (1d / dLength) * (firstMinChannel + 1)
                        };
                        rangeViewModels.Add(firstRangeViewModel);
                    }

                    var firstMaxChannel = Calculations.CalcChannel(0, firstMeasureResult.ToDiameter,
                        firstRange.MaxLimit, length);
                    var firstRangeViewModel2 = new RangeViewModel()
                    {
                        IsTransparent = true,
                        WidthPercentage = (1d / dLength) *
                                          ((firstMaxChannel + 1) - (firstMinChannel < 0 ? 0 : firstMinChannel + 1)),
                        RangeName = firstRange.Name,
                        Subpopulation = firstRange.Subpopulation
                    };

                    rangeViewModels.Add(firstRangeViewModel2);

                    var lastRange = ranges.Last();

                    if (firstRange == lastRange)
                    {
                        var lastRangeViewModel = new RangeViewModel()
                        {
                            IsTransparent = false,
                            WidthPercentage = (1d / dLength) * (1024 - firstMaxChannel + 1)
                        };
                        rangeViewModels.Add(lastRangeViewModel);
                    }
                    else
                    {
                        var lastMaxChannel = firstMaxChannel;
                        foreach (var range in ranges)
                        {
                            if (range == firstRange) continue;
                            ;
                            var rangeMinChannel = Calculations.CalcChannel(0, firstMeasureResult.ToDiameter,
                                                      range.MinLimit, length) - 1;

                            var rangeViewModel = new RangeViewModel()
                            {
                                IsTransparent = false,
                                WidthPercentage = (1d / dLength) * (rangeMinChannel + 1 - lastMaxChannel + 1)
                            };
                            rangeViewModels.Add(rangeViewModel);

                            var rangeMaxChannel = Calculations.CalcChannel(0, firstMeasureResult.ToDiameter,
                                range.MaxLimit, length);

                            lastMaxChannel = rangeMaxChannel;

                            var rangeViewModel2 = new RangeViewModel()
                            {
                                IsTransparent = true,
                                WidthPercentage = (1d / dLength) * (rangeMaxChannel + 1 - rangeMinChannel + 1),
                                RangeName = range.Name,
                                Subpopulation = firstRange.Subpopulation
                            };

                            rangeViewModels.Add(rangeViewModel2);

                            if (range == lastRange)
                            {
                                var lastRangeViewModel = new RangeViewModel()
                                {
                                    IsTransparent = false,
                                    WidthPercentage = (1d / dLength) * (1024 - rangeMaxChannel + 1)
                                };
                                rangeViewModels.Add(lastRangeViewModel);
                            }
                        }
                    }
                }

                bool isLast = false;
                foreach (var measureResult in _overlayMeasureResults)
                {
                    isLast = _overlayMeasureResults.IndexOf(measureResult) == _overlayMeasureResults.Count() - 1;

                    var entries = new List<ChartEntry>(measureResult.MeasureResultDataItems.First().DataBlock.Length);
                    
                    for (var i = 0; i < length; i++)
                    {
                        var value = (float) measureResult.MeasureResultDataItems.Sum(x => x.DataBlock[i]);

                        var smoothedDiameter = Calculations.CalcSmoothedDiameter(0,
                            measureResult.ToDiameter, i,
                            length);

                        if (i % 32 == 0 || i == length - 1)
                        {
                            entries.Add(new ChartEntry(value)
                            {
                                Color = SKColor.Parse(measureResult.Color),
                                Label = $"{smoothedDiameter:F}",
                                TextColor = SKColors.White,
                                ValueLabel = " "
                            });
                        }
                        else
                        {
                            entries.Add(new ChartEntry(value)
                            {
                                Color = SKColor.Parse(measureResult.Color),
                                TextColor = SKColors.White,
                                Label = "",
                                ValueLabel = ""
                            });
                        }
                    }

                    var lineChart = new LineChart()
                    {
                        MinValue = 0,
                        MaxValue = measureResult.ToDiameter,
                        Entries = entries,
                        LineMode = LineMode.Straight,
                        PointMode = PointMode.None,
                        LineSize = 1,
                        BackgroundColor = SKColors.Transparent,
                        LabelOrientation = Orientation.Vertical,
                        IsAnimated = false,
                        LabelTextSize = 16f,
                        LabelColor = SKColors.White,
                        AnimationDuration = TimeSpan.Zero,
                        LineAreaAlpha = 0
                    };

                    var chartViewModel = new ChartViewModel
                    {
                        Chart = lineChart,
                        Color = Color.FromHex(measureResult.Color),
                        MeasurementName = string.Join("; ", _overlayMeasureResults.Select(x => x.Name))
                    //HasComment = !string.IsNullOrEmpty(measureResult.Comment),
                    //Comment = measureResult.Comment,
                    //HasSubpopulations = measureResult.HasSubpopulations,
                    //CountsTitle = measureResult.MeasureMode == MeasureModes.Viability ? "CELLS / ML" : "COUNTS / ML",
                    //PercentageTitle = measureResult.MeasureMode == MeasureModes.Viability ? "VIABILITY" : "% COUNT",
                    //SubPopAWidth = measureResult.Ranges.Any(x => x.Subpopulation == "A") ? 65d : 0d,
                    //SubPopBWidth = measureResult.Ranges.Any(x => x.Subpopulation == "B") ? 65d : 0d,
                    //SubPopCWidth = measureResult.Ranges.Any(x => x.Subpopulation == "C") ? 65d : 0d,
                    //SubPopDWidth = measureResult.Ranges.Any(x => x.Subpopulation == "D") ? 65d : 0d,
                    //SubPopEWidth = measureResult.Ranges.Any(x => x.Subpopulation == "E") ? 65d : 0d,
                    //TotalCountsLabel = $"Total Counts ({measureResult.Repeats} x {(int)measureResult.Volume}):",
                    //TotalCounts = measureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.Counts && string.IsNullOrEmpty(x.AssociatedRange)).ResultItemValue.ToString("G"),
                    //TotalCountsCursorLabel = measureResult.MeasureMode == MeasureModes.Viability ? "Viable cells:" : "Counts range(s):",
                    //TotalCountsCursor = totalCountsCursor.ToString("G"),
                    //CountsAboveDiameterLabel = $"Counts / ml > {measureResult.ToDiameter} µm:",
                    //CountsAboveDiameter = measureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsAboveDiameter).ResultItemValue.ToString("G"),
                    //Concentration = measureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.Concentration).ResultItemValue == 0d ? "OK" : "TOO HIGH"
                };

                    if (isLast)
                    {
                        chartViewModel.RangeViewModels = rangeViewModels;
                        chartViewModel.ChartViewModels = new List<ChartViewModel>();

                        foreach (var measureResult2 in _overlayMeasureResults)
                        {
                            var chartViewModel2 = new ChartViewModel()
                            {
                                HasComment = !string.IsNullOrEmpty(measureResult2.Comment),
                                Comment = measureResult2.Comment,
                                HasSubpopulations = measureResult2.HasSubpopulations,
                                CountsTitle = measureResult2.MeasureMode == MeasureModes.Viability
                                    ? "CELLS / ML"
                                    : "COUNTS / ML",
                                PercentageTitle = measureResult2.MeasureMode == MeasureModes.Viability
                                    ? "VIABILITY"
                                    : "% COUNT",
                                SubPopAWidth = measureResult2.Ranges.Any(x => x.Subpopulation == "A") ? 65d : 0d,
                                SubPopBWidth = measureResult2.Ranges.Any(x => x.Subpopulation == "B") ? 65d : 0d,
                                SubPopCWidth = measureResult2.Ranges.Any(x => x.Subpopulation == "C") ? 65d : 0d,
                                SubPopDWidth = measureResult2.Ranges.Any(x => x.Subpopulation == "D") ? 65d : 0d,
                                SubPopEWidth = measureResult2.Ranges.Any(x => x.Subpopulation == "E") ? 65d : 0d,
                                Color = Color.FromHex(measureResult2.Color)
                            };
                            
                            var rangeViewModels2 = new List<RangeViewModel>();
                            if (measureResult2.Ranges != null && measureResult2.Ranges.Any())
                            {
                                var ranges = measureResult2.Ranges.OrderBy(x => x.MinLimit);

                                var firstRange = ranges.First();

                                var firstRangeViewModel2 = new RangeViewModel()
                                {
                                    IsTransparent = true,
                                    RangeName = firstRange.Name,
                                    Subpopulation = firstRange.Subpopulation
                                };

                                var countsPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage &&
                                    x.AssociatedRange == firstRange.Name);

                                if (countsPercentageItem != null)
                                {
                                    firstRangeViewModel2.CountsPercentage =
                                        countsPercentageItem.ResultItemValue.ToString("0.0 %");
                                }

                                if (measureResult.AggregationCalculationMode == AggregationCalculationModes.Off)
                                {
                                    firstRangeViewModel2.AggregationFactor = "Off";
                                }
                                else
                                {
                                    var aggrFacResultItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.AssociatedRange == firstRange.Name && x.MeasureResultItemType ==
                                        MeasureResultItemTypes.AggregationFactor);

                                    firstRangeViewModel2.AggregationFactor =
                                        aggrFacResultItem.ResultItemValue.ToString("0.0");
                                }

                                var countsPerMlItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                    x.AssociatedRange == firstRange.Name);

                                if (countsPerMlItem != null)
                                {
                                    firstRangeViewModel2.CountsPerMl =
                                        countsPerMlItem.ResultItemValue.ToString("0.000E+00");
                                }

                                var volumePerMlItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl &&
                                    x.AssociatedRange == firstRange.Name);

                                if (volumePerMlItem != null)
                                {
                                    firstRangeViewModel2.VolumePerMl =
                                        volumePerMlItem.ResultItemValue.ToString("0.000E+00 fl");
                                }

                                var peakVolumeItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.PeakVolume &&
                                    x.AssociatedRange == firstRange.Name);

                                if (peakVolumeItem != null)
                                {
                                    firstRangeViewModel2.PeakVolume =
                                        peakVolumeItem.ResultItemValue.ToString("0.000E+00 fl");
                                }

                                var peakDiameterItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter &&
                                    x.AssociatedRange == firstRange.Name);

                                if (peakDiameterItem != null)
                                {
                                    firstRangeViewModel2.PeakDiameter =
                                        peakDiameterItem.ResultItemValue.ToString("0.00 µm");
                                }

                                var meanDiameterItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter &&
                                    x.AssociatedRange == firstRange.Name);

                                if (meanDiameterItem != null)
                                {
                                    firstRangeViewModel2.MeanDiameter =
                                        meanDiameterItem.ResultItemValue.ToString("0.00 µm");
                                }

                                firstRangeViewModel2.RangeSettings =
                                    $"{firstRange.MinLimit:0.00 µm} - {firstRange.MaxLimit:0.00 µm}";

                                if (firstRange.Subpopulation == "A")
                                {
                                    var subPopAPercentageItem =
                                        measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType ==
                                            MeasureResultItemTypes.SubpopulationAPercentage &&
                                            x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPercentageA = subPopAPercentageItem != null
                                        ? subPopAPercentageItem.ResultItemValue.ToString("0.0 %")
                                        : "";

                                    var subPopACountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                        x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPerMlA = subPopACountsItem != null
                                        ? subPopACountsItem.ResultItemValue.ToString("0.000E+00")
                                        : "";
                                }

                                if (firstRange.Subpopulation == "B")
                                {
                                    var subPopBPercentageItem =
                                        measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType ==
                                            MeasureResultItemTypes.SubpopulationBPercentage &&
                                            x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPercentageB = subPopBPercentageItem != null
                                        ? subPopBPercentageItem.ResultItemValue.ToString("0.0 %")
                                        : "";

                                    var subPopBCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                        x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPerMlB = subPopBCountsItem != null
                                        ? subPopBCountsItem.ResultItemValue.ToString("0.000E+00")
                                        : "";
                                }

                                if (firstRange.Subpopulation == "C")
                                {
                                    var subPopCPercentageItem =
                                        measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType ==
                                            MeasureResultItemTypes.SubpopulationCPercentage &&
                                            x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPercentageC = subPopCPercentageItem != null
                                        ? subPopCPercentageItem.ResultItemValue.ToString("0.0 %")
                                        : "";

                                    var subPopCCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                        x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPerMlC = subPopCCountsItem != null
                                        ? subPopCCountsItem.ResultItemValue.ToString("0.000E+00")
                                        : "";
                                }

                                if (firstRange.Subpopulation == "D")
                                {
                                    var subPopDPercentageItem =
                                        measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType ==
                                            MeasureResultItemTypes.SubpopulationDPercentage &&
                                            x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPercentageD = subPopDPercentageItem != null
                                        ? subPopDPercentageItem.ResultItemValue.ToString("0.0 %")
                                        : "";

                                    var subPopDCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                        x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPerMlD = subPopDCountsItem != null
                                        ? subPopDCountsItem.ResultItemValue.ToString("0.000E+00")
                                        : "";
                                }

                                if (firstRange.Subpopulation == "E")
                                {
                                    var subPopEPercentageItem =
                                        measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType ==
                                            MeasureResultItemTypes.SubpopulationEPercentage &&
                                            x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPercentageE = subPopEPercentageItem != null
                                        ? subPopEPercentageItem.ResultItemValue.ToString("0.0 %")
                                        : "";

                                    var subPopECountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                        x.AssociatedRange == firstRange.Name);
                                    firstRangeViewModel2.CountsPerMlE = subPopECountsItem != null
                                        ? subPopECountsItem.ResultItemValue.ToString("0.000E+00")
                                        : "";
                                }

                                rangeViewModels2.Add(firstRangeViewModel2);

                                var lastRange = ranges.Last();

                                // Only exists one range
                                if (firstRange != lastRange)
                                {
                                    foreach (var range in ranges)
                                    {
                                        if (range == firstRange) continue;

                                        var rangeViewModel2 = new RangeViewModel()
                                        {
                                            IsTransparent = true,
                                            RangeName = range.Name,
                                            Subpopulation = firstRange.Subpopulation
                                        };

                                        if (measureResult.AggregationCalculationMode == AggregationCalculationModes.Off)
                                        {
                                            rangeViewModel2.AggregationFactor = "Off";
                                        }
                                        else
                                        {
                                            var aggrFacResultItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                                x.AssociatedRange == range.Name && x.MeasureResultItemType ==
                                                MeasureResultItemTypes.AggregationFactor);

                                            rangeViewModel2.AggregationFactor =
                                                aggrFacResultItem.ResultItemValue.ToString("0.0");
                                        }

                                        countsPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage &&
                                            x.AssociatedRange == range.Name);

                                        if (countsPercentageItem != null)
                                        {
                                            rangeViewModel2.CountsPercentage =
                                                countsPercentageItem.ResultItemValue.ToString("0.0 %");
                                        }

                                        countsPerMlItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                            x.AssociatedRange == range.Name);

                                        if (countsPerMlItem != null)
                                        {
                                            rangeViewModel2.CountsPerMl =
                                                countsPerMlItem.ResultItemValue.ToString("0.000E+00");
                                        }

                                        volumePerMlItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl &&
                                            x.AssociatedRange == range.Name);

                                        if (volumePerMlItem != null)
                                        {
                                            rangeViewModel2.VolumePerMl =
                                                volumePerMlItem.ResultItemValue.ToString("0.000E+00 fl");
                                        }

                                        peakVolumeItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.PeakVolume &&
                                            x.AssociatedRange == range.Name);

                                        if (peakVolumeItem != null)
                                        {
                                            rangeViewModel2.PeakVolume =
                                                peakVolumeItem.ResultItemValue.ToString("0.000E+00 fl");
                                        }

                                        peakDiameterItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter &&
                                            x.AssociatedRange == range.Name);

                                        if (peakDiameterItem != null)
                                        {
                                            rangeViewModel2.PeakDiameter =
                                                peakDiameterItem.ResultItemValue.ToString("0.00 µm");
                                        }

                                        meanDiameterItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x =>
                                            x.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter &&
                                            x.AssociatedRange == range.Name);

                                        if (meanDiameterItem != null)
                                        {
                                            rangeViewModel2.MeanDiameter =
                                                meanDiameterItem.ResultItemValue.ToString("0.00 µm");
                                        }

                                        rangeViewModel2.RangeSettings = $"{range.MinLimit:0.00 µm} - {range.MaxLimit:0.00 µm}";

                                        if (range.Subpopulation == "A")
                                        {
                                            var subPopAPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationAPercentage && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPercentageA = subPopAPercentageItem != null ? subPopAPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                            var subPopACountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPerMlA = subPopACountsItem != null ? subPopACountsItem.ResultItemValue.ToString("0.000E+00") : "";
                                        }

                                        if (firstRange.Subpopulation == "B")
                                        {
                                            var subPopBPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBPercentage && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPercentageB = subPopBPercentageItem != null ? subPopBPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                            var subPopBCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPerMlB = subPopBCountsItem != null ? subPopBCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                                        }

                                        if (firstRange.Subpopulation == "C")
                                        {
                                            var subPopCPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCPercentage && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPercentageC = subPopCPercentageItem != null ? subPopCPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                            var subPopCCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPerMlC = subPopCCountsItem != null ? subPopCCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                                        }

                                        if (firstRange.Subpopulation == "D")
                                        {
                                            var subPopDPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDPercentage && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPercentageD = subPopDPercentageItem != null ? subPopDPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                            var subPopDCountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPerMlD = subPopDCountsItem != null ? subPopDCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                                        }

                                        if (firstRange.Subpopulation == "E")
                                        {
                                            var subPopEPercentageItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationEPercentage && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPercentageE = subPopEPercentageItem != null ? subPopEPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                            var subPopECountsItem = measureResult2.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                            rangeViewModel2.CountsPerMlE = subPopECountsItem != null ? subPopECountsItem.ResultItemValue.ToString("0.000E+00") : "";
                                        }

                                        rangeViewModels2.Add(rangeViewModel2);
                                    }

                                    chartViewModel2.RangeViewModels = rangeViewModels;
                                }
                            }

                            chartViewModel2.RangeViewModels = rangeViewModels2;

                            chartViewModel.ChartViewModels.Add(chartViewModel2);
                        }
                    }

                    result.Add(chartViewModel);
                }
                return result;
            }
        }

        private void OnSelectedMeasureResultsChanged(object sender, EventArgs e)
        {
            IsBusy = true;
            RaisePropertyChanged(() => ChartViewModels);
            IsBusy = false;
        }
    }
}
