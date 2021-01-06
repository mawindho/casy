using OLS.Casy.Core.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.Com.Api
{
    /// <summary>
    /// Interface to the implementation communicating to casy device via serial port
    /// </summary>
    public interface ICasySerialPortDriver : IService
    {
        /// <summary>
        /// Returns async the serial number and the check sum of the casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The serial number and corresponding check sum of the casy device</returns>
        Tuple<string, uint> GetSerialNumber(IProgress<string> progress);

        bool SetSerialNumber(string serialNumber, IProgress<string> progress);

        /// <summary>
        /// Starts async a clean on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <param name="cleanCount">Optional: Number of cleans to be executed by the casy device</param>
        /// <returns>The result string of the operation</returns>
        string Clean(IProgress<string> progress, int cleanCount = 1);

        string CleanWaste(IProgress<string> progress);

        string CleanCapillary(IProgress<string> progress);

        /// <summary>
        /// Starts async a self test on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string StartSelfTest(IProgress<string> progress);

        /// <summary>
        /// Starts async a hardware self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string StartHardwareSelfTest(IProgress<string> progress);

        /// <summary>
        /// Starts async a software self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string StartSoftwareSelfTest(IProgress<string> progress);

        /// <summary>
        /// Starts async a pressure system self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string StartPressureSystemSelfTest(IProgress<string> progress);

        /// <summary>
        /// Returns async the last error occured on casy device.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The last error string occured on casy device</returns>
        string GetError(IProgress<string> progress);

        /// <summary>
        /// Calibrates async the casy device with the passed calibration data
        /// </summary>
        /// <param name="calibrationData">Calibration data</param>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string Calibrate(ushort toDiameter, byte[] calibrationData, IProgress<string> progress);

        Tuple<ushort, ushort, uint> GetCalibrationVerifactionData(IProgress<string> progress);

        /// <summary>
        /// Starts async a measurement with the casy device  (200 micro litre).
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string Measure200(IProgress<string> progress);

        /// <summary>
        /// Starts async a measurement with the casy device (400 micro litre).
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        string Measure400(IProgress<string> progress);

        void Stop(IProgress<string> progress);



        /// <summary>
        /// Returns async last measurement result data from casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        byte[] GetBinaryData(IProgress<string> progress);

        Tuple<DateTime, uint> GetDateTime(IProgress<string> progress);

        bool SetDateTime(DateTime dateTime, IProgress<string> progress);

        bool VerifyMasterPin(string masterPin, IProgress<string> progress);

        Tuple<byte[], uint> GetHeader(IProgress<string> progress);

        uint RequestLastChecksum(IProgress<string> progress);

        string CreateTestPattern(IProgress<string> progress);

        string Dry(IProgress<string> progress);

        byte StartLEDTest(IProgress<string> progress);
        bool PerformBlow(IProgress<string> progress);
        bool PerformSuck(IProgress<string> progress);

        bool SetVacuumVentilState(bool state, IProgress<string> progress);
        bool SetPumpEngineState(bool state, IProgress<string> progress);
        bool SetCapillaryRelayVoltage(bool state, IProgress<string> progress);
        bool SetMeasValveRelayVoltage(bool state, IProgress<string> progress);
        bool SetWasteValveRelayVoltage(bool state, IProgress<string> progress);
        byte GetValveState(IProgress<string> progress);
        byte[] GetStatistik(IProgress<string> progress);
        bool SetCleanValveRelayVoltage(bool state, IProgress<string> progress);
        bool SetBlowValveRelayVoltage(bool state, IProgress<string> progress);
        bool SetSuckValveRelayVoltage(bool state, IProgress<string> progress);
        bool SetCapillaryVoltage(int value, IProgress<string> progress);
        double GetCapillaryVoltage(IProgress<string> progress);
        double GetPressure(IProgress<string> progress);
        bool ClearErrorBytes(IProgress<string> progress);
        bool ResetStatistic(IProgress<string> progress);
        bool ResetCalibration(IProgress<string> progress);
        string CheckRisetime(IProgress<string> progress);
        string CheckTightness(IProgress<string> progress);
        bool SendInfo(IProgress<string> progress);

        bool SendSwitchToTTC(IProgress<string> progress);

        /// <summary>
        /// Returns the connection state of the casy serial port device
        /// </summary>
        bool IsConnected { get; }

        bool CheckCasyDeviceConnection(IProgress<string> progress = null);

        /// <summary>
        /// Name of the serial port the casy device is connected to
        /// </summary>
        string ConnectedSerialPort { get; }

        IEnumerable<string> SerialPorts { get; }

        /// <summary>
        /// Event will be raise when connection state has changed
        /// </summary>
        event EventHandler OnIsConnectedChangedEvent;
    }
}
