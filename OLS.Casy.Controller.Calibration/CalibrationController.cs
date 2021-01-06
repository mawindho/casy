using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.Models.Enums;
using OLS.Casy.Models;
using System.Threading;
using System.Linq;
using OLS.Casy.Core.Notification.Api;

namespace OLS.Casy.Controller.Calibration
{
    /// <summary>
    /// Implementation of <see cref="ICalibrationController"/>.
    /// Calibrates the casy device via serial port.
    /// Exported as <see cref="IService"/> so it's initiated while bootstrapping process
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ICalibrationController))]
    [Export(typeof(IService))]
    public class CalibrationController : BaseCasyController, ICalibrationController
    {
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IEnvironmentService _environmentService;
        private readonly Dictionary<string, string> _knownCalibrationNames;
        private readonly INotificationService _notificationService;
        private IDictionary<int, IList<int>> _diameterByCappillarySizeMapping;

        private CalibrationParameter _activeCalibrationParameter;

        private static SemaphoreSlim SlowStuffSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        /// <param name="fileSystemStorageService">Implementation of <see cref="IFileSystemStorageService"/></param>
        /// <param name="casySerialPortDriver">Implementation of <see cref="ICasySerialPortDriver"/></param>
        [ImportingConstructor]
        public CalibrationController(IConfigService configService, 
            IFileSystemStorageService fileSystemStorageService,
            [Import(AllowDefault = true)] ICasySerialPortDriver casySerialPortDriver,
            IEnvironmentService environmentService,
            INotificationService notificationService)
            : base(configService, casySerialPortDriver)
        {
            this._fileSystemStorageService = fileSystemStorageService;
            this._environmentService = environmentService;
            this._notificationService = notificationService;

            this._knownCalibrationNames = new Dictionary<string, string>();
            this._diameterByCappillarySizeMapping = new Dictionary<int, IList<int>>();
        }

        /// <summary>
        /// Configuration property to store the location of the calibration files directory
        /// </summary>
        [ConfigItem(@"Calibration")]
        public string CalibrationFileDirectory { get; set; }

        public uint MaxCounts
        {
            get { return _activeCalibrationParameter == null ? 0 : SwapHelper.SwapBytes(_activeCalibrationParameter.MaxCounts); }
        }

        /// <summary>
        /// Returns the list of names of all known calibrations of the system
        /// </summary>
        public IEnumerable<string> KnownCalibrationNames
        {
            get { return _knownCalibrationNames.Keys; }
        }

        /// <summary>
        /// Property returning all known cappillary sizes
        /// </summary>
        public IEnumerable<int> KnownCappillarySizes
        {
            get { return _diameterByCappillarySizeMapping.Keys; }
        }

        /// <summary>
        /// Property returning corresponding diameters for the passed cappillary size
        /// </summary>
        /// <param name="cappillarySize">Cappillary size</param>
        /// <returns>Correxponding diameters</returns>
        public IEnumerable<int> GetDiametersByCappillarySize(int cappillarySize)
        {
            IList<int> diameters;
            if(_diameterByCappillarySizeMapping.TryGetValue(cappillarySize, out diameters))
            {
                return diameters;
            }
            return new List<int>(0);
        }

        public bool IsValidCalibratrion(int capillarySize, int diameter)
        {
            var diameters = this.GetDiametersByCappillarySize(capillarySize);
            return diameters.Contains(diameter);
        }

        /// <summary>
        /// Activiates async the passed calibration in system and casy device
        /// </summary>
        /// <param name="calibratitonName">Name of the calibration to be activated</param>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> to report progress of calibraion process</param>
        /// <returns>Result string of the calibration process</returns>
        private void SetActiveCalibration(MeasureSetup measureSetup)
        {
            var capillarySize = measureSetup.CapillarySize.ToString().PadLeft(3, '0');
            var toDiameter = measureSetup.ToDiameter.ToString().PadLeft(3, '0');
            string calibrationName = string.Format("K000_{0}.{1}", toDiameter, capillarySize);

            _activeCalibrationParameter = ReadCalibrationData(calibrationName, new Progress<string>());
        }

        public string TransferCalibration(IProgress<string> progress, MeasureSetup measureSetup, bool allowDefault)
        {
            byte[] calibrationData;
            if (measureSetup == null && allowDefault)
            {
                calibrationData = GetCalibrationData(ReadCalibrationData(this._knownCalibrationNames.Keys.First(), new Progress<string>()));
            }
            else
            {
                if (measureSetup == null)
                {
                    throw new ArgumentNullException("measureSetup");
                }

                var capillarySize = measureSetup.CapillarySize.ToString().PadLeft(3, '0');
                var toDiameter = measureSetup.ToDiameter.ToString().PadLeft(3, '0');
                string calibrationName = string.Format("K000_{0}.{1}", toDiameter, capillarySize);

                _activeCalibrationParameter = ReadCalibrationData(calibrationName, new Progress<string>());

                calibrationData = _activeCalibrationParameter.OrigData.Skip(17).ToArray();

                //var temp = new string(calibrationData.Take(125).Select(b => Convert.ToChar(b)).ToArray());
                //calibrationData = GetCalibrationData(_activeCalibrationParameter);
            }

            return CasySerialPortDriver.Calibrate((ushort)(measureSetup == null ? 0 : measureSetup.ToDiameter), calibrationData, progress);
        }

        public bool VerifyActiveCalibration(IProgress<string> progress)
        {
            var result = CasySerialPortDriver.GetCalibrationVerifactionData(progress);
            return _activeCalibrationParameter != null && result.Item1 == _activeCalibrationParameter.ToDiameter && result.Item2 == _activeCalibrationParameter.CapillarySize;
        }

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     This  method can be used to initialize the service and perform actions, which do
        ///     not expect other dependent services with OnReady state.
        /// </summary>
        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);

            var calibrationDataDirectory = Path.GetDirectoryName(_environmentService.GetExecutionPath());
            calibrationDataDirectory = Path.Combine(calibrationDataDirectory, "Data", CalibrationFileDirectory);

            var calibrationFiles = _fileSystemStorageService.GetFiles(calibrationDataDirectory);

            foreach (var calibrationFile in calibrationFiles)
            {
                var calibrationFileName = Path.GetFileName(calibrationFile);
                _knownCalibrationNames.Add(calibrationFileName, calibrationFile);
            }

            OnCasySerialPortConnected(null, EventArgs.Empty);
            this.CasySerialPortDriver.OnIsConnectedChangedEvent += OnCasySerialPortConnected;
        }

        /// <summary>
        /// Event will be raised when all calibration data has been loaded and is available to use
        /// </summary>
        public event EventHandler CaibrationDataLoadedEvent;

        private async void OnCasySerialPortConnected(object sender, EventArgs e)
        {
            if(CasySerialPortDriver.IsConnected)
            { 
                await SlowStuffSemaphore.WaitAsync();
                _diameterByCappillarySizeMapping.Clear();

                bool containsInvalidCalibrations = false;

                foreach (var calibrationFile in _knownCalibrationNames.Keys)
                {
                    if (CasySerialPortDriver.IsConnected)
                    {
                        var serialNumber = CasySerialPortDriver.GetSerialNumber(new Progress<string>());
                        var data = ReadCalibrationData(calibrationFile, new Progress<string>());

                        if (data.SerialNumber.Replace("\0", "") == serialNumber.Item1)
                        {
                            var capillarySize = SwapHelper.SwapBytes(data.CapillarySize);

                            IList<int> diameters;
                            if (!_diameterByCappillarySizeMapping.TryGetValue(capillarySize, out diameters))
                            {
                                diameters = new List<int>();
                                _diameterByCappillarySizeMapping.Add(capillarySize, diameters);
                            }
                            if (!diameters.Contains(data.ToDiameter))
                            {
                                diameters.Add(data.ToDiameter);
                            }
                        }
                        else
                        {
                            if(!containsInvalidCalibrations)
                            {
                                containsInvalidCalibrations = true;
                                var notification = this._notificationService.CreateNotification(NotificationType.InvalidCalibrationsFound);
                                notification.Message = "Invalid calibration files has been found and were ignored by the system. File: '" + data.SerialNumber.Replace("\0", "") + "' - Device: '" + serialNumber.Item1 + "'";
                                notification.Title = "Invalid calibration files";
                                notification.ActionDescription = "Acknowledge";
                                notification.Action = () =>
                                {
                                };
                                this._notificationService.AddNotification(notification);
                            }
                        }
                    }
                }

                if (CaibrationDataLoadedEvent != null)
                {
                    foreach (EventHandler receiver in CaibrationDataLoadedEvent.GetInvocationList())
                    {
                        receiver.BeginInvoke(this, EventArgs.Empty, null, null);
                    }

                }
                SlowStuffSemaphore.Release();
            }
        }

        private CalibrationParameter ReadCalibrationData(string calibrationName, IProgress<string> progress)
        {
            string filepath;
            if (!_knownCalibrationNames.TryGetValue(calibrationName, out filepath))
            {
                throw new ArgumentException("Can't find calibration with name: " + calibrationName);
            }

            var result = new CalibrationParameter();

            var task = Task.Run(async () => await _fileSystemStorageService.ReadFileAsync(filepath));
            byte[] fileBytes = task.Result;

            result.OrigData = fileBytes;

            MemoryStream ms = new MemoryStream(fileBytes);
            
            byte[] sCasyBuf = new byte[7];
            ms.Read(sCasyBuf, 0, 7);

            byte[] sVersionBuf = new byte[5];
            ms.Read(sVersionBuf, 0, 5);
            result.Version = Encoding.ASCII.GetString(sVersionBuf);

            //CTRL_Z
            ms.ReadByte();

            byte[] wFromDiameterBuf = new byte[2];
            ms.Read(wFromDiameterBuf, 0, 2);
            result.FromDiameter = BitConverter.ToUInt16(wFromDiameterBuf, 0);

            byte[] wToDiameterBuf = new byte[2];
            ms.Read(wToDiameterBuf, 0, 2);
            result.ToDiameter = BitConverter.ToUInt16(wToDiameterBuf, 0);

            byte[] cSerialNoBuf = new byte[15];
            ms.Read(cSerialNoBuf, 0, 15);
            result.SerialNumber = Encoding.Default.GetString(cSerialNoBuf);

            byte[] cSignatureBuf = new byte[90];
            ms.Read(cSignatureBuf, 0, 90);
            result.Signature = Encoding.Default.GetString(cSignatureBuf);

            byte[] wCapillarySizeBuf = new byte[2];
            ms.Read(wCapillarySizeBuf, 0, 2);
            result.CapillarySize = BitConverter.ToUInt16(wCapillarySizeBuf, 0);

            result.Amplification = (byte)ms.ReadByte();
            result.Voltage = (byte)ms.ReadByte();
            result.ADiscriminator = (byte)ms.ReadByte();
            result.BDiscriminator = (byte)ms.ReadByte();
            result.MinPulseLength = (byte)ms.ReadByte();

            byte[] lowTime1Buf = new byte[2];
            ms.Read(lowTime1Buf, 0, 2);
            result.LowTime1 = BitConverter.ToUInt16(lowTime1Buf, 0);

            byte[] lowTime2Buf = new byte[2];
            ms.Read(lowTime2Buf, 0, 2);
            result.LowTime2 = BitConverter.ToUInt16(lowTime2Buf, 0);

            byte[] lowTime3Buf = new byte[2];
            ms.Read(lowTime3Buf, 0, 2);
            result.LowTime3 = BitConverter.ToUInt16(lowTime3Buf, 0);

            byte[] time1Buf = new byte[2];
            ms.Read(time1Buf, 0, 2);
            result.Time1 = BitConverter.ToUInt16(time1Buf, 0);

            byte[] time2Buf = new byte[2];
            ms.Read(time2Buf, 0, 2);
            result.Time2 = BitConverter.ToUInt16(time2Buf, 0);

            byte[] time3Buf = new byte[2];
            ms.Read(time3Buf, 0, 2);
            result.Time3 = BitConverter.ToUInt16(time3Buf, 0);

            byte[] volumeCorr200Buf = new byte[2];
            ms.Read(volumeCorr200Buf, 0, 2);
            result.VolumeCorr200 = BitConverter.ToUInt16(volumeCorr200Buf, 0);

            byte[] volumeCorr400Buf = new byte[2];
            ms.Read(volumeCorr400Buf, 0, 2);
            result.VolumeCorr400 = BitConverter.ToUInt16(volumeCorr400Buf, 0);

            byte[] maxBubbleBuf = new byte[2];
            ms.Read(maxBubbleBuf, 0, 2);
            result.MaxBubble = BitConverter.ToUInt16(maxBubbleBuf, 0);

            byte[] maxCountsBuf = new byte[4];
            ms.Read(maxCountsBuf, 0, 4);
            result.MaxCounts = BitConverter.ToUInt32(maxCountsBuf, 0);

            byte[] highPressureCleanBuf = new byte[2];
            ms.Read(highPressureCleanBuf, 0, 2);
            result.HighPressureClean = BitConverter.ToUInt16(highPressureCleanBuf, 0);

            byte[] timeHighPressureCleanBuf = new byte[2];
            ms.Read(timeHighPressureCleanBuf, 0, 2);
            result.TimeHighPressureClean = BitConverter.ToUInt16(timeHighPressureCleanBuf, 0);

            byte[] lowPressureCleanBuf = new byte[2];
            ms.Read(lowPressureCleanBuf, 0, 2);
            result.LowPressureClean = BitConverter.ToUInt16(lowPressureCleanBuf, 0);

            byte[] timeLowPressureCleanBuf = new byte[2];
            ms.Read(timeLowPressureCleanBuf, 0, 2);
            result.TimeLowPressureClean = BitConverter.ToUInt16(timeLowPressureCleanBuf, 0);

            byte[] timeoutCleanBuf = new byte[2];
            ms.Read(timeoutCleanBuf, 0, 2);
            result.TimeoutClean = BitConverter.ToUInt16(timeoutCleanBuf, 0);

            byte[] highPressureStartBuf = new byte[2];
            ms.Read(highPressureStartBuf, 0, 2);
            result.HighPressureStart = BitConverter.ToUInt16(highPressureStartBuf, 0);

            byte[] timeHighPressureStartBuf = new byte[2];
            ms.Read(timeHighPressureStartBuf, 0, 2);
            result.TimeHighPressureStart = BitConverter.ToUInt16(timeHighPressureStartBuf, 0);

            byte[] lowPressureStartBuf = new byte[2];
            ms.Read(lowPressureStartBuf, 0, 2);
            result.LowPressureStart = BitConverter.ToUInt16(lowPressureStartBuf, 0);

            byte[] timeLowPressureStartBuf = new byte[2];
            ms.Read(timeLowPressureStartBuf, 0, 2);
            result.TimeLowPressureStart = BitConverter.ToUInt16(timeLowPressureStartBuf, 0);

            byte[] maxPressureDec200Buf = new byte[2];
            ms.Read(maxPressureDec200Buf, 0, 2);
            result.MaxPressureDec200 = BitConverter.ToUInt16(maxPressureDec200Buf, 0);

            byte[] maxPressureDec400Buf = new byte[2];
            ms.Read(maxPressureDec400Buf, 0, 2);
            result.MaxPressureDec400 = BitConverter.ToUInt16(maxPressureDec400Buf, 0);

            byte[] timeAfterCleanBuf = new byte[2];
            ms.Read(timeAfterCleanBuf, 0, 2);
            result.TimeAfterClean = BitConverter.ToUInt16(timeAfterCleanBuf, 0);

            byte[] calibBuf = new byte[2048];
            ms.Read(calibBuf, 0, 2048);
            result.Calib = calibBuf;

            byte[] measureLimitBuf = new byte[2];
            ms.Read(measureLimitBuf, 0, 2);
            result.MeasureLimit = BitConverter.ToUInt16(measureLimitBuf, 0);

            byte[] scalingLimitBuf = new byte[2];
            ms.Read(scalingLimitBuf, 0, 2);
            result.ScalingLimit = BitConverter.ToUInt16(scalingLimitBuf, 0);

            byte[] checksumBuf = new byte[4];
            ms.Read(checksumBuf, 0, 4);
            result.Checksum = BitConverter.ToUInt32(checksumBuf, 0);

            ms.Close();

            return result;
        }

        private byte[] GetCalibrationData(CalibrationParameter parameter)
        {
            byte[] result = new byte[2214];

            int length = 0;
            MemoryStream ms = new MemoryStream(result);
            BinaryWriter sw = new BinaryWriter(ms, Encoding.ASCII);
            
            var buf = Encoding.ASCII.GetBytes(parameter.SerialNumber.PadRight(15, ' '));
            length += buf.Length;
            sw.Write(buf);

            buf = Encoding.ASCII.GetBytes(parameter.Signature);
            length += buf.Length;
            sw.Write(buf);

            buf = BitConverter.GetBytes(parameter.CapillarySize);
            length += buf.Length;
            sw.Write(buf);

            buf = new byte[1] { parameter.Amplification };
            length += buf.Length;
            sw.Write(buf);

            buf = new byte[1] { parameter.Voltage };
            length += buf.Length;
            sw.Write(buf);

            buf = new byte[1] { parameter.ADiscriminator };
            length += buf.Length;
            sw.Write(buf);

            buf = new byte[1] { parameter.BDiscriminator };
            length += buf.Length;
            sw.Write(buf);

            buf = new byte[1] { parameter.MinPulseLength };
            length += buf.Length;

            sw.Write(buf);

            buf = BitConverter.GetBytes(parameter.LowTime1);
            length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.LowTime2);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.LowTime3);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.Time1);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.Time2);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.Time3);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.VolumeCorr200);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.VolumeCorr400);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.MaxBubble);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.MaxCounts);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.HighPressureClean);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeHighPressureClean);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.LowPressureClean);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeLowPressureClean);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeoutClean);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.HighPressureStart);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeHighPressureStart);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.LowPressureStart);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeLowPressureStart);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.MaxPressureDec200);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.MaxPressureDec400);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.TimeAfterClean);
                length += buf.Length;

                sw.Write(buf);

                buf = parameter.Calib;

                length += buf.Length;
                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.MeasureLimit);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.ScalingLimit);
                length += buf.Length;

                sw.Write(buf);

                buf = BitConverter.GetBytes(parameter.Checksum);
                length += buf.Length;

                sw.Write(buf);

            sw.Close();

            return result;
        }

        public double GetVolumeCorrection(MeasureSetup measureSetup)
        {
            if(measureSetup == null)
            {
                throw new ArgumentNullException("measureSetup");
            }

            var capillarySize = measureSetup.CapillarySize.ToString().PadLeft(3, '0');
            var toDiameter = measureSetup.ToDiameter.ToString().PadLeft(3, '0');
            string calibrationName = string.Format("K000_{0}.{1}", toDiameter, capillarySize);

            var calibrationParameter = ReadCalibrationData(calibrationName, new Progress<string>());

            if (measureSetup.Volume == Volumes.FourHundred)
            {
                return SwapHelper.SwapBytes(calibrationParameter.VolumeCorr400);
            }
            else if(measureSetup.Volume == Volumes.TwoHundred)
            {
                return SwapHelper.SwapBytes(calibrationParameter.VolumeCorr200);
            }
            return 0d;
        }
    }
}
