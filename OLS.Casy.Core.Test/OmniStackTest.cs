using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class OmniStackTest
    {
        [TestMethod]
        public void PushPop_PassValidData()
        {
            OmniStack<string> stack = new OmniStack<string>();
            Assert.AreEqual(0, stack.Count);

            stack.Push("Test1");
            Assert.AreEqual(1, stack.Count);

            stack.Push("Test2");
            Assert.AreEqual(2, stack.Count);

            var pop = stack.Pop();
            StringAssert.Equals(pop, "Test2");
            Assert.AreEqual(1, stack.Count);

            pop = stack.Pop();
            StringAssert.Equals(pop, "Test1");
            Assert.AreEqual(0, stack.Count);

            pop = stack.Pop();
            Assert.AreEqual(default(string), pop);

            stack.Push("Test1");
            stack.Push("Test2");
            Assert.AreEqual(2, stack.Count);

            stack.Remove("Test2");
            Assert.AreEqual(1, stack.Count);

            pop = stack.Pop();
            StringAssert.Equals(pop, "Test1");
            Assert.AreEqual(0, stack.Count);

            stack.Push("Test1");
            stack.Push("Test2");
            Assert.AreEqual(2, stack.Count);

            stack.Clear();
            Assert.AreEqual(0, stack.Count);
        }
    }
}
