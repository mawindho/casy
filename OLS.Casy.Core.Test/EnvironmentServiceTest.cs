using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class EnvironmentServiceTest
    {
        [TestMethod]
        public void Construction()
        {
            var environmentService = new EnvironmentService();

            var info = environmentService.GetEnvironmentInfo("MachineName");
            Assert.IsNotNull(info);
            Assert.IsInstanceOfType(info, typeof(string));
            Assert.AreEqual(Environment.MachineName, (string)info);
        }

        [TestMethod]
        public void SetEnvironmentInfo_PassKeyNull_ShallArgumentNullExcption()
        {
            var environmentService = new EnvironmentService();
            try
            {
                environmentService.SetEnvironmentInfo(null, null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "key");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void GetEnvironmentInfo_PassKeyNull_ShallArgumentNullExcption()
        {
            var environmentService = new EnvironmentService();
            try
            {
                environmentService.GetEnvironmentInfo(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "key");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void GetSetEnvironmentInfo_PassValidValues()
        {
            ManualResetEvent eventAwaiter = new ManualResetEvent(false);
            string eventKey = null;

            var environmentService = new EnvironmentService();
            environmentService.EnvironmentInfoChangedEvent += (sender, key) =>
            {
                eventKey = key;
                eventAwaiter.Set();
            };

            var info = environmentService.GetEnvironmentInfo("key");
            Assert.IsNull(info);

            environmentService.SetEnvironmentInfo("key", "value");
            eventAwaiter.WaitOne(1000, false);

            Assert.IsNotNull(eventKey);
            StringAssert.Equals(eventKey, "key");

            info = environmentService.GetEnvironmentInfo(eventKey);
            Assert.IsNotNull(info);
            Assert.IsInstanceOfType(info, typeof(string));
            Assert.AreEqual((string)info, "value");

            eventAwaiter.Reset();
            eventKey = null;

            environmentService.SetEnvironmentInfo("key", 12345);
            eventAwaiter.WaitOne(1000, false);

            Assert.IsNotNull(eventKey);
            StringAssert.Equals(eventKey, "key");

            info = environmentService.GetEnvironmentInfo(eventKey);
            Assert.IsNotNull(info);
            Assert.IsInstanceOfType(info, typeof(int));
            Assert.AreEqual((int)info, 12345);
        }
    }
}
