using System;
using System.Globalization;
using System.Linq;

namespace OLS.Casy.App.Models
{
    public class MeasureResultData
    {
        private double[] _dataBlock;

        public int AboveCalibrationLimitCount { get; internal set; }
        public int BelowCalibrationLimitCount { get; internal set; }
        public object BelowMeasureLimitCount { get; internal set; }
        public bool ConcentrationTooHigh { get; internal set; }
        public double[] DataBlock
        {
            get
            {
                if (_dataBlock == null)
                {
                    _dataBlock = new double[0];
                }

                if (_dataBlock.Length == 0 && InternalDataBlock != null)
                {
                    _dataBlock = InternalDataBlock.Split(';').Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)).ToArray();
                }
                return _dataBlock;
            }
            set
            {
                _dataBlock = value;
                var ushortArray = _dataBlock.Select(d => (ushort)d).ToArray();
                InternalDataBlock = string.Join(";", ushortArray);
            }
        }

        public string InternalDataBlock { get; set; }
    }
}
