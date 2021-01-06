using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class SwapHelperTest
    {
        [TestMethod]
        public void SwapBytes_PassValidData()
        {
            ushort uShort = 4;
            var uShortResult = SwapHelper.SwapBytes(uShort);
            Assert.AreEqual(1024, uShortResult);

            uint uInt = 4;
            var uIntResult = SwapHelper.SwapBytes(uInt);
            Assert.AreEqual((uint) 67108864, uIntResult);

            int data = 4;
            var intResult = SwapHelper.SwapBytes(data);
            Assert.AreEqual(67108864, intResult);
        }
    }
}
