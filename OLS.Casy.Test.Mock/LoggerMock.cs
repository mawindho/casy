using Moq;
using OLS.Casy.Core.Logging.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Test.Mock
{
    public class LoggerMock : Mock<ILogger>
    {
        public LoggerMock()
        {
            //this.Setup(logger => logger.Debug(It.IsAny<string>(), It.IsAny<Expression<Action>>()));
            //this.Setup(logger => logger.Info(It.IsAny<string>()));
        }
    }
}
