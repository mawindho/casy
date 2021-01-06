using Moq;
using OLS.Casy.Core.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Test.Mock
{
    public class EnvironmentServiceMock : Mock<IEnvironmentService>
    {
        public EnvironmentServiceMock()
        {
            this.Setup(mock => mock.GetExecutionPath()).Returns(() =>
            {
                return @"C:\Test\OLS.Casy.AppService.exe";
            });
        }
    }
}
