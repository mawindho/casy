using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLS.Casy.Authorization.Emergency;
using System;

namespace OLS.Casy.Core.Authorization.Emergency.Test
{
    [TestClass]
    public class SuperPasswordProviderTest
    {
        [TestMethod]
        public void Test1()
        {
            SuperPasswordProvider spp = new SuperPasswordProvider();
            var password = spp.GenerateSuperPassword("TTC-2BA-1015", DateTime.Now);

        }
    }
}
