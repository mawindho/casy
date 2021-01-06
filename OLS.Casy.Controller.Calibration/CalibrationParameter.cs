namespace OLS.Casy.Controller.Calibration
{
    /// <summary>
    /// Model class for holding all calibration data
    /// </summary>
    public class CalibrationParameter
    {
        /// <summary>
        /// Property for the version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Property for the FromDiameter (min x-axis value)
        /// </summary>
        public ushort FromDiameter { get; set; }

        /// <summary>
        /// Property for the ToDiameter (max x-axis value)
        /// </summary>
        public ushort ToDiameter { get; set; }

        /// <summary>
        /// Property for the serial number the calibration is valid for
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Property for the signature
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Property for the capillary size the calibration is valid for
        /// </summary>
        public ushort CapillarySize { get; set; }

        /// <summary>
        /// Property for the amplification
        /// </summary>
        public byte Amplification { get; set; }

        /// <summary>
        /// Property for the voltage
        /// </summary>
        public byte Voltage { get; set; }

        /// <summary>
        /// Property for the A-discriminator
        /// </summary>
        public byte ADiscriminator { get; set; }

        /// <summary>
        /// Property for the Bdiscriminator
        /// </summary>
        public byte BDiscriminator { get; set; }

        /// <summary>
        /// Property for the minimum pulse length
        /// </summary>
        public byte MinPulseLength { get; set; }

        /// <summary>
        /// Property for the low time 1
        /// </summary>
        public ushort LowTime1 { get; set; }

        /// <summary>
        /// Property for the low time 2
        /// </summary>
        public ushort LowTime2 { get; set; }

        /// <summary>
        /// Property for the low time 3
        /// </summary>
        public ushort LowTime3 { get; set; }

        /// <summary>
        /// Property for the time 1
        /// </summary>
        public ushort Time1 { get; set; }

        /// <summary>
        /// Property for the time 2
        /// </summary>
        public ushort Time2 { get; set; }

        /// <summary>
        /// Property for the time 3
        /// </summary>
        public ushort Time3 { get; set; }

        /// <summary>
        /// Property for the volume correction factor for 200er measurement
        /// </summary>
        public ushort VolumeCorr200 { get; set; }

        /// <summary>
        /// Property for the volume correction factor for 400er measurement
        /// </summary>
        public ushort VolumeCorr400 { get; set; }

        /// <summary>
        /// Property for the maximum bubble count
        /// </summary>
        public ushort MaxBubble { get; set; }

        /// <summary>
        /// Property for the max counts
        /// </summary>
        public uint MaxCounts { get; set; }

        /// <summary>
        /// Property for the high pressure clean value
        /// </summary>
        public ushort HighPressureClean { get; set; }

        /// <summary>
        /// Property fot the time of a high pressure clean
        /// </summary>
        public ushort TimeHighPressureClean { get; set; }

        /// <summary>
        /// Property for the low pressure clean value
        /// </summary>
        public ushort LowPressureClean { get; set; }

        /// <summary>
        /// Property fot the time of a low pressure clean
        /// </summary>
        public ushort TimeLowPressureClean { get; set; }

        /// <summary>
        /// Property for the timeout of a clean
        /// </summary>
        public ushort TimeoutClean { get; set; }

        /// <summary>
        /// Property for the start of high pressure
        /// </summary>
        public ushort HighPressureStart { get; set; }

        /// <summary>
        /// Property for tge time for the high pressure start
        /// </summary>
        public ushort TimeHighPressureStart { get; set; }

        /// <summary>
        /// Property fpr the low pressurce start
        /// </summary>
        public ushort LowPressureStart { get; set; }

        /// <summary>
        /// Property for the time for a low pressure start
        /// </summary>
        public ushort TimeLowPressureStart { get; set; }

        /// <summary>
        /// Property for the maximum pressure for 200er mesurement
        /// </summary>
        public ushort MaxPressureDec200 { get; set; }

        /// <summary>
        /// Property for the maximum pressure for 400er mesurement
        /// </summary>
        public ushort MaxPressureDec400 { get; set; }

        /// <summary>
        /// Property for the time after clean
        /// </summary>
        public ushort TimeAfterClean { get; set; }

        /// <summary>
        /// Property for the calibration data block
        /// </summary>
        public byte[] Calib { get; set; }

        /// <summary>
        /// Property for the measurement limit
        /// </summary>
        public ushort MeasureLimit { get; set; }

        /// <summary>
        /// Property for the scaling limit
        /// </summary>
        public ushort ScalingLimit { get; set; }

        /// <summary>
        /// Property for the check sum of the calibration file
        /// </summary>
        public uint Checksum { get; set; }

        public byte[] OrigData { get; set; }
    }
}
