using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.ImportExport.TT;
using System.IO;
using System.Threading.Tasks;
using static OLS.Casy.IO.ImportExport.TT.Interpolations;

namespace OLS.Casy.IO.ImportExport.CRF.Test
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public async void Test1()
        {
            Mock<IFileSystemStorageService> storageProviderMock = new Mock<IFileSystemStorageService>();
            storageProviderMock.Setup(service => service.ReadFileAsync(It.IsAny<string>())).Returns<string>(ReadFileTest);

            Mock<ILogger> loggerMock = new Mock<ILogger>();
            //loggerMock.Setup(logger => logger.Info(It.IsAny<string>()));

            //CRFImportExportProvider provider = new CRFImportExportProvider(storageProviderMock.Object, loggerMock.Object);
            //await provider.ImportAsync(@"C:\Projekte\Casy\Data\CRF_Testfiles\1.CRF", string.Empty);
        }

        private Task<byte[]> ReadFileTest(string arg)
        {
            return Task.Factory.StartNew(() =>
            { 
                byte[] fileBytes = File.ReadAllBytes(arg);
                return fileBytes;
            }
            );
        }

        [TestMethod]
        async Task Test2()
        {
            Mock<IFileSystemStorageService> storageProviderMock = new Mock<IFileSystemStorageService>();
            storageProviderMock.Setup(service => service.ReadFileAsync(It.IsAny<string>())).Returns<string>(ReadFileTest);

            Mock<ILogger> loggerMock = new Mock<ILogger>();
            //loggerMock.Setup(logger => logger.Info(It.IsAny<string>()));

            //TTImportExportProvider provider = new TTImportExportProvider(storageProviderMock.Object, loggerMock.Object);

            Point p0 = new Point(0, 17);
            Point p1 = new Point(1, 7);
            Point p2 = new Point(2, 12);
            Point p3 = new Point(3, 11);

            var a1 = Interpolations.Coeff(p0, p1);
            var a2 = Interpolations.Coeff(p0, p1, p2);
            var a3 = Interpolations.Coeff(p0, p1, p2, p3);
        }
    }
}
