using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Core.Config.Api;
using System.Collections.Generic;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;

namespace OLS.Casy.Controller.Error.Test
{
    /// <summary>
    /// Unit tests for the error controller
    /// </summary>
    [TestClass]
    public class ErrorControllerUnitTest
    {
        /// <summary>
        /// General test 
        /// </summary>
        [TestMethod]
        public void TestParseError()
        {
            Mock<IConfigService> configServiceMock = new Mock<IConfigService>();
            Mock<IDatabaseStorageService> databaseStorageServiceMock = new Mock<IDatabaseStorageService>();

            List<ErrorDetails> errorDetails = new List<ErrorDetails>();
            errorDetails.Add(new ErrorDetails() { ErrorCode = "0000-0000-0001" });
            errorDetails.Add(new ErrorDetails() { ErrorCode = "0000-0000-0100" });
            errorDetails.Add(new ErrorDetails() { ErrorCode = "0000-0001-0000" });

            databaseStorageServiceMock.Setup(channel => channel.GetErrorDetails()).Returns(errorDetails);

            ErrorController errorController = new ErrorController(configServiceMock.Object, databaseStorageServiceMock.Object);
            errorController.OnImportsSatisfied();

            var errorResult = errorController.ParseError("0000,0001,0101");
            Assert.IsNotNull(errorResult);
            Assert.AreEqual(ErrorResultType.MutipleError, errorResult.ErrorResultType);
            Assert.IsTrue(errorResult.FatalErrorDetails.Count + errorResult.SoftErrorDetails.Count == 3);
        }
    }
}
