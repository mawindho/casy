using Moq;
using OLS.Casy.Core.Config.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Test.Mock
{
    public class ConfigServiceMock : Mock<IConfigService>
    {
        public ConfigServiceMock()
        {
            //this.Setup(configService => configService.InitializeByConfiguration<T>(It.IsAny<T>()));
        }
    }
}
