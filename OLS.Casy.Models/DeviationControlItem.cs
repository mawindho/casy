using OLS.Casy.Models.Enums;
using System;
using System.Runtime.Serialization;

namespace OLS.Casy.Models
{
    [Serializable]
    public class DeviationControlItem
    {
        [NonSerialized]
        private int _deviationControlItemId = -1;
        private MeasureResultItemTypes _measureResultItemType;
        private double? _minLimit;
        private double? _maxLimit;

        [OptionalField]
        private int _version;

        public int DeviationControlItemId
        {
            get { return _deviationControlItemId; }
            set { this._deviationControlItemId = value; }
        }

        public int Version
        {
            get { return _version; }
            set
            {
                this._version = value;
            }
        }

        public MeasureResultItemTypes MeasureResultItemType
        {
            get { return _measureResultItemType; }
            set { this._measureResultItemType = value; }
        }

        public double? MinLimit
        {
            get { return _minLimit; }
            set { this._minLimit = value; }
        }

        public double? MaxLimit
        {
            get { return _maxLimit; }
            set { this._maxLimit = value; }
        }

        public MeasureSetup MeasureSetup { get; set; }
    }
}
