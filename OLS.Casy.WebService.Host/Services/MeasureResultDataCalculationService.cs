using OLS.Casy.Calculation.Volume;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OLS.Casy.Core;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Base;

namespace OLS.Casy.WebService.Host.Services
{
    public class MeasureResultDataCalculationService
    {
        private readonly VolumeCalculationProvider _volumeCalculationProvider;

        public MeasureResultDataCalculationService(VolumeCalculationProvider volumeCalculationProvider)
        {
            _volumeCalculationProvider = volumeCalculationProvider;
        }

        public async Task<double[]> SumMeasureResultDataAsync(MeasureResult measureResult)
        {
            return await Task.Run(() =>
            {
                if (measureResult.MeasureResultDatas.Count <= 0) return new double[0];

                var summedData = new double[measureResult.MeasureResultDatas.First().DataBlock.Length];

                foreach (var data in measureResult.MeasureResultDatas)
                {
                    for (var i = 1; i < data.DataBlock.Length; i++)
                    {
                        summedData[i] += data.DataBlock[i];
                    }
                }

                return summedData;
            });
        }

        public async Task UpdateMeanDeviationsAsync(MeasureResult meanMeasureResult, MeasureResult[] measureResults)
        {
            var measureResultItems = new List<MeasureResultItem>();

            foreach (var measureResult in measureResults)
            {
                measureResultItems.AddRange(await GetMeasureResultDataAsync(measureResult,
                    meanMeasureResult.MeasureSetup));
            }

            var itemsGroupedByCursor = measureResultItems.GroupBy(item => item.Cursor);

            foreach (var grouped in itemsGroupedByCursor)
            {
                var values = new Dictionary<MeasureResultItemTypes, List<double>>();

                foreach (var measureResultItem in grouped)
                {
                    if (!values.TryGetValue(measureResultItem.MeasureResultItemType, out var calculatedValues))
                    {
                        calculatedValues = new List<double>();
                        values.Add(measureResultItem.MeasureResultItemType, calculatedValues);
                    }

                    calculatedValues.Add(measureResultItem.ResultItemValue);
                }

                foreach (var measureResultItemType in values.Keys)
                {
                    var calculatedValues = values[measureResultItemType];
                    MeasureResultItem measureResultItem = null;
                    if (grouped.Key == null)
                    {
                        if (meanMeasureResult.MeasureResultItemsContainers[measureResultItemType] != null)
                        {
                            measureResultItem = meanMeasureResult.MeasureResultItemsContainers[measureResultItemType]
                                .MeasureResultItem;
                        }
                    }
                    else
                    {
                        if (meanMeasureResult.MeasureResultItemsContainers[measureResultItemType] != null &&
                            meanMeasureResult.MeasureResultItemsContainers[measureResultItemType].CursorItems != null)
                        {
                            measureResultItem = meanMeasureResult.MeasureResultItemsContainers[measureResultItemType]
                                .CursorItems.FirstOrDefault(x => x != null && x.Cursor == grouped.Key);
                        }
                    }

                    if (measureResultItem != null)
                    {
                        measureResultItem.Deviation = Calculations.CalcRelativeDeviation(calculatedValues.ToArray());
                    }
                }
            }
        }

        //public void UpdateMeasureResultData(MeasureResult measureResult, MeasureSetup measureSetup = null /*, IEnumerable<Cursor> cursors = null*/)
        //{
        //    if (measureResult == null) return;
        //    var result = this.GetMeasureResultData(measureResult, measureSetup/*, cursors*/);
        //    foreach(var item in result)
        //    {
        //        measureResult.MeasureResultItems[item.]
        //        measureResult.MeasureResultItems.Add(item);
        //   }
        //}


        public async Task UpdateMeasureResultDataAsync(MeasureResult measureResult, MeasureSetup measureSetup = null)
        {
            await UpdateMeasureResultData(measureResult, measureSetup, true);
        }

        public async Task<IEnumerable<MeasureResultItem>> GetMeasureResultDataAsync(MeasureResult measureResult,
            MeasureSetup measureSetup)
        {
            return await UpdateMeasureResultData(measureResult, measureSetup, false);
        }

        private async Task<IEnumerable<MeasureResultItem>> UpdateMeasureResultData(MeasureResult measureResult,
            MeasureSetup measureSetupParam, bool updateMeasureResultItems)
        {
            return await Task.Run(() =>
            {
                var result = new List<MeasureResultItem>();
                try
                {
                    if (measureResult == null) return result;

                    var measureSetup = measureResult.MeasureSetup;

                    MeasureModes measureMode;
                    Cursor[] cursors;
                    AggregationCalculationModes aggregationCalculationMode;
                    double manualAggregationCalculationFactor;

                    bool isDirty = true;
                    if (measureSetupParam == null)
                    {
                        if (measureSetup == null) return result;

                        cursors = measureSetup.Cursors.ToArray();
                        measureMode = measureSetup.MeasureMode;
                        aggregationCalculationMode = measureSetup.AggregationCalculationMode;
                        manualAggregationCalculationFactor = measureSetup.ManualAggregationCalculationFactor;

                        isDirty = measureSetup.IsDirty || measureSetup.Cursors.Any(x => x.IsDirty);
                    }
                    else
                    {
                        if (measureSetupParam == measureResult.OriginalMeasureSetup)
                        {
                            measureSetup = measureSetupParam;
                        }

                        cursors = measureSetupParam.Cursors.ToArray();
                        measureMode = measureSetupParam.MeasureMode;
                        aggregationCalculationMode = measureSetupParam.AggregationCalculationMode;
                        manualAggregationCalculationFactor =
                            measureSetupParam.ManualAggregationCalculationFactor;

                        isDirty = measureSetupParam.IsDirty || measureSetupParam.Cursors.Any(x => x.IsDirty);
                    }

                    if (!measureResult.IsDirty && !isDirty && updateMeasureResultItems) return result;

                    if (measureMode == MeasureModes.Viability)
                    {
                        if (cursors.Length != 2)
                        {
                            return result;
                        }

                        var deadCells = cursors.FirstOrDefault(c => c.IsDeadCellsCursor);
                        var vitalCells = cursors.FirstOrDefault(c => !c.IsDeadCellsCursor);

                        cursors[0] = deadCells;
                        cursors[1] = vitalCells;
                    }

                    var measureResultItem = new MeasureResultItem(MeasureResultItemTypes.Concentration)
                    {
                        ResultItemValue = measureResult.MeasureResultDatas.Any(data => data.ConcentrationTooHigh)
                            ? 1d
                            : 0d
                    };
                    result.Add(measureResultItem);

                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    if (measureResult.MeasureResultDatas == null || !measureResult.MeasureResultDatas.Any())
                        return result;

                    var firstMeasureResultData = measureResult.MeasureResultDatas.FirstOrDefault();

                    var effectiveMl = Calculations.CalcEffectiveMl((int) measureSetup.Volume,
                        measureSetup.VolumeCorrectionFactor, measureSetup.DilutionFactor,
                        measureResult.IsTemporary ? measureResult.MeasureResultDatas.Count : measureSetup.Repeats);

                    var summedSingleCounts = new double[measureResult.MeasureResultDatas.Count];
                    var summedData = new double[firstMeasureResultData?.DataBlock.Length ?? 0];
                    var summedVolumes = new double[firstMeasureResultData?.DataBlock.Length ?? 0];

                    var aboveCalibrationCount = 0;
                    var index = 0;

                    foreach (var data in measureResult.MeasureResultDatas)
                    {
                        var volumes =
                            _volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, data.DataBlock);
                        for (var i = 1; i < data.DataBlock.Length; i++)
                        {
                            summedData[i] += data.DataBlock[i];
                            summedSingleCounts[index] += data.DataBlock[i];
                            summedVolumes[i] += volumes[i];
                        }

                        index++;
                        aboveCalibrationCount += data.AboveCalibrationLimitCount;
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.CountsAboveDiameter)
                    {
                        ResultItemValue = aboveCalibrationCount * effectiveMl,
                    };
                    result.Add(measureResultItem);

                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    var cumulatedData = new double[summedData.Length];
                    if (summedData.Length > 0)
                    {
                        cumulatedData[0] = summedData[0];

                        for (var i = 1; i < summedData.Length; i++)
                        {
                            cumulatedData[i] = cumulatedData[i - 1] + summedData[i];
                        }
                    }

                    var debrisCount = 0d;
                    var debrisVolume = 0d;
                    var totalCountsPerMl = 0d;

                    var arrayLength = cursors.Length + 1;
                    var counts = new double[arrayLength];
                    var maxCounts = new double[arrayLength];
                    var totalVolumes = new double[arrayLength];
                    var volumePerMl = new double[arrayLength];
                    var totalMeanCount = new double[arrayLength];
                    var peakDiameter = new double[arrayLength];
                    var peakVolume = new double[arrayLength];
                    var meanDiameter = new double[arrayLength];
                    var meanVolumes = new double[arrayLength];
                    var aggregationFactor = new double[arrayLength];
                    var countsPerMl = new double[arrayLength];

                    var totalCountsPerMlInSubPop = new Dictionary<string, double>();

                    for (var i = 0; i < cursors.Length + 1; i++)
                    {
                        if (i == 0)
                        {
                            //Needed for peak diameter
                            maxCounts[i] = summedData.Length == 0 ? 0 : summedData.Max();

                            //Counts
                            counts[i] = cumulatedData.Length == 0 ? 0 : cumulatedData[cumulatedData.Length - 1];

                            totalVolumes[i] = summedVolumes.Sum();
                            volumePerMl[i] = summedVolumes.Sum() * effectiveMl;

                            totalMeanCount[i] = 0d;

                            var peakIndex = Array.IndexOf(summedData, maxCounts[i]);
                            if (measureSetup.SmoothedDiameters != null)
                            {
                                peakDiameter[i] = peakIndex < 0 ? 0 : measureSetup.SmoothedDiameters[peakIndex];
                            }

                            peakVolume[i] = peakIndex < 0 ? 0 : summedVolumes[peakIndex];

                            totalVolumes[i] = summedVolumes.Sum();
                            volumePerMl[i] = summedVolumes.Sum() * effectiveMl;

                            meanVolumes[i] = totalVolumes[i] / counts[i];
                            countsPerMl[i] = counts[i] * effectiveMl;

                            aggregationFactor[i] = 1d;

                            meanDiameter[i] = counts[i] == 0
                                ? 0d
                                : measureSetup.SmoothedDiameters[(int) (totalMeanCount[i] / counts[i])];
                        }
                        else
                        {
                            var cursor = cursors.ElementAt(i - 1);

                            var cursorMinChannel = Calculations.CalcChannel(0,
                                measureResult.MeasureSetup.ToDiameter, cursor.MinLimit,
                                measureResult.MeasureSetup.ChannelCount);

                            if (cursorMinChannel != 0)
                            {
                                cursorMinChannel += 1;
                            }

                            var cursorMaxChannel = Calculations.CalcChannel(0,
                                measureResult.MeasureSetup.ToDiameter, cursor.MaxLimit,
                                measureResult.MeasureSetup.ChannelCount);

                            var countsIndex = i;
                            if (measureMode == MeasureModes.Viability)
                            {
                                countsIndex = cursor.IsDeadCellsCursor ? 1 : 2;
                            }

                            totalMeanCount[countsIndex] = 0;
                            if (summedData.Length == 0)
                            {
                                totalMeanCount[countsIndex] = 0;
                                peakDiameter[countsIndex] = 0;
                                peakVolume[countsIndex] = 0;
                                counts[countsIndex] = 0;
                                maxCounts[countsIndex] = 0;
                            }
                            else
                            {
                                for (var j = cursorMinChannel; j < cursorMaxChannel; j++)
                                {
                                    totalMeanCount[countsIndex] += summedData[j] * j;
                                }

                                var subArray = summedData.SubArray(cursorMinChannel,
                                    (cursorMaxChannel - cursorMinChannel));

                                //Needed for peak diameter
                                maxCounts[countsIndex] = subArray == null || subArray.Length == 0 ? 0d : subArray.Max();

                                //Counts
                                counts[countsIndex] = cumulatedData[cursorMaxChannel] -
                                                      cumulatedData[cursorMinChannel == 0 ? 0 : cursorMinChannel - 1];

                                if (subArray != null && subArray.Length > 0)
                                {
                                    var peakIndex = Array.IndexOf(subArray, maxCounts[countsIndex]) + cursorMinChannel;
                                    if (measureSetup.SmoothedDiameters != null)
                                    {
                                        peakDiameter[countsIndex] = measureSetup.SmoothedDiameters[peakIndex];
                                    }

                                    peakVolume[countsIndex] = measureSetup.VolumeMapping[peakIndex];
                                }
                            }

                            if (summedVolumes.Length == 0)
                            {
                                totalVolumes[countsIndex] = 0;
                                volumePerMl[countsIndex] = 0;
                            }
                            else
                            {
                                var volSubArray = summedVolumes.SubArray(cursorMinChannel,
                                    cursorMaxChannel - cursorMinChannel);

                                if (volSubArray != null)
                                {
                                    totalVolumes[countsIndex] = volSubArray.Sum();
                                    volumePerMl[countsIndex] = volSubArray.Sum() * effectiveMl;
                                }
                            }

                            if (counts[countsIndex] == 0)
                            {
                                meanVolumes[countsIndex] = 0;
                            }
                            else
                            {
                                meanVolumes[countsIndex] = totalVolumes[countsIndex] / counts[countsIndex];
                            }

                            countsPerMl[countsIndex] = counts[countsIndex] * effectiveMl;

                            aggregationFactor[countsIndex] = 1;

                            if (aggregationCalculationMode == AggregationCalculationModes.FromParent)
                            {
                                aggregationCalculationMode = measureSetup.AggregationCalculationMode;
                            }

                            switch (aggregationCalculationMode)
                            {
                                case AggregationCalculationModes.On:
                                    if (peakVolume[countsIndex] == 0)
                                    {
                                        aggregationFactor[countsIndex] = 1;
                                    }
                                    else
                                    {
                                        aggregationFactor[countsIndex] =
                                            meanVolumes[countsIndex] / peakVolume[countsIndex];
                                    }

                                    break;
                                case AggregationCalculationModes.Off:
                                    aggregationFactor[countsIndex] = 1;
                                    break;
                                case AggregationCalculationModes.Manual:
                                    if (manualAggregationCalculationFactor == 0)
                                    {
                                        aggregationFactor[countsIndex] = 1;
                                    }
                                    else
                                    {
                                        aggregationFactor[countsIndex] =
                                            meanVolumes[countsIndex] / manualAggregationCalculationFactor;
                                    }

                                    break;
                            }

                            if (measureMode != MeasureModes.Viability ||
                                !cursors.ElementAt(countsIndex - 1).IsDeadCellsCursor)
                            {
                                countsPerMl[countsIndex] =
                                    counts[countsIndex] * effectiveMl * aggregationFactor[countsIndex];
                            }

                            totalCountsPerMl += countsPerMl[countsIndex];
                            if (!string.IsNullOrEmpty(cursor.Subpopulation))
                            {
                                if (!totalCountsPerMlInSubPop.ContainsKey(cursor.Subpopulation))
                                {
                                    totalCountsPerMlInSubPop.Add(cursor.Subpopulation, 0d);
                                }

                                totalCountsPerMlInSubPop[cursor.Subpopulation] += countsPerMl[countsIndex];
                            }

                            if (measureSetup.SmoothedDiameters != null)
                            {
                                meanDiameter[countsIndex] = counts[countsIndex] == 0
                                    ? 0d
                                    : measureSetup.SmoothedDiameters[
                                        (int) (totalMeanCount[countsIndex] / counts[countsIndex])];
                            }
                        }
                    }

                    if (measureMode == MeasureModes.Viability)
                    {
                        if (summedData.Length == 0)
                        {
                            debrisCount = 0;
                            debrisVolume = 0;
                        }
                        else
                        {
                            var minMinLimit = cursors.Min(item => item.MinLimit);
                            var minCursor = cursors.FirstOrDefault(c => c.MinLimit == minMinLimit);
                            var debriMaxLimit = Calculations.CalcChannel(0, minCursor.MeasureSetup.ToDiameter,
                                minMinLimit, minCursor.MeasureSetup.ChannelCount);

                            var debriSubArray = summedData.SubArray(0, debriMaxLimit == 0 ? 0 : debriMaxLimit);
                            debrisCount = debriSubArray.Sum();

                            var debriVolSubArray = summedVolumes.SubArray(0, debriMaxLimit == 0 ? 0 : debriMaxLimit);
                            debrisVolume = debriVolSubArray.Sum();
                        }
                    }

                    var countsPercentage = new double[arrayLength];

                    for (var i = 1; i < arrayLength; i++)
                    {
                        if (totalCountsPerMl == 0)
                        {
                            countsPercentage[i] = 0;
                        }
                        else
                        {
                            countsPercentage[i] = countsPerMl[i] / totalCountsPerMl;
                        }
                    }

                    if (measureMode == MeasureModes.MultipleCursor)
                    {
                        for (var i = 0; i < cursors.Length; i++)
                        {
                            var cursor = cursors.ElementAt(i);
                            if (string.IsNullOrEmpty(cursor.Subpopulation)) continue;

                            measureResultItem = new MeasureResultItem((MeasureResultItemTypes) Enum.Parse(
                                typeof(MeasureResultItemTypes),
                                $"Subpopulation{cursor.Subpopulation}Percentage"))
                            {
                                Cursor = cursor,
                                ResultItemValue = countsPerMl[i + 1] / totalCountsPerMlInSubPop[cursor.Subpopulation],
                            };
                            result.Add(measureResultItem);

                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }

                            measureResultItem = new MeasureResultItem((MeasureResultItemTypes) Enum.Parse(
                                typeof(MeasureResultItemTypes),
                                $"Subpopulation{cursor.Subpopulation}CountsPerMl"))
                            {
                                Cursor = cursor,
                                ResultItemValue = totalCountsPerMlInSubPop[cursor.Subpopulation],
                            };
                            result.Add(measureResultItem);

                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }
                        }

                        foreach (var subPop in totalCountsPerMlInSubPop.Keys)
                        {
                            measureResultItem = new MeasureResultItem((MeasureResultItemTypes) Enum.Parse(
                                typeof(MeasureResultItemTypes),
                                $"Subpopulation{subPop}Percentage"))
                            {
                                ResultItemValue = totalCountsPerMlInSubPop[subPop] / totalCountsPerMl,
                            };
                            result.Add(measureResultItem);

                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }

                            measureResultItem = new MeasureResultItem((MeasureResultItemTypes) Enum.Parse(
                                typeof(MeasureResultItemTypes),
                                $"Subpopulation{subPop}CountsPerMl"))
                            {
                                ResultItemValue = totalCountsPerMlInSubPop[subPop],
                            };
                            result.Add(measureResultItem);

                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }
                        }
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.DebrisCount)
                    {
                        ResultItemValue = debrisCount * effectiveMl
                    };

                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.DebrisVolume)
                    {
                        ResultItemValue = debrisVolume,
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.CountsPercentage)
                    {
                        ResultItemValue = countsPercentage[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.TotalCountsPerMl)
                    {
                        ResultItemValue = totalCountsPerMl
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.MeanVolume)
                    {
                        ResultItemValue = meanVolumes[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.MeanDiameter)
                    {
                        ResultItemValue = meanDiameter[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.AggregationFactor)
                    {
                        ResultItemValue = aggregationFactor[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.PeakDiameter)
                    {
                        ResultItemValue = peakDiameter[0],
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.PeakVolume)
                    {
                        ResultItemValue = peakVolume[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.CountsPerMl)
                    {
                        ResultItemValue = countsPerMl[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.VolumePerMl)
                    {
                        ResultItemValue = volumePerMl[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    var relativeDeviation =
                        Calculations.CalcRelativeDeviation(summedSingleCounts.Select(i => i).ToArray());

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.Deviation)
                    {
                        ResultItemValue = relativeDeviation
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    measureResultItem = new MeasureResultItem(MeasureResultItemTypes.Counts)
                    {
                        ResultItemValue = counts[0]
                    };
                    result.Add(measureResultItem);
                    if (updateMeasureResultItems)
                    {
                        UpdateMeasureResultItem(measureResult, measureResultItem);
                    }

                    if (cursors.Any())
                    {
                        if (measureMode == MeasureModes.Viability)
                        {
                            measureResultItem = new MeasureResultItem(MeasureResultItemTypes.ViableCellsPerMl)
                            {
                                ResultItemValue = countsPerMl[1],
                                Cursor = cursors.FirstOrDefault(c => c.IsDeadCellsCursor)
                            };
                            result.Add(measureResultItem);
                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }

                            measureResultItem = new MeasureResultItem(MeasureResultItemTypes.Viability)
                            {
                                ResultItemValue = countsPercentage[1],
                                Cursor = cursors.FirstOrDefault(c => c.IsDeadCellsCursor)
                            };
                            result.Add(measureResultItem);
                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }

                            measureResultItem = new MeasureResultItem(MeasureResultItemTypes.ViableCellsPerMl)
                            {
                                ResultItemValue = countsPerMl[2],
                                Cursor = cursors.FirstOrDefault(c => !c.IsDeadCellsCursor)
                            };
                            result.Add(measureResultItem);
                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }

                            measureResultItem = new MeasureResultItem(MeasureResultItemTypes.Viability)
                            {
                                ResultItemValue = countsPercentage[2],
                                Cursor = cursors.FirstOrDefault(c => !c.IsDeadCellsCursor)
                            };
                            result.Add(measureResultItem);
                            if (updateMeasureResultItems)
                            {
                                UpdateMeasureResultItem(measureResult, measureResultItem);
                            }
                        }

                        lock (_lock)
                        {
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.CountsPerMl,
                                countsPerMl, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.TotalCountsPerMl, countsPerMl, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.CountsPercentage, countsPercentage, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.VolumePerMl,
                                volumePerMl, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.PeakDiameter,
                                peakDiameter, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.PeakVolume,
                                peakVolume, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.MeanDiameter,
                                meanDiameter, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.MeanVolume,
                                meanVolumes, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors,
                                MeasureResultItemTypes.AggregationFactor, aggregationFactor, updateMeasureResultItems);
                            CreateMultiCursorResult(ref result, measureResult, cursors, MeasureResultItemTypes.Counts,
                                counts, updateMeasureResultItems);
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine("Blubb");
                }

                return result;
            });
        }

        private static void UpdateMeasureResultItem(MeasureResult measureResult,
            MeasureResultItem measureResultItemParam)
        {
            MeasureResultItem measureResultItem;

            if (measureResultItemParam.Cursor == null)
            {
                measureResultItem = measureResult
                    .MeasureResultItemsContainers[measureResultItemParam.MeasureResultItemType].MeasureResultItem;
            }
            else
            {
                measureResultItem = measureResult
                    .MeasureResultItemsContainers[measureResultItemParam.MeasureResultItemType]
                    .CursorItems
                    .FirstOrDefault(x => x.Cursor == measureResultItemParam.Cursor);
                if (measureResultItem == null)
                {
                    measureResult.MeasureResultItemsContainers[measureResultItemParam.MeasureResultItemType].CursorItems
                        .Add(measureResultItemParam);
                    measureResultItem = measureResultItemParam;
                }
            }

            measureResultItem.ResultItemValue = measureResultItemParam.ResultItemValue;
        }

        private object _lock = new object();

        private void CreateMultiCursorResult(ref List<MeasureResultItem> result, MeasureResult measureResult,
            IReadOnlyList<Cursor> cursors, MeasureResultItemTypes itemType, IReadOnlyList<double> values,
            bool updateMeasureResultItems)
        {
            var newItems = new List<MeasureResultItem>();

            for (var i = 1; i < values.Count; i++)
            {
                var cursor = cursors[i - 1];

                var resultItem = new MeasureResultItem(itemType) {ResultItemValue = values[i], Cursor = cursor};
                result.Add(resultItem);
                newItems.Add(resultItem);

                if (updateMeasureResultItems)
                {
                    UpdateMeasureResultItem(measureResult, resultItem);
                }

            }

            measureResult.MeasureResultItemsContainers[itemType].CursorItems.Clear();
            measureResult.MeasureResultItemsContainers[itemType].CursorItems.AddRange(newItems);
        }
    }
}

