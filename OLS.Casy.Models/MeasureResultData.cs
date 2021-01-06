using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace OLS.Casy.Models
{
    [Serializable]
    public class MeasureResultData : ModelBase
    {
        [NonSerialized]
        private double[] _dataBlock = new double[0];

        [NonSerialized]
        private int _measureResultDataId = -1;
        private string _internalDataBlock;
        private int _belowMeasureLimitCount;
        private int _aboveCalibrationLimitCount;
        private bool _concentrationTooHigh;
        private string _color;
        private string _createdBy;

        [OptionalField]
        private DateTimeOffset _createdAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _createdAt;

        private string _lastModifiedBy;

        [OptionalField]
        private DateTimeOffset _lastModifiedAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _lastModifiedAt;

        private int _belowCalibrationLimitCount;

        public int MeasureResultDataId
        {
            get { return _measureResultDataId; }
            set { this._measureResultDataId = value; }
        }

        public double[] DataBlock
        {
            get
            {
                if(this._dataBlock == null)
                {
                    this._dataBlock = new double[0];
                }

                if(_dataBlock.Length == 0 && this.InternalDataBlock != null)
                {
                    this._dataBlock = this.InternalDataBlock.Split(';').Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)).ToArray();
                }
                return _dataBlock;
            }
            set
            {
                this._dataBlock = value;
                ushort[] ushortArray = this._dataBlock.Select(d => (ushort) d).ToArray();
                this.InternalDataBlock = string.Join(";", ushortArray);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string InternalDataBlock
        {
            get { return _internalDataBlock; }
            set { this._internalDataBlock = value; }
        }

        public int BelowMeasureLimtCount
        {
            get { return _belowMeasureLimitCount; }
            set { this._belowMeasureLimitCount = value; }
        }

        public int BelowCalibrationLimitCount
        {
            get { return _belowCalibrationLimitCount; }
            set { _belowCalibrationLimitCount = value; }
        }

        public int AboveCalibrationLimitCount
        {
            get { return _aboveCalibrationLimitCount; }
            set { this._aboveCalibrationLimitCount = value; }
        }

        public bool ConcentrationTooHigh
        {
            get { return _concentrationTooHigh; }
            set { this._concentrationTooHigh = value; }
        }

        public string Color
        {
            get { return _color; }
            set
            {
                this._color = value;
                NotifyOfPropertyChange();
            }
        }

        public MeasureResult MeasureResult { get; set; }

        public string CreatedBy
        {
            get { return _createdBy; }
            set { this._createdBy = value; }
        }

        public DateTimeOffset CreatedAt
        {
            get { return _createdAtOffset; }
            set { this._createdAtOffset = value; }
        }

        public string LastModifiedBy
        {
            get { return _lastModifiedBy; }
            set { this._lastModifiedBy = value; }
        }

        public DateTimeOffset LastModifiedAt
        {
            get { return _lastModifiedAtOffset; }
            set { this._lastModifiedAtOffset = value; }
        }

        /*
        public bool IsDelete { get; set; }

        public string DeletedBy { get; set; }

        public DateTime DeletedAt { get; set; }
        */

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            if (_createdAt != default(DateTime))
            {
                _createdAtOffset = new DateTimeOffset(_createdAt);
                _createdAt = default(DateTime);
            }

            if (_lastModifiedAt != default(DateTime))
            {
                _lastModifiedAtOffset = new DateTimeOffset(_lastModifiedAt);
                _lastModifiedAt = default(DateTime);
            }
        }

        public void Migrate()
        {
            if (_createdAt != default(DateTime))
            {
                _createdAtOffset = new DateTimeOffset(_createdAt);
                _createdAt = default(DateTime);
            }

            if (_lastModifiedAt != default(DateTime))
            {
                _lastModifiedAtOffset = new DateTimeOffset(_lastModifiedAt);
                _lastModifiedAt = default(DateTime);
            }
        }
    }
}
