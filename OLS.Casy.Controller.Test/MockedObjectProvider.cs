using Moq;
using OLS.Casy.Com.Test;
using OLS.Casy.Controller.Calibration;
using OLS.Casy.Controller.Error;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Test
{
    public class MockedCalibrationControllerProvider
    {
        public MockedCalibrationControllerProvider(MockedCasySerialPortDriverProvider mockedCasySerialPortDriverProvider)
        {
            var configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            configServiceMock.Setup(mock => mock.InitializeByConfiguration(It.IsAny<object>())).Callback((object o) =>
            {
                if (o is CalibrationController)
                {
                    ((CalibrationController)o).CalibrationFileDirectory = "Calibration";
                }
            });

            var fileSystemStorageServiceMock = Casy.Test.Mock.Mocks.FileSystemStorageServiceMock;
            fileSystemStorageServiceMock.Setup(mock => mock.GetFiles(It.IsAny<string>())).Returns((string dirPath) =>
            {
                return new[] { "K000_003.045", "K000_005.060", "K000_015.150", "K000_120.150" };
            });
            fileSystemStorageServiceMock.Setup(mock => mock.ReadFileAsync(It.IsAny<string>())).Returns((string filePath) =>
            {
                var fileName = Path.GetFileName(filePath);
                return Task.Factory.StartNew<byte[]>(() => File.ReadAllBytes(string.Format(@"TestCalibration\{0}", fileName)));
            });

            //this.CalibrationController = new CalibrationController(
            //    configServiceMock.Object,
            //    fileSystemStorageServiceMock.Object,
            //    mockedCasySerialPortDriverProvider.CasySerialPortDriver,
            //    Casy.Test.Mock.Mocks.EnvironmentServiceMock.Object);

            var progressMock = new Mock<IProgress<string>>();
            progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            this.CalibrationController.Prepare(progressMock.Object);
        }

        public CalibrationController CalibrationController { get; private set; }
    }

    public class MockedErrorControllerProvider
    {
        private List<ErrorDetails> _errorDetails;

        public MockedErrorControllerProvider()
        {
            this._errorDetails = new List<ErrorDetails>();

            var configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            var databaseStorageServiceMock = new Mock<IDatabaseStorageService>();
            databaseStorageServiceMock.Setup(mock => mock.GetErrorDetails()).Returns(() => this._errorDetails);
            databaseStorageServiceMock.Setup(mock => mock.SaveErrorDetails(It.IsAny<ErrorDetails>())).Callback((ErrorDetails errorDetails) =>
            {
                this._errorDetails.Add(errorDetails);
            });

            ErrorController = new ErrorController(
                configServiceMock.Object,
                databaseStorageServiceMock.Object);
        }

        public ErrorController ErrorController { get; private set; }
    }
}
