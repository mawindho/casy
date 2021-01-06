using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Com.Api;
using OLS.Casy.Com.Test;
using OLS.Casy.Controller.Api;
using OLS.Casy.Controller.Calibration;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Test.Mock;

namespace OLS.Casy.Controller.Test
{
    [TestClass]
    public class CasyControllerTest
    {
        private MockedCasySerialPortDriverProvider _casySerialPortDriverProvider;
        private MockedTTSwitchServiceProvider _ttSwitchServiceProvider;
        private MockedErrorControllerProvider _errorControllerProvider;

        private SerialPortMock _serialPortMock;

        private Mock<IProgress<string>> _progressMock;
        private Mock<IConfigService> _configServiceMock;
        private Mock<IEnvironmentService> _environmentServiceMock;
        private Mock<ILocalizationService> _localizationServiceMock;

        private EventAggregatorProviderMock _eventAggregatorProviderMock;

        [TestInitialize]
        public void Initialize()
        {
            this._progressMock = new Mock<IProgress<string>>();
            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            this._configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            this._configServiceMock.Setup(mock => mock.InitializeByConfiguration(It.IsAny<CasyController>()));

            this._environmentServiceMock = Casy.Test.Mock.Mocks.EnvironmentServiceMock;

            this._eventAggregatorProviderMock = Casy.Test.Mock.Mocks.EventAggregatorProviderMock;
            this._serialPortMock = Casy.Test.Mock.Mocks.SerialPortMock;
            this._casySerialPortDriverProvider = new MockedCasySerialPortDriverProvider(_serialPortMock);
            this._ttSwitchServiceProvider = new MockedTTSwitchServiceProvider(_casySerialPortDriverProvider);
            this._localizationServiceMock = Casy.Test.Mock.Mocks.LocalizationServiceMock;
            this._errorControllerProvider = new MockedErrorControllerProvider();
        }

        [TestMethod]
        public void Prepare_Test()
        {
            var casyController = this.GetCasyController();
            casyController.OnImportsSatisfied();

            this._casySerialPortDriverProvider.SerialPortMock.WrittenValues.Clear();
            this._casySerialPortDriverProvider.SerialPortMock.ReadValues.Clear();
            
            this._casySerialPortDriverProvider.Prepare(this._progressMock);
            casyController.Prepare(this._progressMock.Object);
        }

        [TestMethod]
        public void StartSelfTest_Test()
        {
            var casyController = this.GetCasyController();

            this._localizationServiceMock.Setup(ls => ls.GetLocalizedString(It.IsAny<string>(), It.IsAny<string[]>())).Returns((string s, string[] paras) => s);

            casyController.StartSelfTest(true);
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!80\r");
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!81\r");
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!82\r");
            Assert.AreEqual(1, _eventAggregatorProviderMock.RaisedEventCounts[typeof(ShowLoginScreenEvent)]);

            this._casySerialPortDriverProvider.SerialPortMock.WrittenValues.Clear();

            casyController.StartSelfTest(false);
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!80\r");
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!81\r");
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!82\r");
            Assert.AreEqual(1, _eventAggregatorProviderMock.RaisedEventCounts[typeof(ShowLoginScreenEvent)]);
        }

        private CasyController GetCasyController()
        {
            var calibrationControllerMock = new MockedCalibrationControllerProvider(this._casySerialPortDriverProvider);
            return null;
            //CasyController casyController = new CasyController(
            //    this._configServiceMock.Object,
            //    this._casySerialPortDriverProvider.CasySerialPortDriver,
            //    this._errorControllerProvider.ErrorController,
            //    this._eventAggregatorProviderMock.Object,
            //    this._localizationServiceMock.Object,
            //    this._environmentServiceMock.Object,
            //    calibrationControllerMock.CalibrationController,
            //    this._ttSwitchServiceProvider.TTSwitchService,
            //    Casy.Test.Mock.Mocks.MonitoringServiceMock.Object);

            //return casyController;
        }
    }
}
