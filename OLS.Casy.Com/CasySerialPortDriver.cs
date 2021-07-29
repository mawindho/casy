using OLS.Casy.Com.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OLS.Casy.Base;

namespace OLS.Casy.Com
{
    /// <summary>
    /// Implementation of <see cref="ICasySerialPortDriver"/> for communicating to casy device via serial port
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    [Export(typeof(ICasySerialPortDriver))]
    public class CasySerialPortDriver : AbstractService, ICasySerialPortDriver, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;

        private readonly ISerialPort _serialPort;
        private ManualResetEvent _awaiter;
        private object _responseResult;
        private CasyCommandWrapper _executingCommand;
        private bool _isConnected;
        private string _commandString;
        private string _commandStringPrint;
        private string _responseString;
        private string _errorString;
        private volatile Queue<byte> _receivedBytes = new Queue<byte>();
        private bool _responseSuccess;
        private Tuple<string, uint> _serialNumber;

        private BlockingCollection<CasyCommandWrapper> _commandQueue;

        private ManualResetEvent _currentAwaiter;

        private bool _currentCommandCanceled;
        private readonly CancellationTokenSource _tokenSource;
        
        private readonly ManualResetEvent _connectedAwaiter = new ManualResetEvent(false);

        private readonly object _lock = new object();

        /// <summary>
        /// Importing constructor
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        [ImportingConstructor]
        public CasySerialPortDriver(ILogger logger, 
            IConfigService configService, 
            ILocalizationService localizationService,
            ISerialPort serialPort)
            : base(configService)
        {
            _localizationService = localizationService;
            _logger = logger;
            _serialPort = serialPort;

            _commandQueue = new BlockingCollection<CasyCommandWrapper>();

            _tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(ConsumeCasyCommandWorker, _tokenSource.Token);
        }

        [ConfigItem("Auto")]
        public string SerialPort { get; set; }

        /// <summary>
        /// Returns the connection state of the casy serial port device
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (value == _isConnected) return;
                _isConnected = value;
                RaiseConnectionChangedEvent();
            }
        }

        private void RaiseConnectionChangedEvent()
        {
            if (OnIsConnectedChangedEvent == null) return;
            foreach (var @delegate in OnIsConnectedChangedEvent.GetInvocationList())
            {
                var receiver = (EventHandler) @delegate;
                receiver.BeginInvoke(this, EventArgs.Empty, null, null);
            }
        }

        /// <summary>
        /// Event will be raise when connection state has changed
        /// </summary>
        public event EventHandler OnIsConnectedChangedEvent;

        /// <summary>
        /// Name of the serial port the casy device is connected to
        /// </summary>
        public string ConnectedSerialPort => _serialPort.PortName;

        public IEnumerable<string> SerialPorts => _serialPort.GetPortNames();

        /// <summary>
        /// Method will be called after imports have been satisfied during bootstrapping process
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> to report progress while bootstrapping process</param>
        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);

            progress?.Report(_localizationService.GetLocalizedString("SplashScreen_Message_TryDetectDevice"));

            CheckCasyDeviceConnection(progress);

            progress?.Report(IsConnected
                ? _localizationService.GetLocalizedString("SplashScreen_Message_CasyDeviceDetected",
                    _serialPort.PortName)
                : _localizationService.GetLocalizedString("SplashScreen_Message_UnableConnectCasy"));

            RaiseConnectionChangedEvent();
        }

        private readonly object _conLock = new object();

        public bool CheckCasyDeviceConnection(IProgress<string> progress = null)
        {
            lock (_conLock)
            {
                if (_serialPort != null && _serialPort.IsOpen && IsConnected)
                {
                    return true;
                }

                var arrayComPortsNames = _serialPort.GetPortNames();
                int connectCount = 0;
                while (connectCount < 2)
                {
                    try
                    {
                        if (SerialPort == "Auto")
                        {
                            if (!IsConnected)
                            {
                                foreach (var comPort in arrayComPortsNames)
                                {
                                    progress?.Report($"Trying to connect on serial port \"{comPort}\"");
                                    if (ConnectCasyDriver(comPort))
                                    {
                                        _isConnected = false;
                                        IsConnected = true;
                                        _connectedAwaiter.Set();
                                        return true;
                                    }
                                    else
                                    {
                                        progress?.Report($"Connecting on serial port \"{comPort}\" failed.");
                                    }
                                }
                            }

                            if (arrayComPortsNames.Length == 0 || (_serialPort != null && !_serialPort.IsOpen))
                            {
                                _isConnected = true;
                                IsConnected = false;
                                _connectedAwaiter.Set();
                                return false;
                            }
                        }
                        else
                        {
                            if (!arrayComPortsNames.Contains(SerialPort))
                            {
                                progress?.Report($"Unknown serial port \"{SerialPort}\" configured");
                                _isConnected = true;
                                IsConnected = false;
                                _connectedAwaiter.Set();
                                return false;
                            }

                            if (ConnectCasyDriver(SerialPort))
                            {
                                _isConnected = false;
                                IsConnected = true;
                                _connectedAwaiter.Set();
                                return true;
                            }

                            progress?.Report($"Connecting on serial port \"{SerialPort}\" failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(LogCategory.Instrument, "An error occured while connecting serial port.",
                            () => CheckCasyDeviceConnection(progress), ex);
                    }
                    finally
                    {
                        Thread.Sleep(1000);
                        connectCount++;
                    }
                }

                _isConnected = true;
                IsConnected = false;
                _connectedAwaiter.Set();
                return false;
            }
        }

        /// <summary>
        /// Method will be executed just before application shuts down
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> to report progress while shutdown process</param>
        public override void Deinitialize(IProgress<string> progress)
        {
            base.Deinitialize(progress);
            _tokenSource.Cancel();
            _serialPort.Close();
        }

        /// <summary>
        /// Returns async the serial number and the check sum of the casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The serial number and corresponding check sum of the casy device</returns>
        public Tuple<string, uint> GetSerialNumber(IProgress<string> progress)
        {
            return _serialNumber ?? (_serialNumber =
                       EnqueueCommand(progress, CasyCommand.GETSERIALNO, timeout: 5000) as Tuple<string, uint>);
        }

        public bool SetSerialNumber(string serialNumber, IProgress<string> progress)
        {
            _serialNumber = null;

            var data = new byte[20];
            using (var ms = new MemoryStream(data))
            using (var sw = new BinaryWriter(ms, Encoding.Default))
            {
                var buf = Encoding.Default.GetBytes(serialNumber);
                sw.Write(buf);

                sw.Seek(15, SeekOrigin.Begin);

                var checkSum = SwapHelper.SwapBytes(Calculations.CalcChecksum(data));
                sw.Write(BitConverter.GetBytes(checkSum));
            }

            var result = EnqueueCommand(progress, CasyCommand.SETSERIALNO, new Tuple<string, byte[]>(serialNumber, data));
            return result is bool b && b;
        }

        /// <summary>
        /// Starts async a clean on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <param name="cleanCount">Optional: Number of cleans to be executed by the casy device</param>
        /// <returns>The result string of the operation</returns>
        public string Clean(IProgress<string> progress, int cleanCount = 1)
        {
            if (cleanCount == 1)
            {
                return EnqueueCommand(progress, CasyCommand.CLEAN) as string;
            }

            if (cleanCount > 1)
            {
                return EnqueueCommand(progress, CasyCommand.CLEANNUM, cleanCount) as string;
            }

            throw new ArgumentException("cleanCount");
        }

        public string CleanWaste(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.CLEANWASTE) as string;
        }

        public string CleanCapillary(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.CLEANCAP) as string;
        }

        /// <summary>
        /// Starts async a self test on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartSelfTest(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.STEST) as string;
        }

        /// <summary>
        /// Starts async a hardware self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartHardwareSelfTest(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.STESTHW) as string;
        }

        /// <summary>
        /// Starts async a software self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartSoftwareSelfTest(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.STESTSW) as string;
        }

        /// <summary>
        /// Starts async a pressure system self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartPressureSystemSelfTest(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.STESTPR) as string;
        }

        /// <summary>
        /// Returns async the last error occured on casy device.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The last error string occured on casy device</returns>
        public string GetError(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.GETERROR) as string;
        }

        /// <summary>
        /// Calibrates async the casy device with the passed calibration data
        /// </summary>
        /// <param name="calibrationData">Calibration data</param>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string Calibrate(ushort toDiameter, byte[] calibrationData, IProgress<string> progress)
        {
            if(calibrationData == null)
            {
                throw new ArgumentNullException("calibrationData");
            }
            return EnqueueCommand(progress, CasyCommand.CALIBTTC, calibrationData/*.Skip(17).ToArray()*/) as string;
        }

        public Tuple<ushort, ushort, uint> GetCalibrationVerifactionData(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.CHECKCALIB) as Tuple<ushort, ushort, uint>;
        }

        /// <summary>
        /// Starts async a measurement with the casy device (200 micro liter).
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string Measure200(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.START200) as string;
        }

        /// <summary>
        /// Starts async a measurement with the casy device (400 micro liter).
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string Measure400(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.START400) as string;
        }

        public async void Stop(IProgress<string> progress)
        {
            _currentCommandCanceled = true;

            _awaiter.Set();

            var wrapper = new CasyCommandWrapper()
            {
                Command = CasyCommand.STOP,
                Progress = progress,
                Awaiter = new ManualResetEvent(false),
                CommandParameter = 0,
                Timeout = 1000
            };

            await ExecuteCommandWrapper(wrapper);

            _currentAwaiter.Set();
            _awaiter.Set();
            _currentCommandCanceled = false;
        }

        /// <summary>
        /// Returns async last measurement result data from casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public byte[] GetBinaryData(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.BINDAT_CS) as byte[];
        }

        public Tuple<DateTime, uint> GetDateTime(IProgress<string> progress)
        {
            if (!(EnqueueCommand(progress, CasyCommand.GETDATETIME) is Tuple<string, uint> result)) return null;
            
            var year = int.Parse(result.Item1.Substring(0, 4));
            var month = int.Parse(result.Item1.Substring(4, 2).Trim());
            var day = int.Parse(result.Item1.Substring(6, 2).Trim());
            var hour = int.Parse(result.Item1.Substring(8, 2).Trim());
            var minute = int.Parse(result.Item1.Substring(10, 2).Trim());
            var second = int.Parse(result.Item1.Substring(12, 2).Trim());

            return new Tuple<DateTime, uint>(new DateTime(year, month, day, hour, minute, second), result.Item2);
        }

        public bool SetDateTime(DateTime dateTime, IProgress<string> progress)
        {
            var sYear = dateTime.Year.ToString();
            var sMonth = dateTime.Month.ToString().PadLeft(2, '0');
            var sDay = dateTime.Day.ToString().PadLeft(2, '0');
            var sHour = dateTime.Hour.ToString().PadLeft(2, '0');
            var sMinute = dateTime.Minute.ToString().PadLeft(2, '0');
            var sSecond = dateTime.Second.ToString().PadLeft(2, '0');

            var dateTimeString = sYear + sMonth + sDay + sHour + sMinute + sSecond;

            var length = 0;
            var data = new byte[18];
            using (var ms = new MemoryStream(data))
            using (var sw = new BinaryWriter(ms, Encoding.Default))
            {                
                var buf = Encoding.Default.GetBytes(dateTimeString);
                length += buf.Length;
                sw.Write(buf);

                sw.Seek(14, SeekOrigin.Begin);

                var checkSum = SwapHelper.SwapBytes(Calculations.CalcChecksum(data));
                sw.Write(BitConverter.GetBytes(checkSum));
            }

            var result = EnqueueCommand(progress, CasyCommand.SETDATETIME, data);
            return result is bool b && b;
        }

        public bool VerifyMasterPin(string masterPin, IProgress<string> progress)
        {
            if(string.IsNullOrEmpty(masterPin))
            {
                return false;
            }
            EnqueueCommand(progress, CasyCommand.MASTERPIN, masterPin);
            return true;
        }

        public Tuple<byte[], uint> GetHeader(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.GETHEADER) as Tuple<byte[], uint>;
        }

        public uint RequestLastChecksum(IProgress<string> progress)
        {
            return (uint) EnqueueCommand(progress, CasyCommand.REQ_CS);
        }

        public string CreateTestPattern(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.TESTPATTERN) as string;
        }

        public string Dry(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.DRY) as string;
        }

        public byte StartLEDTest(IProgress<string> progress)
        {
            var result = EnqueueCommand(progress, CasyCommand.LEDTEST);
            return Encoding.Default.GetBytes(result as string)[0];
        }

        public bool PerformBlow(IProgress<string> progress)
        {
            return (bool) EnqueueCommand(progress, CasyCommand.BLOW1);
        }

        public bool PerformSuck(IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.SUCK1);
        }

        public bool SetVacuumVentilState(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VVAC, state ? 1 : 0);
        }

        public bool SetPumpEngineState(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.MOT, state ? 1 : 0);
        }

        public bool SetCapillaryRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.UKAP, state ? 1 : 0);
        }

        public bool SetMeasValveRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VMES, state ? 1 : 0);
        }

        public bool SetWasteValveRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VWST, state ? 1 : 0);
        }

        public byte GetValveState(IProgress<string> progress)
        {
            var result = EnqueueCommand(progress, CasyCommand.GETVENTSTATUS);
            return Encoding.Default.GetBytes(result as string)[0];
        }

        public byte[] GetStatistik(IProgress<string> progress)
        {
            return (byte[])EnqueueCommand(progress, CasyCommand.STATISTIK);
        }

        public bool SetCleanValveRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VCLN, state ? 1 : 0);
        }

        public bool SetBlowValveRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VBLO, state ? 1 : 0);
        }

        public bool SetSuckValveRelayVoltage(bool state, IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.VSUC, state ? 1 : 0);
        }

        public bool SetCapillaryVoltage(int value, IProgress<string> progress)
        {
            if(value < 220 || value > 255)
            {
                return false;
            }
            return (bool)EnqueueCommand(progress, CasyCommand.SETCAPVLT, value);
        }

        public double GetCapillaryVoltage(IProgress<string> progress)
        {
            if (!(EnqueueCommand(progress, CasyCommand.GETCAPVLT) is string response)) return -1d;
            
            response = response.Replace(",", ".");

            double.TryParse(response, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);

            return result;
        }

        public double GetPressure(IProgress<string> progress)
        {
            if (!(EnqueueCommand(progress, CasyCommand.GETPRESSURE) is string response)) return -1d;
            
            response = response.Replace(",", ".");

            double.TryParse(response, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);

            return result;
        }

        public bool ClearErrorBytes(IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.CLEARERROR);
        }

        public bool ResetStatistic(IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.INITSTAT);
        }

        public bool ResetCalibration(IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.INITCAL);
        }

        public string CheckRisetime(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.RISETIME, timeout: 900000) as string;
        }

        public string CheckTightness(IProgress<string> progress)
        {
            return EnqueueCommand(progress, CasyCommand.TIGHTNESS, timeout: 240000) as string;
        }

        /*
         *BOOL  OperateClass::SwitchToTTC()
{  // switch hardware to type TTC

  if (!SendPassword()) return FALSE;
  if (!Comm()->WriteAndCheck(CA_SWITCH,0))
  {
    ReportCommError("SwitchToTTC");
    return FALSE;
  }
  return TRUE;
}
         */

        public bool SendInfo(IProgress<string> progress)
        {
            return (bool) EnqueueCommand(progress, CasyCommand.INFO, commandParameter: "INFO ON");
        }

        public bool SendSwitchToTTC(IProgress<string> progress)
        {
            return (bool)EnqueueCommand(progress, CasyCommand.SWITCH, commandParameter: 0);
        }

        private object EnqueueCommand(IProgress<string> progress, CasyCommand command, object commandParameter = null, int timeout = 60000)
        {
            _currentAwaiter = new ManualResetEvent(false);
            var wrapper = new CasyCommandWrapper()
            {
                Command = command,
                Progress = progress,
                Awaiter = _currentAwaiter,
                CommandParameter = commandParameter ?? 0,
                Timeout = timeout
            };

            _commandQueue.Add(wrapper);

            return _currentAwaiter.WaitOne(timeout) ? wrapper.Result : null;
        }

        private bool ConnectCasyDriver(string portname)
        {
            if(_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            if (_serialPort == null) return false;

            try
            {
                _serialPort.PortName = portname;
                _serialPort.Open();
            
                var answer = GetSerialNumber(new Progress<string>());

                if (_serialPort.IsOpen && answer != null && !string.IsNullOrEmpty(answer.Item1))
                {
                    //if(answer.Item1.StartsWith("TT-"))
                    //{
                    //  var task = SwitchToTTC(new Progress<string>());
                    //                    task.Start();
                    //                  task.Wait();
                    //            }
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            var actualLength = _serialPort.BytesToRead;
            var received = new byte[actualLength];
            _serialPort.Read(received, 0, actualLength);

            lock (_lock)
            {
                foreach (var b in received)
                {
                    _receivedBytes.Enqueue(b);
                }
            }

            ProcessCurrent();
        }

        private void CleanUp()
        {
            try
            {
                lock (_lock)
                {
                    _receivedBytes.Clear();
                }

                _awaiter?.Reset();
                _responseResult = null;
                _responseSuccess = false;

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                }

                if (_executingCommand == null) return;
                
                _executingCommand.Progress?.Report("CASYCOMMAND_PROGRESS_CLEANUP");
                _executingCommand = null;
            }
            catch(Exception ex)
            {
                _logger.Error(LogCategory.Instrument, "Exception while COM Clean Up", () => CleanUp(), ex);
            }
        }

        //private async Task<object> StartCommand(CasyCommand command, int param = 0, int timeout = 60000)
        private async void ConsumeCasyCommandWorker()
        {
            try
            {
                foreach (var casyCommand in _commandQueue.GetConsumingEnumerable(_tokenSource.Token))
                {
                    if (!_currentCommandCanceled)
                    {
                        //CleanUp();
                        await ExecuteCommandWrapper(casyCommand);
                    }
                    else
                    {
                        casyCommand.Awaiter.Set();
                    }

                    if (_commandQueue.Count == 0)
                    {
                        _currentCommandCanceled = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _commandQueue.Dispose();
                _commandQueue = new BlockingCollection<CasyCommandWrapper>();

            }
            catch (UnauthorizedAccessException)
            {
                // perform your cleanup
                _commandQueue.Dispose();
                _commandQueue = new BlockingCollection<CasyCommandWrapper>();

                // rethrow exception so caller knows you’ve canceled.
                // DON’T “throw ex;” because that stomps on 
                // the Exception.StackTrace property.
                //throw;
            }
        }

        private async Task ExecuteCommandWrapper(CasyCommandWrapper casyCommand)
        {
            casyCommand.Progress?.Report("CASYCOMMAND_PROGRESS_SENDING");
            if (_serialPort.IsOpen)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                _executingCommand = casyCommand;
                var intCommand = (int)casyCommand.Command;
                var stringCommand = Enum.GetName(typeof(CasyCommand), casyCommand.Command);
                _commandString = string.Empty;
                _responseString = string.Empty;
                _errorString = string.Empty;

                switch (casyCommand.Command)
                {
                    case CasyCommand.CLEAN:
                    case CasyCommand.LEDTEST:
                    case CasyCommand.BLOW1:
                    case CasyCommand.SUCK1:
                    case CasyCommand.STEST:
                    case CasyCommand.GETVENTSTATUS:
                    case CasyCommand.STATISTIK:
                    case CasyCommand.GETERROR:
                    case CasyCommand.START200:
                    case CasyCommand.START400:
                    case CasyCommand.GETDATETIME:
                    case CasyCommand.GETSERIALNO:
                    case CasyCommand.CHECKCALIB:
                    case CasyCommand.GETHEADER:
                    case CasyCommand.REQ_CS:
                    case CasyCommand.TESTPATTERN:
                    case CasyCommand.STESTHW:
                    case CasyCommand.STESTSW:
                    case CasyCommand.STESTPR:
                    case CasyCommand.DRY:
                    case CasyCommand.CLEANWASTE:
                    case CasyCommand.CLEANCAP:
                    case CasyCommand.CLEARERROR:
                    case CasyCommand.INITSTAT:
                    case CasyCommand.INITCAL:
                    case CasyCommand.SETDATETIME:
                    case CasyCommand.RISETIME:
                    case CasyCommand.TIGHTNESS:
                    case CasyCommand.STOP:
                        _commandString = $"!{intCommand:X2}\r";
                        _commandStringPrint = $"{stringCommand}";
                        _responseString = $"!{intCommand:X2} OK";
                        _errorString = $"!{intCommand:X2} ??";
                        break;
                    case CasyCommand.BINDAT:
                    case CasyCommand.BINDAT_CS:
                    case CasyCommand.CLEANNUM:
                    case CasyCommand.VVAC:
                    case CasyCommand.MOT:
                    case CasyCommand.VMES:
                    case CasyCommand.VWST:
                    case CasyCommand.VCLN:
                    case CasyCommand.VBLO:
                    case CasyCommand.VSUC:
                    case CasyCommand.SWITCH:
                    case CasyCommand.SETCAPVLT:
                        _commandString = $"!{intCommand:X2}#{(int) casyCommand.CommandParameter}\r";
                        _commandStringPrint = $"{stringCommand}#{(int) casyCommand.CommandParameter}";
                        _responseString = $"!{intCommand:X2}#{(int) casyCommand.CommandParameter} OK";
                        _errorString = $"!{intCommand:X2}#{(int) casyCommand.CommandParameter} ??";
                        break;
                    case CasyCommand.CALIBTTC:
                    case CasyCommand.GETPRESSURE:
                    case CasyCommand.GETCAPVLT:
                        _commandString = $"{stringCommand}\r";
                        _commandStringPrint = $"{stringCommand}";
                        _responseString = $"{stringCommand} OK";
                        _errorString = $"{stringCommand} ??";
                        break;
                    case CasyCommand.MASTERPIN:
                        _commandString = $"MASTERPIN#{casyCommand.CommandParameter}\r";
                        _commandStringPrint = $"MASTERPIN#{casyCommand.CommandParameter}";
                        _responseString = $"MASTERPIN#{casyCommand.CommandParameter} OK";
                        _errorString = $"MASTERPIN#{casyCommand.CommandParameter} ??";
                        break;
                    case CasyCommand.SETSERIALNO:
                        _commandString = $"!{intCommand:X2}\r";
                        _commandStringPrint = $"{stringCommand}";
                        _responseString = $"!{intCommand:X2} OK";
                        _errorString = $"!{intCommand:X2} ??";
                        break;
                    case CasyCommand.INFO:
                        _commandString = $"{casyCommand.CommandParameter as string}\r";
                        _commandStringPrint = $"{casyCommand.CommandParameter as string}";
                        _responseString = $"{casyCommand.CommandParameter as string} OK";
                        _errorString = $"{casyCommand.CommandParameter as string} ??";
                        break;
                    case CasyCommand.UKAP:
                        _commandString = $"{stringCommand}#{(int) casyCommand.CommandParameter}\r";
                        _commandStringPrint = $"{stringCommand}#{(int) casyCommand.CommandParameter}";
                        _responseString = $"{stringCommand}#{(int) casyCommand.CommandParameter} OK";
                        _errorString = $"{stringCommand}#{(int) casyCommand.CommandParameter} ??";
                        break;
                }

                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Executing command '{_commandStringPrint}'", () => ExecuteCommandWrapper(casyCommand));

                _awaiter = new ManualResetEvent(false);

                _serialPort.Write(_commandString);

                await Task.Factory.StartNew(() =>
                {
                    //object result = null;
                    if (!_awaiter.WaitOne(casyCommand.Timeout)) return;
                    casyCommand.Progress?.Report("CASYCOMMAND_PROGRESS_RECEIVED");
                    casyCommand.Result = _responseResult;
                    casyCommand.Awaiter.Set();
                    //return result;
                }).ContinueWith((t) => CleanUp());
            }
        }

        private void ProcessCurrent()
        {
            while (true)
            {
                byte[] buffer;
                lock (_lock)
                { 
                    var count = _receivedBytes.Count;
                    buffer = _receivedBytes.Take(count).ToArray();
                }
                var current = Encoding.Default.GetString(buffer);

                var index = current.IndexOf("\n\r", StringComparison.Ordinal);

                if (_responseSuccess)
                {
                    string answer;
                    switch (_executingCommand.Command)
                    {
                        // Serial Number
                        case CasyCommand.GETSERIALNO:
                            answer = current.Replace("\"53,", "");

                            if (answer.Length >= 19)
                            {
                                var serialNo = answer.Substring(0, 15);
                                serialNo = serialNo.Replace(" ", "");
                                var checksum = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, buffer.Length - 4));
                                _responseResult = new Tuple<string, uint>(serialNo, checksum);

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{serialNo}'", () => ProcessCurrent());

                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.GETDATETIME:
                            answer = current.Replace($"\"{((int) _executingCommand.Command):X2},", "");
                            answer = new string(answer.SkipWhile(c => c == '\0').ToArray());

                            if (answer.Length >= 18)
                            {
                                var dateTime = answer.Substring(0, 14);
                                dateTime = dateTime.Replace("\0", "");
                                var checksum = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, buffer.Length - 4));
                                _responseResult = new Tuple<string, uint>(dateTime, checksum);

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{dateTime}'", () => ProcessCurrent());

                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.SETDATETIME:
                        case CasyCommand.SETSERIALNO:
                            answer = current.Replace($"\"{((int) _executingCommand.Command):X2},", "");
                            if (answer.Length == 4)
                            {
                                _responseResult = true;
                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.CHECKCALIB:
                            answer = current.Replace("\"56,", "");
                            if (answer.Length >= 10)
                            {
                                var data = buffer.Skip(4).ToArray();
                                var checksum = BitConverter.ToUInt32(data.Skip(4).Take(4).ToArray(), 0);
                                var cappilarySize = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);
                                var toDiameter = BitConverter.ToUInt16(data.Skip(2).Take(2).ToArray(), 0);
                                _responseResult = new Tuple<ushort, ushort, uint>(cappilarySize, toDiameter, checksum);

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{cappilarySize}; {toDiameter}'", () => ProcessCurrent());

                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.GETHEADER:
                            answer = current.Replace("\"5D,", "");
                            if (answer.Length >= 162)
                            {
                                var data = buffer.Skip(4).ToArray();
                                var checksum = BitConverter.ToUInt32(data, data.Length - 4);

                                _responseResult = new Tuple<byte[], uint>(data.Take(158).ToArray(), checksum);

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successful.", () => ProcessCurrent());
                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.REQ_CS:
                            answer = current.Replace("\"61,", "");
                            if (answer.Length >= 4)
                            {
                                var data = buffer.Skip(4).ToArray();
                                _responseResult = BitConverter.ToUInt32(data, 0);

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{((uint) _responseResult).ToString()}'", () => ProcessCurrent());

                                _awaiter.Set();
                            }

                            break;
                        case CasyCommand.LEDTEST:
                        case CasyCommand.GETVENTSTATUS:
                        case CasyCommand.GETPRESSURE:
                        case CasyCommand.GETCAPVLT:
                            if (index > -1)
                            {
                                current = current.Replace("\n\r", "");
                                _responseResult = current;

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{current}'", () => ProcessCurrent());
                                _awaiter.Set();
                            }

                            break;
                        // Clean
                        case CasyCommand.CLEAN:
                        //case CasyCommand.CLEANNUM:
                        // CALIBTTC
                        case CasyCommand.CALIBTTC:
                        // Self Test
                        case CasyCommand.STEST:
                        case CasyCommand.STESTHW:
                        case CasyCommand.STESTSW:
                        case CasyCommand.STESTPR:
                        // START200
                        case CasyCommand.START200:
                        // START400
                        case CasyCommand.START400:
                        case CasyCommand.TESTPATTERN:
                        case CasyCommand.DRY:
                        case CasyCommand.CLEANCAP:
                        case CasyCommand.CLEANWASTE:
                        case CasyCommand.TIGHTNESS:
                        case CasyCommand.GETERROR:
                            if (index > -1)
                            {
                                AnalyzeStandardResponse(current, 12);
                            }

                            break;
                        case CasyCommand.RISETIME:
                            if (index > -1)
                            {
                                if (current.Contains("\"01,"))
                                {
                                    current = current.Replace("\"01,", "");

                                    var split = current.Split(',');
                                    if (split.Take(3).Any(s => s != "0000"))
                                    {
                                        _responseResult = current;
                                        _awaiter.Set();
                                        //AnalyzeStandardResponse(current, 12, CasyCommand.CLEAN);
                                    }
                                    else
                                    {
                                        lock (_lock)
                                        {
                                            _receivedBytes.Clear();
                                        }
                                    }
                                }
                                else
                                {
                                    current = current.Replace($"\"{((int) _executingCommand.Command):X2},", "");
                                    AnalyzeStandardResponse(current, 13);
                                }
                            }

                            break;
                        case CasyCommand.CLEANNUM:
                            if (index > -1)
                            {
                                AnalyzeStandardResponse(current, 12, CasyCommand.CLEAN);
                            }

                            break;
                        // BINDAT#0
                        case CasyCommand.BINDAT:
                            answer = current.Replace("\"32,", "");

                            if (answer.Length > 0)
                            {
                                var measureData = buffer.Skip(4).ToArray();

                                if (_serialPort.BytesToRead == 0 && measureData.Length >= 2060)
                                {
                                    _responseResult = measureData;

                                    _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successful", () => ProcessCurrent());
                                    _awaiter.Set();
                                }
                            }

                            break;
                        case CasyCommand.BINDAT_CS:
                            answer = current.Replace("\"55,", "");

                            if (answer.Length > 0)
                            {
                                var measureData = buffer.Skip(4).ToArray();

                                if (_serialPort.BytesToRead == 0 && measureData.Length >= 2064)
                                {
                                    _responseResult = measureData;

                                    _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successful", () => ProcessCurrent());
                                    _awaiter.Set();
                                }
                            }

                            break;
                        case CasyCommand.STATISTIK:
                            answer = current.Replace("\"20,", "");
                            if (answer.Length > 0)
                            {
                                var statistikData = buffer.Skip(4).ToArray();

                                if (_serialPort.BytesToRead == 0 && statistikData.Length >= 2924)
                                {
                                    _responseResult = statistikData;

                                    _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successful", () => ProcessCurrent());
                                    _awaiter.Set();
                                }
                            }

                            break;
                    }
                }
                else if (index > -1)
                {
                    var resp = current.Substring(0, index);
                    lock (_lock)
                    {
                        for (var i = 0; i < index + 2; i++)
                        {
                            _receivedBytes.Dequeue();
                        }
                    }

                    if (resp == _errorString)
                    {
                        _logger.Debug(LogCategory.Instrument, $"CASY Communication: Executing command '{_commandStringPrint}' failed with response '{resp}'", () => ProcessCurrent());

                        switch (_executingCommand.Command)
                        {
                            case CasyCommand.MASTERPIN:
                            case CasyCommand.BLOW1:
                            case CasyCommand.SUCK1:
                            case CasyCommand.VVAC:
                            case CasyCommand.MOT:
                            case CasyCommand.UKAP:
                            case CasyCommand.VMES:
                            case CasyCommand.VWST:
                            case CasyCommand.VCLN:
                            case CasyCommand.VBLO:
                            case CasyCommand.VSUC:
                            case CasyCommand.SETCAPVLT:
                            case CasyCommand.CLEARERROR:
                            case CasyCommand.INITSTAT:
                            case CasyCommand.INITCAL:
                            case CasyCommand.INFO:
                            case CasyCommand.SWITCH:
                            case CasyCommand.STOP:
                                _responseResult = false;
                                break;
                            default:
                                _responseResult = "Invalid command: " + resp;
                                break;
                        }

                        _awaiter.Set();
                    }
                    else if (resp == _responseString || resp == _responseString.Replace(" ", ""))
                    {
                        if (_executingCommand == null) return;

                        _responseSuccess = true;
                        _executingCommand.Progress?.Report("CASYCOMMAND_PROGRESS_CONFIRMED");

                        _logger.Debug(LogCategory.Instrument, $"CASY Communication: Executing command '{_commandStringPrint}' succeeded with response '{resp}'", () => ProcessCurrent());

                        switch (_executingCommand.Command)
                        {
                            case CasyCommand.SETDATETIME:
                                //Thread.Sleep(500);
                                lock (_lock)
                                {
                                    _receivedBytes.Clear();
                                }

                                //current = string.Empty;
                                var parameter = (byte[]) _executingCommand.CommandParameter;
                                _serialPort.Write(parameter, 0, parameter.Length);
                                //_serialPort.Write("\n\r");
                                //_serialPort.Write(parameter, 0, parameter.Length);
                                break;
                            case CasyCommand.CALIBTTC:
                                lock (_lock)
                                {
                                    _receivedBytes.Clear();
                                }

                                var data = (byte[]) _executingCommand.CommandParameter;
                                _serialPort.Write(data, 0, data.Length);
                                break;
                            case CasyCommand.SETSERIALNO:
                                lock (_lock)
                                {
                                    _receivedBytes.Clear();
                                }

                                //current = string.Empty;
                                var (_, item2) = (Tuple<string, byte[]>) _executingCommand.CommandParameter;
                                _serialPort.Write(item2, 0, item2.Length);
                                //_serialPort.Write("\n\r");
                                break;
                        }

                        switch (_executingCommand.Command)
                        {
                            case CasyCommand.MASTERPIN:
                            case CasyCommand.BLOW1:
                            case CasyCommand.SUCK1:
                            case CasyCommand.VVAC:
                            case CasyCommand.MOT:
                            case CasyCommand.UKAP:
                            case CasyCommand.VMES:
                            case CasyCommand.VWST:
                            case CasyCommand.VCLN:
                            case CasyCommand.VBLO:
                            case CasyCommand.VSUC:
                            case CasyCommand.SETCAPVLT:
                            case CasyCommand.CLEARERROR:
                            case CasyCommand.INITSTAT:
                            case CasyCommand.INITCAL:
                            case CasyCommand.INFO:
                            case CasyCommand.SWITCH:
                            case CasyCommand.STOP:
                                //case CasyCommand.SETDATETIME:
                                _responseResult = true;

                                _logger.Debug(LogCategory.Instrument, $"CASY Communication: Command '{_commandStringPrint}' successful", () => ProcessCurrent());
                                _awaiter.Set();
                                break;
                        }

                        current = current.Replace(resp, "");
                        current = current.Replace("\n\r", "");
                        if (string.IsNullOrEmpty(current)) return;
                        Thread.Sleep(50);
                        continue;
                    }
                }

                break;
            }
        }

        private void AnalyzeStandardResponse(string response, int chunkCount, CasyCommand? alternativeResponse = null)
        {
            response = response.Replace("\n\r", "");

            var chunks = response.Split(',');
            if (chunks.Length != chunkCount) return;

            var toSearch = $"\"{((int) _executingCommand.Command):X2},";

            var index = response.IndexOf(toSearch, StringComparison.Ordinal);
            if(index < 0 && alternativeResponse.HasValue)
            {
                toSearch = $"\"{((int) alternativeResponse):X2},";
                index = response.IndexOf(toSearch, StringComparison.Ordinal);
            }

            response = new string(response.Skip(index).ToArray());
            response = response.Replace(toSearch, "");

            _responseResult = response;

            _logger.Debug(LogCategory.Instrument,
                $"CASY Communication: Command '{_commandStringPrint}' successfully returned value: '{response}'", () => ProcessCurrent());

            _awaiter.Set();
        }

        public void OnImportsSatisfied()
        {
            _serialPort.BaudRate = 921600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            //_serialPort.RtsEnable = true;

            _serialPort.DataReceived += OnDataReceived;
        }
    }
}
