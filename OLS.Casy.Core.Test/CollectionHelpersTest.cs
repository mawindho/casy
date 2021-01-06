using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class CollectionHelpersTest
    {
        [TestMethod]
        public void SubArray_PassLengthBelowZero_ShallReturnNull()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            var result = array.SubArray(0, -1);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SubArray_PassLengthZero_ShallReturnEmptyArray()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            var result = array.SubArray(0, 0);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void SubArray_PassIndexBelowZero_ShallArgumentOutOfRangeException()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            try
            {
                var result = array.SubArray(-1, 0);
            }
            catch(ArgumentOutOfRangeException e)
            {
                StringAssert.Equals(e.ParamName, "index");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void SubArray_PassIndexBiggerLength_ShallArgumentOutOfRangeException()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            try
            {
                var result = array.SubArray(5, 1);
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.Equals(e.ParamName, "index");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void SubArray_PassLengthBiggerArrayLength_ShallArgumentOutOfRangeException()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            try
            {
                var result = array.SubArray(0, 6);
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.Equals(e.ParamName, "length");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void SubArray_PassValidIndex()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            var result = array.SubArray(0, 1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result[0]);

            result = array.SubArray(3, 1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(4, result[0]);
        }

        [TestMethod]
        public void SubArray_PassValidLength()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            var result = array.SubArray(0, 1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result[0]);

            result = array.SubArray(2, 2);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(3, result[0]);
            Assert.AreEqual(4, result[1]);
        }

        [TestMethod]
        public void UnorderedEqual_PassNull_ShallArgumentNullException()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            int[] toCompare = new int[] { 3, 5, 1, 2, 4 };
            try
            {
                var result = array.UnorderedEqual(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "b");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void UnorderedEqual_PassUnequalLength()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            int[] toCompare = new int[] { 3, 5, 1, 2 };

            Assert.IsFalse(array.UnorderedEqual(toCompare));
        }

        [TestMethod]
        public void UnorderedEqual_PassValidData()
        {
            int[] array = new int[] { 1, 2, 3, 4, 5 };
            int[] toCompare = new int[] { 3, 5, 1, 2, 4 };

            Assert.IsTrue(array.UnorderedEqual(toCompare));

            toCompare = new int[] { 3, 5, 2, 2, 4 };
            Assert.IsFalse(array.UnorderedEqual(toCompare));
        }
    }
}
