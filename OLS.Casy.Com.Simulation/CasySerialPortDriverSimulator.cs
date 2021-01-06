using OLS.Casy.Base;
using OLS.Casy.Com.Api;
using OLS.Casy.Com.Simulation.ViewModels;
using OLS.Casy.Com.Simulation.Views;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OLS.Casy.Com.Simulation
{
    /// <summary>
    /// Simulation implementation for <see cref="ICasySerialPortDriver"/>. Simulates the communication
    /// with the casy device when no device is connected.
    /// It's possible to define return values for several functions in a special GUI.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    [Export(typeof(ICasySerialPortDriver))]
    [Export(typeof(CasySerialPortDriverSimulator))]
    public class CasySerialPortDriverSimulator : ModelBase, ICasySerialPortDriver
    {
        private readonly ICompositionFactory _compositionFactory;
        private readonly IConfigService _configService;
        private readonly ILocalizationService _localizationService;

        private bool _isConnected;
        private byte[] _binaryData;
        private ushort _currentToDiameter;
        private ushort _currentCapillarySize;
        private uint _lastChecksum;
        private bool _vacuumVentilState;
        private bool _pumpEngineState;
        private bool _capillaryRelayVoltage;
        private bool _measValveRelayVoltage;
        private bool _wasteValveRelayVoltage;
        private bool _cleanValceRelayVoltage;
        private bool _suckValveRelayVoltage;
        private bool _blowValveRelayVoltage;
        private double _capillaryVoltage;

        private Dictionary<int, ushort[]> _dataBlocks;

        private CancellationTokenSource _tokenSource;

        private MeasureResult _selectedMeasureResult;

        /// <summary>
        /// MEF Importing Constructor
        /// </summary>
        /// <param name="childViewService">Implementation of <see cref="IChildViewService"/></param>
        /// <param name="compositionFactory">Implementation of <see cref="ICompositionFactory"/> </param>
        [ImportingConstructor]
        public CasySerialPortDriverSimulator(
            ICompositionFactory compositionFactory,
            IConfigService configService,
            ILocalizationService localizationService)
        {
            this._compositionFactory = compositionFactory;
            this._configService = configService;
            this._localizationService = localizationService;

            this._dataBlocks = new Dictionary<int, ushort[]>();
            this._dataBlocks.Add(45, new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 21, 19, 25, 13, 29, 23, 19, 18, 27, 21, 27, 21, 24, 20, 22, 29, 25, 30, 31, 26, 28, 28, 28, 23, 27, 29, 36, 33, 35, 36, 27, 33, 35, 35, 40, 35, 29, 37, 46, 45, 46, 36, 45, 45, 47, 62, 55, 63, 56, 59, 57, 73, 60, 59, 59, 75, 64, 52, 80, 69, 68, 74, 81, 70, 76, 85, 83, 82, 80, 82, 82, 90, 86, 88, 90, 91, 89, 86, 91, 82, 91, 103, 106, 121, 110, 98, 104, 110, 81, 117, 137, 117, 108, 113, 112, 116, 112, 125, 125, 117, 117, 107, 106, 124, 104, 119, 126, 118, 117, 83, 126, 123, 119, 119, 119, 137, 123, 109, 120, 115, 120, 128, 118, 124, 123, 110, 93, 126, 111, 104, 108, 120, 122, 114, 107, 115, 115, 96, 107, 114, 102, 85, 112, 108, 112, 106, 97, 89, 96, 86, 84, 109, 95, 103, 103, 93, 91, 113, 103, 95, 96, 116, 96, 99, 102, 113, 97, 116, 82, 118, 115, 92, 124, 103, 108, 108, 104, 109, 107, 115, 105, 124, 113, 130, 107, 120, 112, 122, 109, 107, 108, 117, 134, 114, 110, 117, 91, 119, 123, 115, 114, 116, 110, 103, 113, 109, 113, 132, 113, 98, 124, 110, 98, 129, 130, 113, 119, 100, 107, 125, 78, 108, 108, 86, 103, 97, 89, 98, 96, 85, 107, 91, 104, 98, 96, 99, 94, 98, 116, 108, 91, 105, 87, 87, 87, 55, 90, 86, 83, 79, 75, 73, 84, 71, 84, 67, 72, 84, 67, 73, 70, 70, 60, 68, 65, 57, 61, 63, 66, 75, 56, 63, 58, 56, 49, 67, 45, 54, 54, 44, 58, 63, 50, 46, 51, 50, 58, 49, 49, 51, 48, 42, 45, 42, 55, 50, 39, 54, 35, 45, 48, 46, 33, 41, 30, 42, 38, 33, 30, 35, 41, 30, 45, 30, 35, 33, 35, 33, 36, 36, 39, 33, 31, 35, 38, 33, 37, 36, 29, 35, 32, 27, 17, 31, 24, 33, 25, 33, 22, 24, 22, 17, 28, 26, 23, 28, 22, 21, 21, 17, 22, 25, 33, 20, 26, 28, 16, 18, 25, 20, 14, 20, 14, 22, 24, 19, 25, 23, 16, 26, 24, 15, 21, 13, 15, 17, 17, 18, 31, 18, 16, 14, 23, 15, 13, 11, 17, 17, 20, 15, 16, 17, 15, 9, 10, 21, 13, 22, 11, 21, 22, 18, 11, 12, 20, 15, 11, 11, 12, 11, 7, 16, 10, 13, 11, 12, 7, 14, 17, 14, 10, 6, 13, 9, 12, 12, 13, 17, 12, 10, 8, 10, 12, 4, 7, 9, 8, 10, 12, 10, 7, 5, 8, 7, 13, 13, 11, 8, 12, 13, 12, 12, 8, 9, 7, 8, 10, 9, 13, 8, 9, 5, 7, 8, 10, 9, 18, 9, 11, 6, 10, 9, 11, 7, 11, 8, 12, 6, 6, 4, 7, 13, 6, 8, 11, 8, 11, 10, 9, 8, 8, 8, 9, 10, 10, 11, 7, 6, 12, 7, 8, 4, 6, 9, 7, 5, 6, 5, 5, 6, 9, 11, 8, 5, 8, 7, 2, 7, 10, 8, 12, 7, 11, 6, 9, 5, 5, 5, 3, 6, 7, 9, 13, 5, 3, 4, 8, 5, 3, 2, 9, 4, 7, 9, 5, 6, 8, 8, 7, 6, 5, 5, 9, 6, 10, 6, 3, 4, 4, 6, 3, 3, 6, 6, 1, 4, 7, 5, 4, 5, 4, 4, 6, 4, 7, 6, 4, 5, 6, 3, 2, 6, 5, 6, 4, 4, 2, 8, 2, 6, 2, 5, 7, 2, 4, 7, 4, 11, 2, 2, 8, 4, 8, 1, 4, 3, 5, 1, 1, 5, 5, 5, 4, 4, 2, 0, 8, 5, 4, 1, 4, 4, 3, 1, 4, 3, 1, 4, 5, 6, 2, 2, 5, 1, 4, 3, 0, 3, 6, 3, 4, 2, 3, 3, 3, 3, 2, 2, 6, 1, 6, 3, 3, 1, 3, 2, 3, 6, 4, 2, 6, 4, 3, 3, 4, 2, 1, 3, 3, 2, 1, 3, 4, 2, 3, 2, 2, 3, 1, 4, 3, 0, 5, 3, 1, 3, 5, 4, 3, 3, 2, 3, 2, 4, 0, 1, 1, 3, 4, 6, 4, 8, 3, 5, 2, 5, 2, 2, 3, 1, 0, 0, 4, 1, 1, 1, 1, 1, 2, 2, 3, 1, 3, 4, 1, 3, 1, 4, 3, 2, 2, 0, 0, 3, 2, 3, 2 });
            this._dataBlocks.Add(60, new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 88, 77, 68, 79, 71, 57, 55, 53, 51, 52, 50, 49, 44, 46, 46, 34, 35, 39, 38, 41, 20, 29, 23, 28, 23, 18, 11, 13, 19, 16, 10, 15, 11, 16, 11, 9, 12, 14, 10, 9, 12, 7, 15, 9, 10, 9, 4, 7, 5, 10, 10, 10, 13, 4, 9, 8, 7, 10, 7, 5, 6, 2, 4, 8, 5, 7, 6, 5, 9, 7, 3, 2, 6, 10, 5, 5, 5, 3, 3, 9, 7, 4, 4, 6, 3, 6, 4, 4, 3, 7, 3, 2, 2, 4, 3, 9, 2, 2, 2, 1, 7, 4, 2, 4, 0, 3, 6, 1, 2, 4, 7, 8, 5, 5, 4, 5, 2, 2, 3, 4, 4, 2, 3, 1, 5, 2, 2, 3, 3, 3, 4, 2, 2, 4, 3, 2, 2, 1, 6, 2, 2, 3, 4, 0, 2, 1, 4, 3, 4, 2, 7, 2, 2, 1, 1, 4, 3, 1, 5, 6, 1, 4, 3, 5, 2, 2, 7, 2, 4, 6, 5, 7, 4, 3, 8, 2, 4, 3, 0, 4, 2, 5, 2, 4, 3, 9, 4, 4, 6, 4, 4, 4, 4, 6, 6, 8, 6, 4, 5, 4, 2, 2, 5, 7, 6, 4, 6, 2, 7, 0, 3, 7, 8, 7, 2, 7, 2, 1, 5, 7, 6, 9, 7, 10, 9, 8, 10, 7, 6, 13, 3, 0, 5, 6, 13, 11, 3, 4, 7, 11, 7, 10, 13, 4, 17, 11, 9, 12, 11, 9, 8, 11, 8, 12, 11, 12, 12, 10, 9, 2, 11, 16, 6, 15, 15, 14, 17, 17, 18, 20, 16, 16, 18, 12, 13, 15, 11, 15, 20, 17, 15, 20, 17, 19, 22, 13, 16, 11, 10, 27, 22, 22, 15, 14, 13, 19, 28, 22, 23, 15, 23, 23, 24, 16, 23, 24, 29, 27, 23, 22, 23, 20, 18, 21, 18, 16, 19, 21, 21, 30, 26, 32, 28, 23, 27, 24, 25, 20, 21, 29, 32, 29, 26, 22, 25, 18, 24, 30, 25, 29, 22, 18, 30, 28, 16, 24, 26, 23, 34, 30, 28, 27, 20, 19, 32, 25, 33, 32, 27, 19, 26, 26, 19, 39, 35, 25, 27, 29, 30, 31, 27, 18, 26, 20, 27, 28, 32, 30, 25, 25, 22, 25, 21, 35, 28, 31, 24, 35, 31, 25, 38, 36, 27, 24, 27, 23, 28, 22, 32, 41, 29, 28, 21, 25, 25, 37, 28, 31, 23, 35, 18, 26, 27, 17, 25, 16, 27, 24, 29, 13, 19, 17, 28, 25, 18, 17, 22, 22, 20, 15, 19, 30, 29, 24, 20, 24, 11, 20, 16, 19, 22, 16, 16, 23, 27, 13, 14, 19, 19, 20, 13, 15, 15, 22, 16, 18, 8, 12, 15, 17, 18, 16, 19, 21, 22, 13, 12, 13, 13, 14, 8, 15, 11, 9, 11, 15, 13, 12, 7, 12, 17, 9, 8, 13, 8, 9, 8, 10, 9, 12, 9, 19, 7, 16, 10, 6, 6, 9, 4, 6, 5, 4, 8, 9, 3, 4, 9, 4, 4, 6, 5, 5, 11, 4, 11, 5, 7, 3, 4, 4, 5, 4, 5, 4, 2, 4, 5, 4, 5, 8, 7, 5, 5, 9, 8, 3, 1, 3, 5, 6, 4, 4, 3, 5, 3, 5, 5, 7, 6, 6, 6, 3, 5, 6, 6, 2, 1, 5, 3, 6, 3, 4, 4, 4, 2, 2, 4, 4, 6, 3, 3, 1, 8, 5, 3, 0, 4, 2, 6, 2, 0, 1, 1, 6, 4, 4, 3, 2, 6, 0, 2, 1, 2, 2, 4, 4, 4, 3, 1, 4, 2, 1, 4, 1, 1, 4, 3, 3, 4, 4, 3, 3, 1, 1, 4, 3, 1, 1, 2, 2, 1, 1, 1, 3, 1, 4, 0, 1, 2, 1, 1, 3, 1, 1, 2, 0, 2, 4, 3, 1, 1, 2, 1, 3, 0, 2, 1, 0, 0, 4, 0, 2, 5, 1, 4, 0, 2, 0, 1, 2, 3, 1, 3, 4, 2, 0, 0, 2, 1, 3, 3, 1, 2, 0, 1, 1, 1, 1, 0, 1, 3, 2, 0, 1, 2, 3, 2, 2, 2, 1, 2, 2, 2, 1, 1, 1, 0, 3, 2, 2, 2, 0, 2, 3, 3, 1, 3, 3, 1, 3, 1, 1, 2, 2, 2, 1, 1, 4, 3, 1, 0, 1, 2, 1, 2, 1, 0, 1, 1, 0, 0, 2, 1, 1, 3, 2, 2, 0, 2, 3, 1, 1, 2, 3, 0, 1, 4, 4, 1, 2, 0, 2, 1, 1, 1, 0, 2, 1, 1, 2, 2, 1, 0, 0, 0, 0, 4, 1, 1, 0, 2, 0, 0, 1, 1, 1, 1, 1, 2, 1, 2, 0, 0, 3, 1, 1, 0, 0, 0, 1, 3, 0, 0, 0, 0, 2, 0, 0, 0, 0, 2, 2, 2, 1, 2, 0, 1, 1, 1, 0, 0, 1, 2, 2, 2, 0, 1, 0, 1, 2, 3, 1, 0, 1, 2, 4, 1, 1, 1, 1, 2, 0, 1, 1, 1, 0, 3, 0, 0, 2, 0, 0, 1, 0, 0, 0, 1, 1, 0, 2, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 3, 1, 1, 1, 0, 1, 1, 0, 2, 1, 0, 2, 2, 1, 1, 0, 1, 0, 2, 1, 1, 0, 2, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 2, 1, 1, 1, 0, 1, 1, 4, 1, 2, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 1, 1, 0 });
            this._dataBlocks.Add(150, new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 3, 5, 1, 6, 1, 3, 3, 2, 2, 3, 2, 1, 4, 0, 2, 4, 1, 4, 3, 4, 1, 4, 2, 4, 3, 1, 0, 1, 1, 1, 5, 6, 1, 1, 0, 4, 1, 2, 5, 0, 3, 2, 3, 3, 3, 3, 1, 3, 3, 1, 0, 0, 2, 5, 1, 2, 2, 1, 3, 1, 6, 2, 1, 3, 0, 1, 4, 0, 4, 1, 0, 0, 3, 0, 1, 1, 3, 3, 2, 2, 1, 3, 1, 1, 2, 2, 0, 0, 3, 1, 0, 1, 1, 0, 0, 2, 1, 0, 1, 0, 2, 1, 2, 0, 1, 1, 0, 1, 1, 0, 0, 0, 1, 2, 0, 2, 3, 1, 1, 1, 0, 0, 2, 1, 1, 1, 1, 0, 2, 1, 1, 0, 2, 0, 0, 1, 0, 3, 1, 3, 0, 2, 2, 1, 1, 2, 2, 1, 0, 2, 1, 1, 1, 0, 2, 2, 0, 1, 2, 4, 1, 2, 0, 2, 1, 3, 1, 3, 1, 3, 2, 2, 1, 3, 1, 2, 4, 4, 2, 3, 4, 4, 2, 2, 1, 4, 2, 6, 3, 2, 3, 2, 0, 5, 2, 4, 3, 3, 2, 5, 1, 6, 7, 4, 5, 7, 6, 7, 4, 6, 12, 6, 10, 5, 7, 7, 12, 10, 11, 11, 10, 14, 16, 8, 11, 11, 12, 9, 13, 17, 19, 14, 13, 9, 16, 13, 16, 17, 12, 15, 13, 20, 15, 22, 18, 16, 15, 18, 16, 17, 26, 14, 16, 20, 21, 26, 25, 15, 23, 14, 21, 17, 24, 16, 20, 20, 28, 21, 24, 25, 24, 36, 27, 22, 23, 25, 25, 23, 22, 21, 18, 27, 26, 22, 18, 19, 34, 35, 25, 28, 24, 28, 30, 30, 27, 23, 29, 27, 24, 30, 27, 38, 24, 18, 21, 23, 17, 18, 35, 26, 27, 14, 31, 18, 30, 27, 24, 24, 25, 28, 23, 25, 22, 29, 20, 27, 18, 22, 19, 18, 26, 29, 13, 21, 20, 22, 25, 15, 21, 25, 15, 30, 21, 16, 26, 18, 28, 22, 23, 21, 19, 18, 19, 19, 19, 15, 22, 18, 23, 22, 17, 21, 17, 22, 23, 13, 22, 16, 17, 12, 10, 16, 14, 17, 11, 13, 11, 14, 18, 13, 17, 14, 16, 12, 18, 9, 9, 12, 16, 10, 6, 9, 10, 15, 15, 10, 10, 12, 5, 16, 14, 11, 11, 4, 9, 11, 13, 13, 6, 8, 9, 8, 6, 9, 2, 11, 15, 13, 5, 5, 10, 14, 10, 13, 10, 6, 9, 7, 9, 6, 5, 11, 8, 6, 9, 4, 5, 7, 5, 5, 4, 8, 2, 4, 4, 8, 3, 8, 9, 7, 8, 2, 9, 9, 2, 11, 4, 6, 2, 6, 3, 5, 7, 2, 10, 6, 7, 3, 7, 3, 4, 4, 3, 5, 5, 4, 1, 4, 4, 2, 4, 6, 2, 6, 3, 4, 0, 6, 7, 4, 8, 2, 7, 5, 4, 6, 5, 2, 6, 1, 3, 2, 4, 3, 4, 6, 3, 1, 3, 3, 2, 2, 0, 4, 1, 2, 2, 3, 2, 1, 2, 1, 3, 1, 5, 0, 2, 0, 3, 2, 5, 3, 1, 4, 4, 4, 3, 3, 0, 1, 0, 4, 2, 1, 3, 3, 0, 2, 2, 3, 2, 3, 3, 2, 2, 3, 5, 0, 3, 0, 1, 1, 0, 2, 0, 3, 1, 3, 3, 0, 2, 2, 2, 1, 1, 1, 0, 2, 2, 2, 3, 4, 3, 3, 2, 0, 1, 0, 3, 1, 2, 1, 1, 3, 0, 3, 1, 1, 3, 0, 1, 0, 1, 0, 0, 0, 2, 2, 2, 4, 1, 0, 0, 3, 0, 2, 0, 2, 2, 2, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 2, 1, 1, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0, 3, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 2, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 2, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            //this.CurrentDataBlock = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 481, 265, 200, 155, 109, 90, 65, 55, 40, 32, 43, 24, 27, 11, 13, 11, 7, 7, 6, 8, 6, 10, 3, 9, 4, 3, 2, 2, 2, 4, 0, 2, 0, 4, 3, 3, 1, 0, 1, 2, 1, 2, 1, 1, 3, 0, 2, 2, 0, 0, 1, 0, 1, 2, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 2, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 2, 0, 2, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 2, 0, 0, 2, 0, 0, 0, 0, 1, 2, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 2, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 1, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 3, 1, 1, 0, 2, 2, 0, 1, 0, 1, 1, 1, 3, 2, 2, 3, 3, 1, 1, 1, 2, 1, 3, 6, 1, 6, 3, 6, 5, 4, 1, 4, 3, 8, 8, 5, 5, 10, 5, 10, 10, 11, 11, 9, 7, 13, 12, 11, 9, 9, 20, 9, 18, 17, 23, 9, 20, 15, 14, 15, 19, 19, 18, 22, 24, 31, 20, 22, 23, 24, 27, 25, 20, 31, 27, 41, 21, 38, 28, 31, 19, 33, 24, 33, 34, 28, 21, 42, 30, 31, 26, 29, 24, 31, 31, 29, 29, 31, 21, 21, 30, 24, 22, 15, 18, 18, 22, 23, 16, 18, 15, 13, 16, 16, 23, 19, 11, 17, 11, 10, 12, 11, 11, 14, 4, 6, 5, 9, 9, 4, 5, 5, 6, 2, 3, 4, 2, 1, 2, 1, 5, 2, 2, 3, 2, 2, 2, 5, 2, 3, 5, 5, 3, 6, 0, 4, 3, 3, 2, 3, 2, 5, 1, 2, 0, 3, 5, 0, 2, 1, 2, 1, 0, 5, 1, 4, 3, 4, 3, 5, 5, 4, 2, 2, 1, 3, 4, 1, 3, 1, 1, 3, 0, 1, 4, 4, 5, 3, 2, 2, 2, 2, 3, 3, 3, 2, 3, 2, 2, 2, 1, 2, 1, 3, 5, 4, 1, 1, 0, 4, 3, 1, 1, 2, 1, 1, 0, 2, 1, 0, 1, 2, 4, 3, 1, 1, 3, 1, 0, 0, 0, 2, 0, 0, 1, 0, 1, 2, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 1, 1, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 3, 0, 0, 1, 1, 0, 2, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0 };
            this.CurrentStatistic = Encoding.Default.GetBytes("\0–\0\u0001\0\0\0\0\0\n\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0 \0\u0002\0\u0014\0\v\0\u0005\0\t\aà\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0<\0\b\0\u0005\0\0\0ô\0\0\u0002ª\0\u0010\0\0\0\0\0\a\0\v\0\b\0\v\aà=®\0\0\0\0\0\u0010\0\0\0\0\0\b\0\v\0\b\0\v\aà>*\0\0\0\0\0\u0010\0\0\0\0\0\u0010\0\f\0\b\0\v\aà=Ž\0\0\0\0\0\u0010\0\0\0\0\0\u0010\0\f\0\b\0\v\aà=Ä\0\0\0\0\0\u0010\0\0\0\0\0\u0016\0\u000e\0\b\0\v\aà=é\0\0\0\0\0\u0010\0\0\0\0\0\u0019\0\u000e\0\b\0\v\aà=á\0\0\0\0\0\u0010\0\0\0\0\0\u001a\0\u000e\0\b\0\v\aà=Â\0\0\0\0\0\u0010\0\0\0\0\0$\0\u000f\0\b\0\v\aà=Ö\0\0\0\0\0\u0001\0\0\0\0\0\u001c\0\r\0\u001e\0\v\aà\0\0\0\0\0\0\0\u0010\0\0\0\0\02\0\u000e\0\a\0\v\aà=²\0\0\0\0\0\0\0\0\0\0\0\u0015\0\u0010\0\u0019\0\n\aà;¼>½\0\0\0\0\0\0\0\0\0\u0016\0\u0010\0\u0019\0\n\aà=$>Ç\0J\0\0\0\0\0\0\0\u0017\0\u0010\0\u0019\0\n\aà;Ì>°\0\0\0\0\0\0\0\0\0\u0018\0\u0010\0\u0019\0\n\aà;Î?\u0005\0\0\0\0\0\0\0\0\0\u0019\0\u0010\0\u0019\0\n\aà;Î>ä\0\0\0\0\0\0\0\0\0\u001a\0\u0010\0\u0019\0\n\aà;É?\u0006\0\0\0\0\0\0\0\0\0\u0010\0\u0010\0\u0019\0\n\aà;ü?\u0014\0\0\0\0\0\0\0\0\0\u0010\0\u0010\0\u0019\0\n\aà<&?D\0\0\0\0\0\0\0\0\0\u0014\0\u0010\0\u0019\0\n\aà;©>Ý\0\0\0\0\0\0\0\0\0\u0014\0\u0010\0\u0019\0\n\aà;É>Í\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\r\0\u000e\0\u000f\0\u0006\aà\0\u001c\0\r\0\u001e\0\v\aà\0\u000e\0\u0014\0\u000e\0\u0006\0\u0004\aà\0\u0010\0\v\0\u0019\0\n\aà\0\n\04\0\a\0\u001b\0\a\aà\0\f\0\u0010\0\u0019\0\n\aà\0\u0011\0+\0\t\0\u001b\0\u0004\aà\0\u001c\0\f\0\u0019\0\n\aà\0\a\0/\0\r\0\u001a\0\a\aà\0$\0\u000f\0\b\0\v\aà\0(\0\u0004\0\n\0\u001b\0\u0004\aà\05\0\u000f\0\u0019\0\n\aà\0\u001a\0\u0016\0\u0010\0\u000e\0\v\aà\0\0\0\0\0\0\0\0\0\0\0\u0001\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0&\0\t\0\u0011\0\b\aà\0\u0005\0\u000e\0\u0010\0\t\aà\0\u0013\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\u0002\0\t\0\n\0\n\aà\0\0\0\0\0\0\0\0\0\0\0\u0001\0,\0\u000e\0\u001b\0\u0004\aà\06\0\u000e\0\u0018\0\b\aà\0\u0004\0\u0005\0\n\0\u001b\0\a\aà\0\u0005\0\n\0\u001b\0\a\aà\0\u0002\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\u0014\0\v\0\u0005\0\t\aà\04\0\v\0\u0005\0\t\aà\0\u0002\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\u0014\0\v\0\u0005\0\f\aà\0\f\0\r\0\u0005\0\f\aà\0\u0002\0\u000f\0\n\0\u0003\aà\0\u001b\0\a\0\n\0\0\0*\0\0\u0001^\0\0\"\u0019\0\0Rö");

            this.UseCorrectChecksum = true;
            this.CurrentBelowMeasureLimtCount = 0;
            this.CurrentBelowCalibrationLimitCount = 0;
            this.CurrentAboveCalibrationLimitCount = 0;
            this.CurrentMeasureDelay = 4d;

            //SetBinaryData();

            this.CurrentError = "0000,0000,0000";
            this.CurrentCleanError = "0000,0000,0000";
            this.CurrentHardwareSelfTestError = "0000,0000,0000";
            this.CurrentMeasureError = "0000,0000,0000";
            this.CurrentPressureSelfTestError = "0000,0000,0000";
            this.CurrentSelfTestError = "0000,0000,0000";
            this.CurrentSoftwareSelfTestError = "0000,0000,0000";
            this.CurrentCalibrationError = "0000,0000,0000";
            this.CurrentSerialNumber = "TTC-2BA-1015";
            this.CurrentDryError = "0000,0000,0000";
            this.CurrentMasterPin = "12345";

            this._tokenSource = new CancellationTokenSource();
        }

        private uint SetBinaryData()
        {
            uint checksum;

            if (this.SelectedMeasureResult != null &&
                this.SelectedMeasureResult.MeasureSetup.CapillarySize == this.CapillarySize &&
                this.SelectedMeasureResult.MeasureSetup.ToDiameter == this.ToDiameter)
            {
                this._binaryData = new byte[(this.SelectedMeasureResult.MeasureSetup.ChannelCount * 2) + 16];

                int index = this._currentRepeat;
                while(index > this.SelectedMeasureResult.MeasureResultDatas.Count)
                {
                    index -= this.SelectedMeasureResult.MeasureResultDatas.Count;
                }

                ushort[] dataBlock = this.SelectedMeasureResult.MeasureResultDatas[index-1].DataBlock.Select(d => (ushort) d).ToArray();
                for (int i = 0; i < dataBlock.Length; i++)
                {
                    var converted = BitConverter.GetBytes(dataBlock[i]);
                    this._binaryData[2 * i] = converted[1];
                    this._binaryData[2 * i + 1] = converted[0];
                }

                var count = this._binaryData.Length;

                var bytes = BitConverter.GetBytes(this.SelectedMeasureResult.MeasureResultDatas[index - 1].BelowMeasureLimtCount);
                this._binaryData[count-16] = bytes[3];
                this._binaryData[count-15] = bytes[2];
                this._binaryData[count-14] = bytes[1];
                this._binaryData[count-13] = bytes[0];

                bytes = BitConverter.GetBytes(this.SelectedMeasureResult.MeasureResultDatas[index - 1].BelowCalibrationLimitCount);
                this._binaryData[count-12] = bytes[3];
                this._binaryData[count-11] = bytes[2];
                this._binaryData[count-10] = bytes[1];
                this._binaryData[count-9] = bytes[0];

                bytes = BitConverter.GetBytes(this.SelectedMeasureResult.MeasureResultDatas[index - 1].AboveCalibrationLimitCount);
                this._binaryData[count-8] = bytes[3];
                this._binaryData[count-7] = bytes[2];
                this._binaryData[count-6] = bytes[1];
                this._binaryData[count-5] = bytes[0];

                if(UseCorrectChecksum)
                {
                    checksum = CalcChecksum(_binaryData);
                }
                else
                {
                    checksum = 0;
                }
                bytes = BitConverter.GetBytes(checksum);
                this._binaryData[count-4] = bytes[3];
                this._binaryData[count-3] = bytes[2];
                this._binaryData[count-2] = bytes[1];
                this._binaryData[count-1] = bytes[0];
            }
            else
            {
                this._binaryData = new byte[2064];
                ushort[] dataBlock = this._dataBlocks[this.CapillarySize];

                for (int i = 0; i < dataBlock.Length; i++)
                {
                    var converted = BitConverter.GetBytes(dataBlock[i]);
                    this._binaryData[2 * i] = converted[1];
                    this._binaryData[2 * i + 1] = converted[0];
                }
                var bytes = BitConverter.GetBytes(CurrentBelowMeasureLimtCount);
                this._binaryData[2048] = bytes[3];
                this._binaryData[2049] = bytes[2];
                this._binaryData[2050] = bytes[1];
                this._binaryData[2051] = bytes[0];

                bytes = BitConverter.GetBytes(CurrentBelowCalibrationLimitCount);
                this._binaryData[2052] = bytes[3];
                this._binaryData[2053] = bytes[2];
                this._binaryData[2054] = bytes[1];
                this._binaryData[2055] = bytes[0];

                bytes = BitConverter.GetBytes(CurrentAboveCalibrationLimitCount);
                this._binaryData[2056] = bytes[3];
                this._binaryData[2057] = bytes[2];
                this._binaryData[2058] = bytes[1];
                this._binaryData[2059] = bytes[0];

                if (UseCorrectChecksum)
                {
                    checksum = CalcChecksum(_binaryData);
                }
                else
                {
                    checksum = 0;
                }
                bytes = BitConverter.GetBytes(checksum);
                this._binaryData[2060] = bytes[3];
                this._binaryData[2061] = bytes[2];
                this._binaryData[2062] = bytes[1];
                this._binaryData[2063] = bytes[0];
            }
            return checksum;
        }

        private uint CalcChecksum(byte[] source)
        {
            uint checksum = 0;

            for (int i = 0; i < source.Length - 4; i++)
            {
                checksum += (uint)source[i];
            }
            _lastChecksum = checksum;
            return checksum;
        }

        public MeasureResult SelectedMeasureResult
        {
            get { return _selectedMeasureResult; }
            set { this._selectedMeasureResult = value; }
        }

        /// <summary>
        /// Name of the serial port the casy device is connected to
        /// </summary>
        public string ConnectedSerialPort
        {
            get { return "Simulator"; }
        }

        /// <summary>
        /// Returns the connection state of the casy serial port device
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                this._isConnected = value;
                if (OnIsConnectedChangedEvent != null)
                {
                    foreach (EventHandler receiver in OnIsConnectedChangedEvent.GetInvocationList())
                    {
                        receiver.BeginInvoke(this, EventArgs.Empty, null, null);
                    }

                }
            }
        }

        private async void DoProgress(IProgress<string> progress)
        {
            await Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                progress?.Report("CASYCOMMAND_PROGRESS_SENDING");
                //Thread.Sleep(100);
                progress?.Report("CASYCOMMAND_PROGRESS_CONFIRMED");
                //Thread.Sleep(100);
                progress?.Report("CASYCOMMAND_PROGRESS_RECEIVED");
                //Thread.Sleep(100);
                progress?.Report("CASYCOMMAND_PROGRESS_CLEANUP");
            }, DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Event will be raised when connection state has changed
        /// </summary>
        public event EventHandler OnIsConnectedChangedEvent;

        internal string CurrentCalibrationError { get; set; }
        public string Calibrate(ushort toDiameter, byte[] calibrationData, IProgress<string> progress)
        {
            this.ToDiameter = toDiameter;

            this._currentRepeat = 0;
            this.CurrentError = CurrentCalibrationError;
            DoProgress(progress);
            ExtractCalibrationInfo(calibrationData);
            return CurrentCalibrationError;
        }

        private void ExtractCalibrationInfo(byte[] calibrationData)
        {
            MemoryStream ms = new MemoryStream(calibrationData);

            var buffer = new byte[105];
            ms.Read(buffer, 0, 105);
            //buffer = new byte[105];
            //ms.Read(buffer, 0, 105);
                
            byte[] wCapillarySizeBuf = new byte[2];
            ms.Read(wCapillarySizeBuf, 0, 2);
            this.CapillarySize = SwapHelper.SwapBytes(BitConverter.ToUInt16(wCapillarySizeBuf, 0));

            ms.Close();
        }


        /// <summary>
        ///     The Deinitialize method is for cleaning up, storing and closing resouces.
        /// </summary>
        public void Deinitialize(IProgress<string> progress)
        {
        }

        /// <summary>
        ///     Method will be called when all dependent services in the state Ready.
        /// </summary>
        public void DependenciesReady()
        {
        }

        /// <summary>
        /// Property for the UI to set the current data block the simulator returns
        /// </summary>
        //internal ushort[] CurrentDataBlock { get; set; }

        /// <summary>
        /// Property for the UI to set the current below measure count the simulator returns
        /// </summary>
        internal uint CurrentBelowMeasureLimtCount { get; set; }

        /// <summary>
        /// Property for the UI to set the below calibration limit count block the simulator returns
        /// </summary>
        internal uint CurrentBelowCalibrationLimitCount { get; set; }

        /// <summary>
        /// Property for the UI to set the above calibration limit count block the simulator returns
        /// </summary>
        internal uint CurrentAboveCalibrationLimitCount { get; set; }

        /// <summary>
        /// Property for the UI to set the indicator wheather the simulator shall return the correct checksum or not
        /// </summary>
        internal bool UseCorrectChecksum { get; set; }

        /// <summary>
        /// Returns async last measurement result data from casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public byte[] GetBinaryData(IProgress<string> progress)
        {
            var checksum = SetBinaryData();
            DoProgress(progress);
            return this._binaryData;
        }

        /// <summary>
        /// Property for the UI to set the current error the simulator returns
        /// </summary>
        internal string CurrentError { get; set; }

        /// <summary>
        /// Returns async the last error occured on casy device.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The last error string occured on casy device</returns>
        public string GetError(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CurrentError;
        }

        /// <summary>
        /// Property for the UI to set the current serial number the simulator returns
        /// </summary>
        internal string CurrentSerialNumber { get; set; }

        /// <summary>
        /// Returns async the serial number and the check sum of the casy device
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The serial number and corresponding check sum of the casy device</returns>
        public Tuple<string, uint> GetSerialNumber(IProgress<string> progress)
        {
            DoProgress(progress);
            _lastChecksum = 0;
            return new Tuple<string, uint>(CurrentSerialNumber, 0); 
        }

        /// <summary>
        /// Property for the UI to set the current measurement error the simulator returns
        /// </summary>
        internal string CurrentMeasureError { get; set; }

        /// <summary>
        /// Property for the UI to set the current measure delay simulator uses while measuring to simulate long running measurement processes
        /// </summary>
        internal double CurrentMeasureDelay { get; set; }

        private int _currentRepeat = 0;

        /// <summary>
        /// Starts async a measurement with the casy device (200 micro litre)
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string Measure200(IProgress<string> progress)
        {
            this._currentRepeat++;
            DoProgress(progress);
            this.CurrentError = CurrentMeasureError;
           Thread.Sleep(TimeSpan.FromSeconds(CurrentMeasureDelay));
            return this.CurrentMeasureError;
        }

        /// <summary>
        /// Starts async a measurement with the casy device (400 micro litre)
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string Measure400(IProgress<string> progress)
        {
            this._currentRepeat++;
            DoProgress(progress);
            this.CurrentError = CurrentMeasureError;
            Thread.Sleep(TimeSpan.FromSeconds(CurrentMeasureDelay));
            return this.CurrentMeasureError;
        }

        public void Stop(IProgress<string> progress)
        {
            DoProgress(progress);
            this._tokenSource.Cancel();
            this._tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     This  method can be used to initialize the service and perform actions, which do
        ///     not expect other dependent services with OnReady state.
        /// </summary>
        public void Prepare(IProgress<string> progress)
        {
            progress.Report(_localizationService.GetLocalizedString("SplashScreen_Message_SimulatorDetected"));

            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                var view = _compositionFactory.GetExport<CasySerialPortSimulatorView>().Value;
                var viewModel = _compositionFactory.GetExport<CasySerialPortSimulatorViewModel>().Value;

                view.DataContext = viewModel;
                view.Show();
            }, DispatcherPriority.ApplicationIdle);
            CheckCasyDeviceConnection();
        }

        public bool CheckCasyDeviceConnection(IProgress<string> progress = null)
        {
            if (!this.IsConnected)
            {
                this.IsConnected = true;
            }
            return true;
        }

        /// <summary>
        /// Property for the UI to set the current clean error the simulator returns
        /// </summary>
        internal string CurrentCleanError { get; set; }

        /// <summary>
        /// Starts async a clean on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <param name="cleanCount">Optional: Number of cleans to be executed by the casy device</param>
        /// <returns>The result string of the operation</returns>
        public string Clean(IProgress<string> progress, int cleanCount = 1)
        {
            Thread.Sleep(2000 * cleanCount);
            DoProgress(progress);
            this.CurrentError = CurrentCleanError;
            return this.CurrentCleanError;
        }

        /// <summary>
        /// Property for the UI to set the current hardware self test error the simulator returns
        /// </summary>
        internal string CurrentHardwareSelfTestError { get; set; }

        /// <summary>
        /// Starts async a hardware self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartHardwareSelfTest(IProgress<string> progress)
        {
            DoProgress(progress);
            Thread.Sleep(2000);
            this.CurrentError = CurrentHardwareSelfTestError;
            return this.CurrentHardwareSelfTestError;
        }

        /// <summary>
        /// Property for the UI to set the current pressure selt test error the simulator returns
        /// </summary>
        internal string CurrentPressureSelfTestError { get; set; }

        /// <summary>
        /// Starts async a pressure system self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartPressureSystemSelfTest(IProgress<string> progress)
        {
            DoProgress(progress);
            Thread.Sleep(2000);
            this.CurrentError = CurrentPressureSelfTestError;
            return this.CurrentPressureSelfTestError;
        }

        /// <summary>
        /// Property for the UI to set the current self test error the simulator returns
        /// </summary>
        internal string CurrentSelfTestError { get; set; }

        /// <summary>
        /// Starts async a self test on the casy device and returns the corresponding result string.
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartSelfTest(IProgress<string> progress)
        {
            DoProgress(progress);
            this.CurrentError = CurrentSelfTestError;
            return this.CurrentSelfTestError;
        }

        /// <summary>
        /// Property for the UI to set the current software self test error the simulator returns
        /// </summary>
        public string CurrentSoftwareSelfTestError { get; set; }

        /// <summary>
        /// Starts async a software self test on the casy device an returns the corresponding result string
        /// </summary>
        /// <param name="progress">Implementation of <see cref="IProgress{T}"/> for reporting the progress of the operation</param>
        /// <returns>The result string of the operation</returns>
        public string StartSoftwareSelfTest(IProgress<string> progress)
        {
            DoProgress(progress);
            Thread.Sleep(2000);
            this.CurrentError = CurrentSoftwareSelfTestError;
            return this.CurrentSoftwareSelfTestError;
        }

        public Tuple<DateTime, uint> GetDateTime(IProgress<string> progress)
        {
            DoProgress(progress);
            _lastChecksum = 0;
            return new Tuple<DateTime, uint>(DateTime.Now, 0);
        }

        internal ushort ToDiameter
        {
            get { return _currentToDiameter; }
            set
            {
                _currentToDiameter = value;
                NotifyOfPropertyChange();
            }
        }
        internal ushort CapillarySize
        {
            get { return _currentCapillarySize; }
            set
            {
                _currentCapillarySize = value;
                NotifyOfPropertyChange();
            }
        }

        public Tuple<ushort, ushort, uint> GetCalibrationVerifactionData(IProgress<string> progress)
        {
            DoProgress(progress);
            _lastChecksum = 0;
            return new Tuple<ushort, ushort, uint>(CapillarySize, ToDiameter, 0);
        }

        internal string CurrentMasterPin { get; set; }

        public bool VerifyMasterPin(string masterPin, IProgress<string> progress)
        {
            DoProgress(progress);
            return masterPin == CurrentMasterPin;
        }

        public Tuple<byte[], uint> GetHeader(IProgress<string> progress)
        {
            _lastChecksum = 0;
            DoProgress(progress);
            return new Tuple<byte[], uint>(Enumerable.Repeat<byte>(0, 158).ToArray(), 0);
        }

        public uint RequestLastChecksum(IProgress<string> progress)
        {
            DoProgress(progress);
            return _lastChecksum;
        }

        public string CreateTestPattern(IProgress<string> progress)
        {
            DoProgress(progress);
            this.CurrentError = CurrentMeasureError;
            Thread.Sleep(TimeSpan.FromSeconds(CurrentMeasureDelay));
            return this.CurrentMeasureError;
        }

        internal string CurrentDryError { get; set; }

        public string Dry(IProgress<string> progress)
        {
            DoProgress(progress);

            Thread.Sleep(2000);

            this.CurrentError = this.CurrentDryError;
            return this.CurrentDryError;
        }

        internal bool IsLightBarrierLED { get; set; }
        internal bool IsGreenLED { get; set; }
        internal bool IsFirstRedLED { get; set; }
        internal bool IsSecondRedLED { get; set; }

        public byte StartLEDTest(IProgress<string> progress)
        {
            DoProgress(progress);

            byte result = 0;
            if(IsLightBarrierLED)
            {
                result += (int) LEDs.LightBarrier;
            }
            if(IsGreenLED)
            {
                result += (int)LEDs.Green;
            }
            if(IsFirstRedLED)
            {
                result += (int)LEDs.FirstRed;
            }
            if(IsSecondRedLED)
            {
                result += (int)LEDs.SecondRed;
            }

            return result;
        }

        public bool PerformBlow(IProgress<string> progress)
        {
            DoProgress(progress);
            Thread.Sleep(1000);
            return true;
        }

        public bool PerformSuck(IProgress<string> progress)
        {
            DoProgress(progress);
            Thread.Sleep(1000);
            return true;
        }

        internal bool VacuumVentilState
        {
            get { return _vacuumVentilState; }
            set
            {
                _vacuumVentilState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetVacuumVentilState(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            VacuumVentilState = state;
            return true;
        }

        internal bool PumpEngineState
        {
            get { return _pumpEngineState; }
            set
            {
                _pumpEngineState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetPumpEngineState(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            PumpEngineState = state;
            return true;
        }

        internal bool CapillaryRelayVoltage
        {
            get { return _capillaryRelayVoltage; }
            set
            {
                _capillaryRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetCapillaryRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            CapillaryRelayVoltage = state;
            return true;
        }

        internal bool MeasValveRelayVoltage
        {
            get { return _measValveRelayVoltage; }
            set
            {
                _measValveRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetMeasValveRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            MeasValveRelayVoltage = state;
            return true;
        }

        internal bool WasteValveRelayVoltage
        {
            get { return _wasteValveRelayVoltage; }
            set
            {
                _wasteValveRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetWasteValveRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            WasteValveRelayVoltage = state;
            return true;
        }

        public byte GetValveState(IProgress<string> progress)
        {
            DoProgress(progress);

            byte result = 0;
            if(CapillaryRelayVoltage)
            {
                result += (int)Valves.Capillary;
            }
            if(WasteValveRelayVoltage)
            {
                result += (int)Valves.Waste;
            }
            if(MeasValveRelayVoltage)
            {
                result += (int)Valves.Meas;
            }
            if(VacuumVentilState)
            {
                result += (int)Valves.Vacuum;
            }
            if(PumpEngineState)
            {
                result += (int)Valves.PumpEngine;
            }
            if(CleanValveRelayVoltage)
            {
                result += (int)Valves.Clean;
            }
            if(BlowValveRelayVoltage)
            {
                result += (int)Valves.Blow;
            }
            if(SuckValveRelayVoltage)
            {
                result += (int)Valves.Suck;
            }
            
            return result;
        }

        public byte[] GetStatistik(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CurrentStatistic;
        }

        public bool SetSerialNumber(string serialNumber, IProgress<string> progress)
        {
            DoProgress(progress);

            var dataBytes = Encoding.UTF8.GetBytes(serialNumber);
            var checkSum = Calculations.CalcChecksum(dataBytes);
            var checkSumBytes = BitConverter.GetBytes(checkSum);

            byte[] data = new byte[15 + checkSumBytes.Length];
            Buffer.BlockCopy(dataBytes, 0, data, 0, dataBytes.Length);
            Buffer.BlockCopy(checkSumBytes, 0, data, data.Length - 4, checkSumBytes.Length);

            this.CurrentSerialNumber = serialNumber;
            return true;
        }

        public string CleanWaste(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CurrentCleanError;
        }

        public string CleanCapillary(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CurrentCleanError;
        }

        public bool SetDateTime(DateTime dateTime, IProgress<string> progress)
        {
            DoProgress(progress);

            return true;
        }

        internal bool CleanValveRelayVoltage
        {
            get { return _cleanValceRelayVoltage; }
            set
            {
                _cleanValceRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetCleanValveRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            this.CleanValveRelayVoltage = state;
            return true;
        }

        internal bool BlowValveRelayVoltage
        {
            get { return _blowValveRelayVoltage; }
            set
            {
                _blowValveRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetBlowValveRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            this.BlowValveRelayVoltage = state;
            return true;
        }

        internal bool SuckValveRelayVoltage
        {
            get { return _suckValveRelayVoltage; }
            set
            {
                _suckValveRelayVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetSuckValveRelayVoltage(bool state, IProgress<string> progress)
        {
            DoProgress(progress);
            this.SuckValveRelayVoltage = state;
            return true;
        }

        internal double CapillaryVoltage
        {
            get { return _capillaryVoltage; }
            set
            {
                _capillaryVoltage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool SetCapillaryVoltage(int value, IProgress<string> progress)
        {
            DoProgress(progress);
            this.CapillaryVoltage = value;
            return true;
        }

        public double GetCapillaryVoltage(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CapillaryVoltage;
        }

        internal double CurrentPressure { get; set; }

        public double GetPressure(IProgress<string> progress)
        {
            DoProgress(progress);
            return this.CurrentPressure;
        }

        public bool ClearErrorBytes(IProgress<string> progress)
        {
            DoProgress(progress);
            return true;
        }

        public bool ResetStatistic(IProgress<string> progress)
        {
            DoProgress(progress);
            return true;
        }

        public bool ResetCalibration(IProgress<string> progress)
        {
            DoProgress(progress);
            return true;
        }

        public string CheckRisetime(IProgress<string> progress)
        {
            DoProgress(progress);

            Thread.Sleep(3000);

            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                "0000", //First Error Code
                "0000", //Second Error Code
                "0000", //Third Error Code,
                "002D", // Min Time Green
                "0001", // Max Time Green
                "0E51", // Avg Time Green,
                "002D", // Min Time 200
                "0001", // Max Time 200
                "0E51", // Avg Time 200
                "002D", // Min Time 400
                "0001", // Max Time 400
                "0E51", // Avg Time 400
                "E4A2" //Cycles
                );
        }

        public string CheckTightness(IProgress<string> progress)
        {
            DoProgress(progress);

            Thread.Sleep(3000);

            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                "0000", //First Error Code
                "0000", //Second Error Code
                "0000", //Third Error Code,
                "002D", // Max Pressure Begin
                "0001", // Max Pressure End
                "0E51", // Max Pressure Difference
                "002D", // Min Pressure Begin
                "0001", // Min Pressure End
                "0000", // Min Pressure Difference
                "002D", // Filltime 400
                "0001" // Bubbletime
                );
        }

        public bool SendInfo(IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        public bool SendSwitchToTTC(IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        internal byte[] CurrentStatistic { get; set; }

        public IEnumerable<string> SerialPorts => new[] {"COM1", "COM2", "COM1220"};
    }
}
