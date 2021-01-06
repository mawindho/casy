using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Controller.Error;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Test
{
    [TestClass]
    public class ErrorControllerTest
    {
        private Mock<IProgress<string>> _progressMock;
        private Mock<IConfigService> _configServiceMock;
        private Mock<IDatabaseStorageService> _databaseStorageServiceMock;

        private List<ErrorDetails> _errorDetails;

        [TestInitialize]
        public void Initialize()
        {
            this._progressMock = new Mock<IProgress<string>>();
            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            this._errorDetails = new List<ErrorDetails>();

            this._configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            this._databaseStorageServiceMock = new Mock<IDatabaseStorageService>();
            this._databaseStorageServiceMock.Setup(mock => mock.GetErrorDetails()).Returns(() => this._errorDetails);
            this._databaseStorageServiceMock.Setup(mock => mock.SaveErrorDetails(It.IsAny<ErrorDetails>())).Callback((ErrorDetails errorDetails) =>
            {
                this._errorDetails.Add(errorDetails);
            });
        }

        [TestMethod]
        public void Prepare_Test()
        {
            var errorController = this.GetErrorController();

            Assert.IsTrue(errorController.ErrorDetails.Length == 0);

            errorController.OnImportsSatisfied();
            errorController.Prepare(this._progressMock.Object);

            Assert.IsTrue(errorController.ErrorDetails.Length == 48);
            Assert.IsTrue(this._errorDetails.Count == 48);
        }

        [TestMethod]
        public void ParseError_Test()
        {
            var errorController = this.GetErrorController();
            errorController.OnImportsSatisfied();
            errorController.Prepare(this._progressMock.Object);

            var result = errorController.ParseError(null);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.NoError);
            Assert.AreEqual(0, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError(string.Empty);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.NoError);
            Assert.AreEqual(0, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("Some random string");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.NoError);
            Assert.AreEqual(0, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0000,0000,0000");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.NoError);
            Assert.AreEqual(0, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0001,0000,0000");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.SingleError);
            Assert.AreEqual(1, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0002,0000,0000");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.SingleError);
            Assert.AreEqual(1, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0004,0000,0000");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.SingleError);
            Assert.AreEqual(1, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0003,0000,0000");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.MutipleError);
            Assert.AreEqual(2, result.SoftErrorDetails.Count);
            Assert.AreEqual(0, result.FatalErrorDetails.Count);

            result = errorController.ParseError("0001,0000,0001");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorResultType == Models.Enums.ErrorResultType.MutipleError);
            Assert.AreEqual(1, result.SoftErrorDetails.Count);
            Assert.AreEqual(1, result.FatalErrorDetails.Count);
        }

        private ErrorController GetErrorController()
        {
            ErrorController errorController = new ErrorController(
                this._configServiceMock.Object,
                this._databaseStorageServiceMock.Object);

            return errorController;
        }
    }
}
