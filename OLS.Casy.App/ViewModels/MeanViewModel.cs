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

namespace OLS.Casy.App.ViewModels
{
    public class MeanViewModel : ViewModelBase
    {
        private readonly IMeasureResultsService _measureResultsService;
        private MeasureResult _meanMeasureResult;
        private IEnumerable<MeasureResult> _parentMeasureResults;

        public MeanViewModel(IMeasureResultsService measureResultsService)
        {
            _measureResultsService = measureResultsService;
            _measureResultsService.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
        }

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;

            var result = await _measureResultsService.GetMean();
            _meanMeasureResult = result.Item1;
            _parentMeasureResults = result.Item2;

            RaisePropertyChanged(() => ChartViewModels);

            IsBusy = false;
        }

        public IEnumerable<ChartViewModel> ChartViewModels
        {
            get
            {
                var result = new List<ChartViewModel>();

                if (_meanMeasureResult == null)
                    return result;

                IsBusy = true;

                var length = _meanMeasureResult.MeasureResultDataItems.First().DataBlock.Length;
                var dLength = (double)length;

                List<ChartEntry> entries;
                LineChart lineChart;
                ChartViewModel chartViewModel;
                /*
                foreach (var measureResult in _parentMeasureResults)
                {
                    entries = new List<ChartEntry>(measureResult.MeasureResultDataItems.First().DataBlock.Length);

                    for (var i = 0; i < length; i++)
                    {
                        var value = (float)measureResult.MeasureResultDataItems.Sum(x => x.DataBlock[i]);

                        var smoothedDiameter = Calculations.CalcSmoothedDiameter(0,
                            measureResult.ToDiameter, i,
                            length);

                        if (i % 32 == 0 || i == length - 1)
                        {
                            entries.Add(new ChartEntry(value)
                            {
                                Color = SKColor.Parse(ReplaceFirst(measureResult.Color, "FF", "44")),
                                Label = $"{smoothedDiameter:F}",
                                TextColor = SKColors.White,
                                ValueLabel = " "
                            });
                        }
                        else
                        {
                            entries.Add(new ChartEntry(value)
                            {
                                Color = SKColor.Parse(ReplaceFirst(measureResult.Color, "FF", "44")),
                                TextColor = SKColors.White,
                                Label = "",
                                ValueLabel = ""
                            });
                        }
                    }

                    lineChart = new LineChart()
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

                    chartViewModel = new ChartViewModel
                    {
                        Chart = lineChart,
                        Color = Color.FromHex(ReplaceFirst(measureResult.Color, "FF", "44")),
                        MeasurementName = " "
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

                    result.Add(chartViewModel);
                }*/


                var rangeViewModels = new List<RangeViewModel>();

                if (_meanMeasureResult.Ranges.Any())
                {
                    var ranges = _meanMeasureResult.Ranges.OrderBy(x => x.MinLimit);

                    var firstRange = ranges.First();

                    var firstMinChannel = Calculations.CalcChannel(0, _meanMeasureResult.ToDiameter,
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

                    var firstMaxChannel = Calculations.CalcChannel(0, _meanMeasureResult.ToDiameter,
                        firstRange.MaxLimit, length);
                    var firstRangeViewModel2 = new RangeViewModel()
                    {
                        IsTransparent = true,
                        WidthPercentage = (1d / dLength) *
                                          ((firstMaxChannel + 1) - (firstMinChannel < 0 ? 0 : firstMinChannel + 1)),
                        RangeName = firstRange.Name,
                        Subpopulation = firstRange.Subpopulation
                    };

                    if (_meanMeasureResult.MeasureMode == MeasureModes.Viability)
                    {
                        var countsPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.Viability &&
                                    x.AssociatedRange == firstRange.Name);

                        if (countsPercentageItem != null)
                        {
                            firstRangeViewModel2.CountsPercentage =
                                countsPercentageItem.ResultItemValue.ToString("0.0 %");

                            if (countsPercentageItem.Deviation.HasValue)
                            {
                                firstRangeViewModel2.CountsPercentage +=
                                    $" (\u00B1 {countsPercentageItem.Deviation.Value:0.0} %)";
                            }
                        }
                    }
                    else
                    {
                        var countsPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage &&
                                    x.AssociatedRange == firstRange.Name);

                        if (countsPercentageItem != null)
                        {
                            firstRangeViewModel2.CountsPercentage =
                                countsPercentageItem.ResultItemValue.ToString("0.0 %");

                            if (countsPercentageItem.Deviation.HasValue)
                            {
                                firstRangeViewModel2.CountsPercentage +=
                                    $" (\u00B1 {countsPercentageItem.Deviation.Value:0.0} %)";
                            }
                        }
                    }

                    if (_meanMeasureResult.AggregationCalculationMode == AggregationCalculationModes.Off)
                    {
                        firstRangeViewModel2.AggregationFactor = "Off";
                    }
                    else
                    {
                        var aggrFacResultItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                            x.AssociatedRange == firstRange.Name && x.MeasureResultItemType ==
                            MeasureResultItemTypes.AggregationFactor);

                        firstRangeViewModel2.AggregationFactor =
                            aggrFacResultItem.ResultItemValue.ToString("0.0");
                    }

                    if (_meanMeasureResult.MeasureMode == MeasureModes.Viability)
                    {
                        var countsPerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.ViableCellsPerMl &&
                        x.AssociatedRange == firstRange.Name);

                        if (countsPerMlItem != null)
                        {
                            firstRangeViewModel2.CountsPerMl =
                                countsPerMlItem.ResultItemValue.ToString("0.000E+00");

                            if (countsPerMlItem.Deviation.HasValue)
                            {
                                firstRangeViewModel2.CountsPerMl +=
                                    $" (\u00B1 {countsPerMlItem.Deviation.Value:0.0} %)";
                            }
                        }
                    }
                    else
                    {
                        var countsPerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                        x.AssociatedRange == firstRange.Name);

                        if (countsPerMlItem != null)
                        {
                            firstRangeViewModel2.CountsPerMl =
                                countsPerMlItem.ResultItemValue.ToString("0.000E+00");

                            if (countsPerMlItem.Deviation.HasValue)
                            {
                                firstRangeViewModel2.CountsPerMl +=
                                    $" (\u00B1 {countsPerMlItem.Deviation.Value:0.0} %)";
                            }
                        }
                    }

                    var volumePerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl &&
                        x.AssociatedRange == firstRange.Name);

                    if (volumePerMlItem != null)
                    {
                        firstRangeViewModel2.VolumePerMl =
                            volumePerMlItem.ResultItemValue.ToString("0.000E+00 fl");

                        if (volumePerMlItem.Deviation.HasValue)
                        {
                            firstRangeViewModel2.VolumePerMl +=
                                $" (\u00B1 {volumePerMlItem.Deviation.Value:0.0} %)";
                        }
                    }

                    var peakVolumeItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.PeakVolume &&
                        x.AssociatedRange == firstRange.Name);

                    if (peakVolumeItem != null)
                    {
                        firstRangeViewModel2.PeakVolume =
                            peakVolumeItem.ResultItemValue.ToString("0.000E+00 fl");

                        if (peakVolumeItem.Deviation.HasValue)
                        {
                            firstRangeViewModel2.PeakVolume +=
                                $" (\u00B1 {peakVolumeItem.Deviation.Value:0.0} %)";
                        }
                    }

                    var peakDiameterItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter &&
                        x.AssociatedRange == firstRange.Name);

                    if (peakDiameterItem != null)
                    {
                        firstRangeViewModel2.PeakDiameter =
                            peakDiameterItem.ResultItemValue.ToString("0.00 µm");

                        if (peakDiameterItem.Deviation.HasValue)
                        {
                            firstRangeViewModel2.PeakDiameter +=
                                $" (\u00B1 {peakDiameterItem.Deviation.Value:0.0} %)";
                        }
                    }

                    var meanDiameterItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                        x.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter &&
                        x.AssociatedRange == firstRange.Name);

                    if (meanDiameterItem != null)
                    {
                        firstRangeViewModel2.MeanDiameter =
                            meanDiameterItem.ResultItemValue.ToString("0.00 µm");

                        if (meanDiameterItem.Deviation.HasValue)
                        {
                            firstRangeViewModel2.MeanDiameter +=
                                $" (\u00B1 {meanDiameterItem.Deviation.Value:0.0} %)";
                        }
                    }

                    firstRangeViewModel2.RangeSettings = $"{firstRange.MinLimit:0.00 µm} - {firstRange.MaxLimit:0.00 µm}";

                    if (firstRange.Subpopulation == "A")
                    {
                        var subPopAPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationAPercentage && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPercentageA = subPopAPercentageItem != null ? subPopAPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                        var subPopACountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPerMlA = subPopACountsItem != null ? subPopACountsItem.ResultItemValue.ToString("0.000E+00") : "";
                    }

                    if (firstRange.Subpopulation == "B")
                    {
                        var subPopBPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBPercentage && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPercentageB = subPopBPercentageItem != null ? subPopBPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                        var subPopBCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPerMlB = subPopBCountsItem != null ? subPopBCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                    }

                    if (firstRange.Subpopulation == "C")
                    {
                        var subPopCPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCPercentage && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPercentageC = subPopCPercentageItem != null ? subPopCPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                        var subPopCCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPerMlC = subPopCCountsItem != null ? subPopCCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                    }

                    if (firstRange.Subpopulation == "D")
                    {
                        var subPopDPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDPercentage && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPercentageD = subPopDPercentageItem != null ? subPopDPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                        var subPopDCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPerMlD = subPopDCountsItem != null ? subPopDCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                    }

                    if (firstRange.Subpopulation == "E")
                    {
                        var subPopEPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationEPercentage && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPercentageE = subPopEPercentageItem != null ? subPopEPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                        var subPopECountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == firstRange.Name);
                        firstRangeViewModel2.CountsPerMlE = subPopECountsItem != null ? subPopECountsItem.ResultItemValue.ToString("0.000E+00") : "";
                    }

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
                            var rangeMinChannel = Calculations.CalcChannel(0, _meanMeasureResult.ToDiameter,
                                                      range.MinLimit, length) - 1;

                            var rangeViewModel = new RangeViewModel()
                            {
                                IsTransparent = false,
                                WidthPercentage = (1d / dLength) * (rangeMinChannel + 1 - lastMaxChannel + 1)
                            };
                            rangeViewModels.Add(rangeViewModel);

                            var rangeMaxChannel = Calculations.CalcChannel(0, _meanMeasureResult.ToDiameter,
                                range.MaxLimit, length);

                            lastMaxChannel = rangeMaxChannel;

                            var rangeViewModel2 = new RangeViewModel()
                            {
                                IsTransparent = true,
                                WidthPercentage = (1d / dLength) * (rangeMaxChannel + 1 - rangeMinChannel + 1),
                                RangeName = range.Name,
                                Subpopulation = firstRange.Subpopulation
                            };

                            if (_meanMeasureResult.AggregationCalculationMode == AggregationCalculationModes.Off)
                            {
                                rangeViewModel2.AggregationFactor = "Off";
                            }
                            else
                            {
                                var aggrFacResultItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.AssociatedRange == range.Name && x.MeasureResultItemType ==
                                    MeasureResultItemTypes.AggregationFactor);

                                rangeViewModel2.AggregationFactor =
                                    aggrFacResultItem.ResultItemValue.ToString("0.0");
                            }

                            if (_meanMeasureResult.MeasureMode == MeasureModes.Viability)
                            {
                                var countsPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.Viability &&
                                    x.AssociatedRange == range.Name);

                                if (countsPercentageItem != null)
                                {
                                    rangeViewModel2.CountsPercentage =
                                        countsPercentageItem.ResultItemValue.ToString("0.0 %");

                                    if (countsPercentageItem.Deviation.HasValue)
                                    {
                                        rangeViewModel2.CountsPercentage +=
                                            $" (\u00B1 {countsPercentageItem.Deviation.Value:0.0} %)";
                                    }
                                }
                            }
                            else
                            {
                                var countsPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage &&
                                    x.AssociatedRange == range.Name);

                                if (countsPercentageItem != null)
                                {
                                    rangeViewModel2.CountsPercentage =
                                        countsPercentageItem.ResultItemValue.ToString("0.0 %");

                                    if (countsPercentageItem.Deviation.HasValue)
                                    {
                                        rangeViewModel2.CountsPercentage +=
                                            $" (\u00B1 {countsPercentageItem.Deviation.Value:0.0} %)";
                                    }
                                }
                            }

                            if (_meanMeasureResult.MeasureMode == MeasureModes.Viability)
                            {
                                var countsPerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.ViableCellsPerMl &&
                                    x.AssociatedRange == range.Name);

                                if (countsPerMlItem != null)
                                {
                                    rangeViewModel2.CountsPerMl =
                                        countsPerMlItem.ResultItemValue.ToString("0.000E+00");

                                    if (countsPerMlItem.Deviation.HasValue)
                                    {
                                        rangeViewModel2.CountsPerMl +=
                                            $" (\u00B1 {countsPerMlItem.Deviation.Value:0.0} %)";
                                    }
                                }
                            }
                            else
                            {
                                var countsPerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                    x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl &&
                                    x.AssociatedRange == range.Name);

                                if (countsPerMlItem != null)
                                {
                                    rangeViewModel2.CountsPerMl =
                                        countsPerMlItem.ResultItemValue.ToString("0.000E+00");

                                    if (countsPerMlItem.Deviation.HasValue)
                                    {
                                        rangeViewModel2.CountsPerMl +=
                                            $" (\u00B1 {countsPerMlItem.Deviation.Value:0.0} %)";
                                    }
                                }
                            }

                            volumePerMlItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                x.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl &&
                                x.AssociatedRange == range.Name);

                            if (volumePerMlItem != null)
                            {
                                rangeViewModel2.VolumePerMl =
                                    volumePerMlItem.ResultItemValue.ToString("0.000E+00 fl");

                                if (volumePerMlItem.Deviation.HasValue)
                                {
                                    rangeViewModel2.VolumePerMl +=
                                        $" (\u00B1 {volumePerMlItem.Deviation.Value:0.0} %)";
                                }
                            }

                            peakVolumeItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                x.MeasureResultItemType == MeasureResultItemTypes.PeakVolume &&
                                x.AssociatedRange == range.Name);

                            if (peakVolumeItem != null)
                            {
                                rangeViewModel2.PeakVolume =
                                    peakVolumeItem.ResultItemValue.ToString("0.000E+00 fl");

                                if (peakVolumeItem.Deviation.HasValue)
                                {
                                    rangeViewModel2.PeakVolume +=
                                        $" (\u00B1 {peakVolumeItem.Deviation.Value:0.0} %)";
                                }
                            }

                            peakDiameterItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                x.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter &&
                                x.AssociatedRange == range.Name);

                            if (peakDiameterItem != null)
                            {
                                rangeViewModel2.PeakDiameter =
                                    peakDiameterItem.ResultItemValue.ToString("0.00 µm");

                                if (peakDiameterItem.Deviation.HasValue)
                                {
                                    rangeViewModel2.PeakDiameter +=
                                        $" (\u00B1 {peakDiameterItem.Deviation.Value:0.0} %)";
                                }
                            }

                            meanDiameterItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x =>
                                x.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter &&
                                x.AssociatedRange == range.Name);

                            if (meanDiameterItem != null)
                            {
                                rangeViewModel2.MeanDiameter =
                                    meanDiameterItem.ResultItemValue.ToString("0.00 µm");

                                if (meanDiameterItem.Deviation.HasValue)
                                {
                                    rangeViewModel2.MeanDiameter +=
                                        $" (\u00B1 {meanDiameterItem.Deviation.Value:0.0} %)";
                                }
                            }

                            rangeViewModel2.RangeSettings = $"{range.MinLimit:0.00 µm} - {range.MaxLimit:0.00 µm}";

                            if (range.Subpopulation == "A")
                            {
                                var subPopAPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationAPercentage && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPercentageA = subPopAPercentageItem != null ? subPopAPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                var subPopACountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPerMlA = subPopACountsItem != null ? subPopACountsItem.ResultItemValue.ToString("0.000E+00") : "";
                            }

                            if (firstRange.Subpopulation == "B")
                            {
                                var subPopBPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBPercentage && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPercentageB = subPopBPercentageItem != null ? subPopBPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                var subPopBCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPerMlB = subPopBCountsItem != null ? subPopBCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                            }

                            if (firstRange.Subpopulation == "C")
                            {
                                var subPopCPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCPercentage && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPercentageC = subPopCPercentageItem != null ? subPopCPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                var subPopCCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPerMlC = subPopCCountsItem != null ? subPopCCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                            }

                            if (firstRange.Subpopulation == "D")
                            {
                                var subPopDPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDPercentage && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPercentageD = subPopDPercentageItem != null ? subPopDPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                var subPopDCountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPerMlD = subPopDCountsItem != null ? subPopDCountsItem.ResultItemValue.ToString("0.000E+00") : "";
                            }

                            if (firstRange.Subpopulation == "E")
                            {
                                var subPopEPercentageItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.SubpopulationEPercentage && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPercentageE = subPopEPercentageItem != null ? subPopEPercentageItem.ResultItemValue.ToString("0.0 %") : "";

                                var subPopECountsItem = _meanMeasureResult.MeasureResultCalculations.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl && x.AssociatedRange == range.Name);
                                rangeViewModel2.CountsPerMlE = subPopECountsItem != null ? subPopECountsItem.ResultItemValue.ToString("0.000E+00") : "";
                            }

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

                entries = new List<ChartEntry>(_meanMeasureResult.MeasureResultDataItems.First().DataBlock.Length);

                for (var i = 0; i < length; i++)
                {
                    var value = (float)_meanMeasureResult.MeasureResultDataItems.Sum(x => x.DataBlock[i]);

                    var smoothedDiameter = Calculations.CalcSmoothedDiameter(0,
                        _meanMeasureResult.ToDiameter, i,
                        length);

                    if (i % 32 == 0 || i == length - 1)
                    {
                        entries.Add(new ChartEntry(value)
                        {
                            Color = SKColor.Parse(_meanMeasureResult.Color),
                            Label = $"{smoothedDiameter:F}",
                            TextColor = SKColors.White,
                            ValueLabel = " "
                        });
                    }
                    else
                    {
                        entries.Add(new ChartEntry(value)
                        {
                            Color = SKColor.Parse(_meanMeasureResult.Color),
                            TextColor = SKColors.White,
                            Label = "",
                            ValueLabel = ""
                        });
                    }
                }

                lineChart = new LineChart()
                {
                    MinValue = 0,
                    MaxValue = _meanMeasureResult.ToDiameter,
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

                var totalCountsCursor =
                    Math.Round(
                        _meanMeasureResult.MeasureResultCalculations
                            .Where(x => x.MeasureResultItemType == MeasureResultItemTypes.Counts &&
                                        !string.IsNullOrEmpty(x.AssociatedRange)).Select(x => x.ResultItemValue)
                            .Sum(), 0);
                chartViewModel = new ChartViewModel
                {
                    Chart = lineChart,
                    Color = Color.FromHex(_meanMeasureResult.Color),
                    MeasurementName = _meanMeasureResult.Name,
                    HasComment = !string.IsNullOrEmpty(_meanMeasureResult.Comment),
                    Comment = _meanMeasureResult.Comment,
                    HasSubpopulations = _meanMeasureResult.HasSubpopulations,
                    CountsTitle = _meanMeasureResult.MeasureMode == MeasureModes.Viability ? "CELLS / ML" : "COUNTS / ML",
                    PercentageTitle = _meanMeasureResult.MeasureMode == MeasureModes.Viability ? "VIABILITY" : "% COUNT",
                    SubPopAWidth = _meanMeasureResult.Ranges.Any(x => x.Subpopulation == "A") ? 65d : 0d,
                    SubPopBWidth = _meanMeasureResult.Ranges.Any(x => x.Subpopulation == "B") ? 65d : 0d,
                    SubPopCWidth = _meanMeasureResult.Ranges.Any(x => x.Subpopulation == "C") ? 65d : 0d,
                    SubPopDWidth = _meanMeasureResult.Ranges.Any(x => x.Subpopulation == "D") ? 65d : 0d,
                    SubPopEWidth = _meanMeasureResult.Ranges.Any(x => x.Subpopulation == "E") ? 65d : 0d,
                    TotalCountsLabel = $"Total Counts ({_meanMeasureResult.Repeats} x {(int)_meanMeasureResult.Volume}):",
                    TotalCounts = _meanMeasureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.Counts && string.IsNullOrEmpty(x.AssociatedRange)).ResultItemValue.ToString("0.##"),
                    TotalCountsCursorLabel = _meanMeasureResult.MeasureMode == MeasureModes.Viability ? "Viable cells:" : "Counts range(s):",
                    TotalCountsCursor = totalCountsCursor.ToString("0.##"),
                    CountsAboveDiameterLabel = $"Counts / ml > {_meanMeasureResult.ToDiameter} µm:",
                    CountsAboveDiameter = _meanMeasureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsAboveDiameter).ResultItemValue.ToString("0.##"),
                    Concentration = _meanMeasureResult.MeasureResultCalculations.First(x => x.MeasureResultItemType == MeasureResultItemTypes.Concentration).ResultItemValue == 0d ? "OK" : "TOO HIGH"
                };

                chartViewModel.RangeViewModels = rangeViewModels;

                result.Add(chartViewModel);

                

                IsBusy = false;

                return result;
            }
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private void OnSelectedMeasureResultsChanged(object sender, EventArgs e)
        {
            IsBusy = true;
            RaisePropertyChanged(() => ChartViewModels);
            IsBusy = false;
        }
    }
}
