using Moq;
using OLS.Casy.Com;
using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Monitoring.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Test.Mock
{
    public class Mocks
    {
        //private static Mock<IConfigService> _configServiceMock = new Mock<IConfigService>();
        //private static Mock<ILogger> _loggerMock = new Mock<ILogger>();
        //private static Mock<ILocalizationService> _localizationServiceMock = new Mock<ILocalizationService>();
        //private static Mock<IMeasureCounter> _measureConterMock = new Mock<IMeasureCounter>();
        private static SerialPortMock _serialPortMock;
        //private static Mock<IErrorContoller> _errorControllerMock = new ErrorControllerMock();
        //private static Mock<IEventAggregatorProvider> _eventAggregatorProviderMock = new EventAggregatorProviderMock();

        public static Mock<IConfigService> ConfigServiceMock
        {
            get { return new ConfigServiceMock(); }
        }

        public static Mock<ILogger> LoggerMock
        {
            get { return new LoggerMock(); }
        }

        public static Mock<ILocalizationService> LocalizationServiceMock
        {
            get { return new LocalizationServiceMock(); }
        }

        public static Mock<IMeasureCounter> MeasureCounterMock
        {
            get { return new MeasureCounterMock(); }
        }

        public static Mock<IErrorContoller> ErrorControllerMock
        {
            get { return new ErrorControllerMock(); }
        }

        public static SerialPortMock SerialPortMock
        {
            get { return new SerialPortMock();}
        }

        public static EventAggregatorProviderMock EventAggregatorProviderMock
        {
            get { return new EventAggregatorProviderMock(); }
        }

        public static Mock<IEnvironmentService> EnvironmentServiceMock
        {
            get { return new EnvironmentServiceMock(); }
        }

        public static Mock<IFileSystemStorageService> FileSystemStorageServiceMock
        {
            get { return new FileSystemStorageServiceMock(); }
        }

        public static Mock<IMonitoringService> MonitoringServiceMock
        {
            get { return new MonitoringServiceMock(); }
        }
    }
}
