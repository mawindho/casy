using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Com.Test;
using OLS.Casy.Controller.Api;
using OLS.Casy.Controller.Calibration;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Test.Mock;

namespace OLS.Casy.Controller.Test
{
    [TestClass]
    public class CalibrationControllerTest
    {
        private Mock<IProgress<string>> _progressMock;
        private SerialPortMock _serialPortMock;
        private Mock<IConfigService> _configServiceMock;
        private Mock<IFileSystemStorageService> _fileSystemStorageServiceMock;
        private Mock<IEnvironmentService> _environmentServiceMock;

        private MockedCasySerialPortDriverProvider _casySerialPortDriverProvider;

        [TestInitialize]
        public void Initialize()
        {
            this._progressMock = new Mock<IProgress<string>>();
            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            this._configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            this._configServiceMock.Setup(mock => mock.InitializeByConfiguration(It.IsAny<object>())).Callback((object o) =>
            {
                if (o is CalibrationController)
                {
                    ((CalibrationController) o).CalibrationFileDirectory = "Calibration";
                }
            });

            this._fileSystemStorageServiceMock = Casy.Test.Mock.Mocks.FileSystemStorageServiceMock;
            this._fileSystemStorageServiceMock.Setup(mock => mock.GetFiles(It.IsAny<string>())).Returns((string dirPath) =>
            {
                return new[] { "K000_003.045", "K000_005.060", "K000_015.150", "K000_120.150" };
            });
            this._fileSystemStorageServiceMock.Setup(mock => mock.ReadFileAsync(It.IsAny<string>())).Returns((string filePath) =>
            {
                var fileName = Path.GetFileName(filePath);
                return Task.Factory.StartNew<byte[]>(() => File.ReadAllBytes(string.Format(@"TestCalibration\{0}", fileName)));
            });

            this._environmentServiceMock = Casy.Test.Mock.Mocks.EnvironmentServiceMock;

            _serialPortMock = Casy.Test.Mock.Mocks.SerialPortMock;
            _casySerialPortDriverProvider = new MockedCasySerialPortDriverProvider(_serialPortMock);
        }

        [TestMethod]
        public void PrepareController_Test()
        {
            ManualResetEvent eventAwaiter = new ManualResetEvent(false);
            bool calibrationDataLoadedRaised = false;

            var calibrationController = this.GetCalibrationController();
            calibrationController.CaibrationDataLoadedEvent += (s, e) =>
            {
                calibrationDataLoadedRaised = true;
                eventAwaiter.Set();
            };
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            Assert.AreEqual(4, calibrationController.KnownCalibrationNames.Count());
            Assert.IsTrue(calibrationController.KnownCalibrationNames.Contains("K000_003.045"));
            Assert.IsTrue(calibrationController.KnownCalibrationNames.Contains("K000_005.060"));
            Assert.IsTrue(calibrationController.KnownCalibrationNames.Contains("K000_015.150"));
            Assert.IsTrue(calibrationController.KnownCalibrationNames.Contains("K000_120.150"));

            Assert.AreEqual(3, calibrationController.KnownCappillarySizes.Count());
            
            Assert.AreEqual(1, calibrationController.GetDiametersByCappillarySize(45).Count());
            Assert.AreEqual(3, calibrationController.GetDiametersByCappillarySize(45).First());

            Assert.AreEqual(1, calibrationController.GetDiametersByCappillarySize(60).Count());
            Assert.AreEqual(5, calibrationController.GetDiametersByCappillarySize(60).First());

            Assert.AreEqual(2, calibrationController.GetDiametersByCappillarySize(150).Count());
            CollectionAssert.Contains(calibrationController.GetDiametersByCappillarySize(150).ToArray(), 15);
            CollectionAssert.Contains(calibrationController.GetDiametersByCappillarySize(150).ToArray(), 120);

            eventAwaiter.WaitOne(5000);

            Assert.IsTrue(calibrationDataLoadedRaised);
        }

        [TestMethod]
        public void GetDiametersByCappillarySize_PassValidValues()
        {
            var calibrationController = this.GetCalibrationController();
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            var diameters = calibrationController.GetDiametersByCappillarySize(0);
            Assert.AreEqual(0, diameters.Count());

            diameters = calibrationController.GetDiametersByCappillarySize(45);
            Assert.AreEqual(1, diameters.Count());

            diameters = calibrationController.GetDiametersByCappillarySize(60);
            Assert.AreEqual(1, diameters.Count());

            diameters = calibrationController.GetDiametersByCappillarySize(150);
            Assert.AreEqual(2, diameters.Count());
        }

        [TestMethod]
        public void TransferCalibration_PassNull()
        {
            var calibrationController = this.GetCalibrationController();
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            try
            {
                var result = calibrationController.TransferCalibration(this._progressMock.Object, null, false);
            }
            catch(ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "measureSetup");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void TransferCalibration_PassValidData()
        {
            var calibrationController = this.GetCalibrationController();
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            var result = calibrationController.TransferCalibration(this._progressMock.Object, null, true);
            this._fileSystemStorageServiceMock.Verify(mock => mock.ReadFileAsync("K000_003.045"));
            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", result);

            MeasureSetup measureSetup = new MeasureSetup();
            measureSetup.CapillarySize = 0;
            measureSetup.ToDiameter = 0;

            bool exceptionCought = false;
            try
            {
                result = calibrationController.TransferCalibration(this._progressMock.Object, measureSetup, false);
            }
            catch(ArgumentException e)
            {
                exceptionCought = true;
                Assert.AreEqual("Can't find calibration with name: K000_000.000", e.Message);
            }

            if(!exceptionCought)
            {
                Assert.Fail("No exception was thrown");
            }

            measureSetup.CapillarySize = 150;
            measureSetup.ToDiameter = 15;
            result = calibrationController.TransferCalibration(this._progressMock.Object, measureSetup, false);
            this._fileSystemStorageServiceMock.Verify(mock => mock.ReadFileAsync("K000_003.045"));
            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", result);
        }

        [TestMethod]
        public void VerifyActiveCalibration_PassValidData()
        {
            var calibrationController = this.GetCalibrationController();
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            MeasureSetup measureSetup = new MeasureSetup();
            measureSetup.CapillarySize = 150;
            measureSetup.ToDiameter = 15;
            calibrationController.TransferCalibration(this._progressMock.Object, measureSetup, false);

            this._casySerialPortDriverProvider.SerialPortMock.WrittenValues.Clear();
            this._casySerialPortDriverProvider.SerialPortMock.ReadValues.Clear();

            var result = calibrationController.VerifyActiveCalibration(this._progressMock.Object);

            Assert.IsTrue(result);
            Assert.AreEqual(1, this._casySerialPortDriverProvider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.WrittenValues, "!56\r");
            Assert.AreEqual(1, this._casySerialPortDriverProvider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._casySerialPortDriverProvider.SerialPortMock.ReadValues, "!56 OK\n\r\"56,\u000f\0\0–\0\0\0\0\n\r");
        }

        [TestMethod]
        public void GetVolumeCorrectionAsync_PassValidData()
        {
            var calibrationController = this.GetCalibrationController();
            Assert.IsTrue(this._casySerialPortDriverProvider.CasySerialPortDriver.IsConnected);

            calibrationController.Prepare(this._progressMock.Object);

            double result;
            bool exceptionCought = false;
            MeasureSetup measureSetup = null;

            try
            {
                result = calibrationController.GetVolumeCorrection(measureSetup);
            }
            catch (ArgumentNullException e)
            {
                exceptionCought = true;
                Assert.AreEqual("measureSetup", e.ParamName);
            }

            if (!exceptionCought)
            {
                Assert.Fail("No exception was thrown");
            }

            exceptionCought = false;
            measureSetup = new MeasureSetup();
            measureSetup.CapillarySize = 0;
            measureSetup.ToDiameter = 0;
                        
            try
            {
                result = calibrationController.GetVolumeCorrection(measureSetup);
            }
            catch (ArgumentException e)
            {
                exceptionCought = true;
                Assert.AreEqual("Can't find calibration with name: K000_000.000", e.Message);
            }

            measureSetup.CapillarySize = 150;
            measureSetup.ToDiameter = 15;
            measureSetup.Volume = Models.Enums.Volumes.TwoHundred;

            result = calibrationController.GetVolumeCorrection(measureSetup);
            Assert.AreEqual(10600d, result);

            measureSetup.Volume = Models.Enums.Volumes.FourHundred;

            result = calibrationController.GetVolumeCorrection(measureSetup);
            Assert.AreEqual(10399d, result);
        }

        private CalibrationController GetCalibrationController()
        {
            this._casySerialPortDriverProvider.Prepare(this._progressMock);
            this._casySerialPortDriverProvider.SerialPortMock.WrittenValues.Clear();
            this._casySerialPortDriverProvider.SerialPortMock.ReadValues.Clear();

            /*CalibrationController calibrationController = new CalibrationController(
                this._configServiceMock.Object,
                this._fileSystemStorageServiceMock.Object,
                this._casySerialPortDriverProvider.CasySerialPortDriver,
                this._environmentServiceMock.Object);

            return calibrationController;*/
            return null;
        }
    }
}
