using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;
using System.ComponentModel;
using OLS.Casy.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OLS.Casy.IO.Api;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System;

namespace OLS.Casy.Com.Simulation.ViewModels
{
    /// <summary>
    /// ViewModel for the casy serial port device simulator view.
    /// Implements <see cref="IChildViewOwner"/> becuase the view is a child view
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(CasySerialPortSimulatorViewModel))]
    public class CasySerialPortSimulatorViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly CasySerialPortDriverSimulator _casySerialPortDriverSimulator;
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IBinaryImportExportProvider _binaryImportExportProvider;
        private readonly ICRFImportExportProvider _crfImportExportProvider;
        private readonly ITTImportExportProvider _ttImportExportProvider;

        private string _lastError;
        private string _cleanError;
        private string _measureError;
        private bool _useCorrectChecksum;
        private uint _aboveCalibrationLimitCount;
        private uint _belowCalibrationLimitCount;
        private uint _belowMeasureLimtCount;
        private string _calibrationError;
        private string _hardwareSelfTestError;
        private double _measureDelay;
        private string _pressureSelfTestError;
        private string _selfTestError;
        private string _serialNumber;
        private string _softwareSelfTestError;
        private int _toDiameter;
        private int _capillarySize;
        private string _masterPin;
        private string _dryError;
        private bool _isLightBarrierLED;
        private bool _isGreenLED;
        private bool _isFirstRedLED;
        private bool _isSecondRedLED;
        private bool _vacuumVentilState;
        private bool _pumpEngineState;
        private bool _capillaryRelayVoltage;
        private bool _measValveRelayVoltage;
        private bool _wasteValveRelayVoltage;
        private bool _cleanValveRelayVoltage;
        private bool _blowValveRelayVoltage;
        private bool _suckValveRelayVoltage;
        private double _capillaryVoltage;
        private double _currentPressure;

        private ObservableCollection<Tuple<MeasureResult,string>> _avaliableMeasureResults;
        private MeasureResult _selectedMeasureResult;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="casySerialPortDriverSimulator">Implementation of <see cref="CasySerialPortDriverSimulator"/></param>
        [ImportingConstructor]
        public CasySerialPortSimulatorViewModel(CasySerialPortDriverSimulator casySerialPortDriverSimulator,
            IFileSystemStorageService fileSystemStorageService,
            IBinaryImportExportProvider binaryImportExportProvider,
            ICRFImportExportProvider crfImportExportProvider,
            ITTImportExportProvider ttImportExportProvider)
        {
            this._casySerialPortDriverSimulator = casySerialPortDriverSimulator;
            this._fileSystemStorageService = fileSystemStorageService;
            this._binaryImportExportProvider = binaryImportExportProvider;
            this._crfImportExportProvider = crfImportExportProvider;
            this._ttImportExportProvider = ttImportExportProvider;

            this._avaliableMeasureResults = new ObservableCollection<Tuple<MeasureResult, string>>();
        }

        public void SetInputData(object data)
        {
        }

        public ObservableCollection<Tuple<MeasureResult, string>> AvailableMeasureResults
        {
            get { return _avaliableMeasureResults; }
        }

        public MeasureResult SelectedMeasureResult
        {
            get { return _selectedMeasureResult; }
            set
            {
                if (value != _selectedMeasureResult)
                {
                    this._selectedMeasureResult = value;
                    NotifyOfPropertyChange();
                    this._casySerialPortDriverSimulator.SelectedMeasureResult = value;
                }
            }
        }

        /// <summary>
        /// Property for the current measure error
        /// </summary>
        public string MeasureError
        {
            get { return _measureError; }
            set
            {
                this._measureError = value;
                this._casySerialPortDriverSimulator.CurrentMeasureError = this._measureError;
                NotifyOfPropertyChange();

                this.LastError = value;
            }
        }

        /// <summary>
        /// Property for the indicator whether the measurement shall return a correct checksum or not
        /// </summary>
        public bool UseCorrectChecksum
        {
            get { return _useCorrectChecksum; }
            set
            {
                this._useCorrectChecksum = value;
                this._casySerialPortDriverSimulator.UseCorrectChecksum = this._useCorrectChecksum;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current clean error
        /// </summary>
        public string CleanError
        {
            get { return _cleanError; }
            set
            {
                this._cleanError = value;
                this._casySerialPortDriverSimulator.CurrentCleanError = this._cleanError;
                NotifyOfPropertyChange();

                this.LastError = value;
            }
        }

        /// <summary>
        /// Property for the last error detected in device
        /// </summary>
        public string LastError
        {
            get { return _lastError; }
            set
            {
                this._lastError = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for counts above calibration limit
        /// </summary>
        public uint AboveCalibrationLimitCount
        {
            get { return _aboveCalibrationLimitCount; }
            set
            {
                this._aboveCalibrationLimitCount = value;
                this._casySerialPortDriverSimulator.CurrentAboveCalibrationLimitCount = this._aboveCalibrationLimitCount;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for counts below calibration limit
        /// </summary>
        public uint BelowCalibrationLimitCount
        {
            get { return _belowCalibrationLimitCount; }
            set
            {
                this._belowCalibrationLimitCount = value;
                this._casySerialPortDriverSimulator.CurrentBelowCalibrationLimitCount = this._belowCalibrationLimitCount;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for counts below measure limit
        /// </summary>
        public uint BelowMeasureLimtCount
        {
            get { return _belowMeasureLimtCount; }
            set
            {
                this._belowMeasureLimtCount = value;
                this._casySerialPortDriverSimulator.CurrentBelowMeasureLimtCount = this._belowMeasureLimtCount;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current calibration error
        /// </summary>
        public string CalibrationError
        {
            get { return _calibrationError; }
            set
            {
                this._calibrationError = value;
                this._casySerialPortDriverSimulator.CurrentCalibrationError = this._calibrationError;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current hardware self test error
        /// </summary>
        public string HardwareSelfTestError
        {
            get { return _hardwareSelfTestError; }
            set
            {
                this._hardwareSelfTestError = value;
                this._casySerialPortDriverSimulator.CurrentHardwareSelfTestError = this._hardwareSelfTestError;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the delay of the measurement process
        /// </summary>
        public double MeasureDelay
        {
            get { return _measureDelay; }
            set
            {
                this._measureDelay = value;
                this._casySerialPortDriverSimulator.CurrentMeasureDelay = this._measureDelay;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current pressure system self test error
        /// </summary>
        public string PressureSelfTestError
        {
            get { return _pressureSelfTestError; }
            set
            {
                this._pressureSelfTestError = value;
                this._casySerialPortDriverSimulator.CurrentPressureSelfTestError = this._pressureSelfTestError;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current self test error
        /// </summary>
        public string SelfTestError
        {
            get { return _selfTestError; }
            set
            {
                this._selfTestError = value;
                this._casySerialPortDriverSimulator.CurrentSelfTestError = this._selfTestError;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current serial number
        /// </summary>
        public string SerialNumber
        {
            get { return _serialNumber; }
            set
            {
                this._serialNumber = value;
                this._casySerialPortDriverSimulator.CurrentSerialNumber = this._serialNumber;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Property for the current software self test error
        /// </summary>
        public string SoftwareSelfTestError
        {
            get { return _softwareSelfTestError; }
            set
            {
                this._softwareSelfTestError = value;
                this._casySerialPortDriverSimulator.CurrentSoftwareSelfTestError = this._softwareSelfTestError;
                NotifyOfPropertyChange();
            }
        }

        public int ToDiameter
        {
            get { return _toDiameter; }
            set
            {
                if(value != _toDiameter)
                {
                    this._toDiameter = value;
                    this._casySerialPortDriverSimulator.ToDiameter = (ushort) this._toDiameter;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int CapillarySize
        {
            get { return _capillarySize; }
            set
            {
                if (value != _capillarySize)
                {
                    this._capillarySize = value;
                    this._casySerialPortDriverSimulator.CapillarySize = (ushort) this._capillarySize;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string MasterPin
        {
            get { return _masterPin; }
            set
            {
                if (value != _masterPin)
                {
                    this._masterPin = value;
                    this._casySerialPortDriverSimulator.CurrentMasterPin = this._masterPin;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string DryError
        {
            get { return _dryError; }
            set
            {
                if (value != _dryError)
                {
                    this._dryError = value;
                    this._casySerialPortDriverSimulator.CurrentDryError = this._dryError;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsLightBarrierLED
        {
            get { return _isLightBarrierLED; }
            set
            {
                if (value != _isLightBarrierLED)
                {
                    this._isLightBarrierLED = value;
                    this._casySerialPortDriverSimulator.IsLightBarrierLED = this._isLightBarrierLED;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsGreenLED
        {
            get { return _isGreenLED; }
            set
            {
                if (value != _isGreenLED)
                {
                    this._isGreenLED = value;
                    this._casySerialPortDriverSimulator.IsGreenLED = this._isGreenLED;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsFirstRedLED
        {
            get { return _isFirstRedLED; }
            set
            {
                if (value != _isFirstRedLED)
                {
                    this._isFirstRedLED = value;
                    this._casySerialPortDriverSimulator.IsFirstRedLED = this._isFirstRedLED;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsSecondRedLED
        {
            get { return _isSecondRedLED; }
            set
            {
                if (value != _isSecondRedLED)
                {
                    this._isSecondRedLED = value;
                    this._casySerialPortDriverSimulator.IsSecondRedLED = this._isSecondRedLED;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool VacuumVentilState
        {
            get { return _vacuumVentilState; }
            set
            {
                if (value != _vacuumVentilState)
                {
                    this._vacuumVentilState = value;
                    this._casySerialPortDriverSimulator.VacuumVentilState = this._vacuumVentilState;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool PumpEngineState
        {
            get { return _pumpEngineState; }
            set
            {
                if (value != _pumpEngineState)
                {
                    this._pumpEngineState = value;
                    this._casySerialPortDriverSimulator.PumpEngineState = this._pumpEngineState;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool CapillaryRelayVoltage
        {
            get { return _capillaryRelayVoltage; }
            set
            {
                if (value != _capillaryRelayVoltage)
                {
                    this._capillaryRelayVoltage = value;
                    this._casySerialPortDriverSimulator.CapillaryRelayVoltage = this._capillaryRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool MeasValveRelayVoltage
        {
            get { return _measValveRelayVoltage; }
            set
            {
                if (value != _measValveRelayVoltage)
                {
                    this._measValveRelayVoltage = value;
                    this._casySerialPortDriverSimulator.MeasValveRelayVoltage = this._measValveRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool WasteValveRelayVoltage
        {
            get { return _wasteValveRelayVoltage; }
            set
            {
                if (value != _wasteValveRelayVoltage)
                {
                    this._wasteValveRelayVoltage = value;
                    this._casySerialPortDriverSimulator.WasteValveRelayVoltage = this._wasteValveRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }
        public bool CleanValveRelayVoltage
        {
            get { return _cleanValveRelayVoltage; }
            set
            {
                if (value != _cleanValveRelayVoltage)
                {
                    this._cleanValveRelayVoltage = value;
                    this._casySerialPortDriverSimulator.CleanValveRelayVoltage = this._cleanValveRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool BlowValveRelayVoltage
        {
            get { return _blowValveRelayVoltage; }
            set
            {
                if (value != _blowValveRelayVoltage)
                {
                    this._blowValveRelayVoltage = value;
                    this._casySerialPortDriverSimulator.BlowValveRelayVoltage = this._blowValveRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool SuckValveRelayVoltage
        {
            get { return _suckValveRelayVoltage; }
            set
            {
                if (value != _suckValveRelayVoltage)
                {
                    this._suckValveRelayVoltage = value;
                    this._casySerialPortDriverSimulator.SuckValveRelayVoltage = this._suckValveRelayVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double CapillaryVoltage
        {
            get { return _capillaryVoltage; }
            set
            {
                if (value != _capillaryVoltage)
                {
                    this._capillaryVoltage = value;
                    this._casySerialPortDriverSimulator.CapillaryVoltage = this._capillaryVoltage;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double CurrentPressure
        {
            get { return _currentPressure; }
            set
            {
                if (value != _currentPressure)
                {
                    this._currentPressure = value;
                    this._casySerialPortDriverSimulator.CurrentPressure = this._currentPressure;
                    NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        /// Method will be called when all MEF imports are fullfilled
        /// </summary>
        public async void OnImportsSatisfied()
        {
            this.LastError = this._casySerialPortDriverSimulator.CurrentError;
            this.MeasureError = this._casySerialPortDriverSimulator.CurrentMeasureError;
            this.UseCorrectChecksum = this._casySerialPortDriverSimulator.UseCorrectChecksum;
            this.CleanError = this._casySerialPortDriverSimulator.CurrentCleanError;
            this.AboveCalibrationLimitCount = this._casySerialPortDriverSimulator.CurrentAboveCalibrationLimitCount;
            this.BelowCalibrationLimitCount = this._casySerialPortDriverSimulator.CurrentBelowCalibrationLimitCount;
            this.BelowMeasureLimtCount = this._casySerialPortDriverSimulator.CurrentBelowMeasureLimtCount;
            this.CalibrationError = this._casySerialPortDriverSimulator.CurrentCalibrationError;
            this.HardwareSelfTestError = this._casySerialPortDriverSimulator.CurrentHardwareSelfTestError;
            this.MeasureDelay = this._casySerialPortDriverSimulator.CurrentMeasureDelay;
            this.PressureSelfTestError = this._casySerialPortDriverSimulator.CurrentPressureSelfTestError;
            this.SelfTestError = this._casySerialPortDriverSimulator.CurrentSelfTestError;
            this.SerialNumber = this._casySerialPortDriverSimulator.CurrentSerialNumber;
            this.SoftwareSelfTestError = this._casySerialPortDriverSimulator.CurrentSoftwareSelfTestError;
            this.DryError = this._casySerialPortDriverSimulator.CurrentDryError;
            this.MasterPin = this._casySerialPortDriverSimulator.CurrentMasterPin;

            this._casySerialPortDriverSimulator.PropertyChanged += OnSimulatorPropertyChanged;

            var simDataDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            simDataDirectory = Path.Combine(simDataDirectory, "Simulator");

            if (Directory.Exists(simDataDirectory))
            {
                var simulationFiles = Directory.GetFiles(simDataDirectory);

                foreach (var simulationFile in simulationFiles)
                {
                    FileInfo fileInfo = new FileInfo(simulationFile);

                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".csy":
                            await this.OpenBinaryFile(fileInfo);
                            break;
                        case ".crf":
                            await this.ImportCRFFile(fileInfo);
                            break;
                        case ".tt":
                            await this.ImportTTFile(fileInfo, false);
                            break;
                        case ".xlsx":
                            await this.ImportTTFile(fileInfo, true);
                            break;
                    }
                }
            }
        }

        private async Task OpenBinaryFile(FileInfo fileInfo)
        {
            var measureResults = await _binaryImportExportProvider.ImportMeasureResultsAsync(fileInfo.FullName);
            foreach(var measureResult in measureResults)
            {
                this._avaliableMeasureResults.Add(new Tuple<MeasureResult,string>(measureResult, string.Format("{0} - Cap: {1} µm - Range: {2}", measureResult.Name, measureResult.MeasureSetup.CapillarySize, measureResult.MeasureSetup.ToDiameter)));
            }
        }

        private async Task ImportCRFFile(FileInfo fileInfo)
        {
            var measureResult = await _crfImportExportProvider.ImportAsync(fileInfo.FullName, "#FF009FE3");
            measureResult.MeasureResultGuid = Guid.NewGuid();
            //measureResult.Name = Path.GetFileNameWithoutExtension(fileInfo.Name);
            //measureResult.MeasureSetup.Name = string.Format("{0} Setup", measureResult.Name);

            this._avaliableMeasureResults.Add(new Tuple<MeasureResult, string>(measureResult, string.Format("{0} - Cap: {1} µm - Range: {2}", measureResult.Name, measureResult.MeasureSetup.CapillarySize, measureResult.MeasureSetup.ToDiameter)));
        }

        private async Task ImportTTFile(FileInfo fileInfo, bool isXlsx)
        {
            IEnumerable<MeasureResult> measureResults;
            if (isXlsx)
            {
                measureResults = await _ttImportExportProvider.ImportXlsxAsync(fileInfo.FullName, 1);
            }
            else
            {
                measureResults = await _ttImportExportProvider.ImportAsync(fileInfo.FullName, 1);
            }

            foreach (var measureResult in measureResults)
            {
                this._avaliableMeasureResults.Add(new Tuple<MeasureResult, string>(measureResult, string.Format("{0} - Cap: {1} µm - Range: {2}", measureResult.Name, measureResult.MeasureSetup.CapillarySize, measureResult.MeasureSetup.ToDiameter)));
            }
        }

        private void OnSimulatorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(e.PropertyName);
        }
    }
}
