using System.Collections.Generic;
using OLS.Casy.Models.Enums;

namespace OLS.Casy.Models
{
    public class MeasureResultItem
    {
        public static IDictionary<MeasureResultItemTypes, string> ValueFormats =
            new Dictionary<MeasureResultItemTypes, string>
            {
                { MeasureResultItemTypes.AggregationFactor, "0.0" },
                { MeasureResultItemTypes.Concentration, "G" },
                { MeasureResultItemTypes.Counts, "G" },
                { MeasureResultItemTypes.CountsAboveDiameter, "0.000E+00" },
                { MeasureResultItemTypes.CountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.CountsPercentage, "0.0 %" },
                { MeasureResultItemTypes.DebrisCount, "0.000E+00" },
                { MeasureResultItemTypes.DebrisVolume, "0.000E+00 fl" },
                { MeasureResultItemTypes.Deviation, "0.0 %" },
                { MeasureResultItemTypes.MeanDiameter, "0.00 µm" },
                { MeasureResultItemTypes.MeanVolume, "0.000E+00 fl" },
                { MeasureResultItemTypes.PeakDiameter, "0.00 µm" },
                { MeasureResultItemTypes.PeakVolume, "0.000E+00 fl" },
                { MeasureResultItemTypes.SubpopulationAPercentage, "0.0 %" },
                { MeasureResultItemTypes.SubpopulationBPercentage, "0.0 %" },
                { MeasureResultItemTypes.SubpopulationCPercentage, "0.0 %" },
                { MeasureResultItemTypes.SubpopulationDPercentage, "0.0 %" },
                { MeasureResultItemTypes.SubpopulationEPercentage, "0.0 %" },
                { MeasureResultItemTypes.SubpopulationACountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.SubpopulationBCountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.SubpopulationCCountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.SubpopulationDCountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.SubpopulationECountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.TotalCountsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.Viability, "0.0 %" },
                { MeasureResultItemTypes.ViableCellsPerMl, "0.000E+00" },
                { MeasureResultItemTypes.VolumePerMl, "0.000E+00 fl" },
                { MeasureResultItemTypes.TotalCellsPerMl, "0.000E+00" }
            };

        public MeasureResultItem(MeasureResultItemTypes measureResultItemType)
        {
            MeasureResultItemType = measureResultItemType;
        }

        public MeasureResultItemTypes MeasureResultItemType { get; }
        public string ValueFormat => ValueFormats[MeasureResultItemType];
        public double ResultItemValue { get; set; }   
        public double? Deviation { get; set; }
        public Cursor Cursor { get; set; }
    }
}
