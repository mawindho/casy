using System;

namespace OLS.Casy.Models
{
    public enum DocumentSettingCategory
    {
        Parameter,
        Result
    }

    public enum DocumentSettingType
    {
        SampleVolume,
        DilutionFactor,
        Capillary,
        Smoothing,
        AggrCorrection,
        SizeScale,
        Ranges,
        Concentration,
        Counts,
        ViableCellsMl,
        DeadCellsMl,
        TotalCellsMl,
        CountsMl,
        TotalCountsMl,
        Viability,
        ViableAggrFactor,
        DebrisMl,
        CellsPercentage,
        AggrFactor,
        CountsMlAboveRange,
        VolumeMlViable,
        MeanDiameterViable,
        PeakDiameterViable,
        MeanVolumeViable,
        PeakVolumeViable,
        VolumeMl,
        MeanDiameter,
        PeakDiameter,
        MeanVolume,
        PeakVolume
    }

    [Serializable]
    public class DocumentSetting
    {
        private DocumentSettingType _type;
        private string _name;
        private bool _isSelected;
        private int _displayOrder;
        private DocumentSettingCategory _category;
        private bool _isViability;
        private bool _isFreeRanges;

        public DocumentSettingType Type
        {
            get => _type;
            set => _type = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => _isSelected = value;
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set => _displayOrder = value;
        }

        public DocumentSettingCategory Category
        {
            get => _category;
            set => _category = value;
        }

        public bool IsViability
        {
            get => _isViability;
            set => _isViability = value;
        }

        public bool IsFreeRanges
        {
            get => _isFreeRanges;
            set => _isFreeRanges = value;
        }
    }
}
