using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLS.Casy.Core;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class CalculationsTest
    {
        [TestMethod]
        public void CalcChannel_MaxChannelLowerZero_ShallArgumentOutOfRangeException()
        {
            try
            {
                var result = Calculations.CalcChannel(1, 64, 0d, 0);
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.Equals(e.ParamName, "maxChannel");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcChannel_EqualToAndFromDiameter_ShallDevideByZeroExcption()
        {
            try
            {
                var result = Calculations.CalcChannel(1, 1, 0d, 1024);
            }
            catch (DivideByZeroException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcChannel_PassNegativeSmoothedDiameter_ShallReturnZero()
        {
            var result = Calculations.CalcChannel(0, 64, -1d, 1024);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void CalcChannel_PassValueHigherMaxChannel_ShallReturnMaxChannelMinusOne()
        {
            var result = Calculations.CalcChannel(0, 64, 65, 1024);
            Assert.AreEqual(1023, result);

            result = Calculations.CalcChannel(0, 64, 64, 1024);
            Assert.AreEqual(1023, result);

            result = Calculations.CalcChannel(0, 64, 63, 1024);
            Assert.AreNotEqual(1023, result);
        }

        [TestMethod]
        public void CalcChannel_PassValidValues()
        {
            var result = Calculations.CalcChannel(0, 64, 0, 1024);
            Assert.AreEqual(0, result);

            result = Calculations.CalcChannel(0, 64, 1, 1024);
            Assert.AreEqual(1024 / 64, result);

            result = Calculations.CalcChannel(0, 64, 64, 1024);
            Assert.AreEqual(1024-1, result);

            result = Calculations.CalcChannel(0, 64, 1.15, 1024);
            Assert.AreEqual(18, result);

            result = Calculations.CalcChannel(0, 64, 1.16, 1024);
            Assert.AreEqual(19, result);
        }

        [TestMethod]
        public void CalcSmoothedDiameter_MaxChannelLowerZero_ShallArgumentOutOfRangeException()
        {
            try
            {
                var result = Calculations.CalcSmoothedDiameter(1, 64, 0, 0);
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.Equals(e.ParamName, "maxChannel");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcSmoothedDiameter_EqualToAndFromDiameter_ShallDevideByZeroExcption()
        {
            try
            {
                var result = Calculations.CalcSmoothedDiameter(1, 1, 0, 1024);
            }
            catch (DivideByZeroException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcSmoothedDiameter_PassZeroOrNegativeChannel_ShallReturnZero()
        {
            var result = Calculations.CalcSmoothedDiameter(0, 64, 0, 1024);
            Assert.AreEqual(0, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, -1, 1024);
            Assert.AreEqual(0, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, -10, 1024);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void CalcSmoothedDiameter_PassValueHigherMaxChannelMinusOne_ShallReturnToDiameter()
        {
            var result = Calculations.CalcSmoothedDiameter(0, 64, 1024, 1024);
            Assert.AreEqual(64, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, 1023, 1024);
            Assert.AreEqual(64, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, 1022, 1024);
            Assert.AreNotEqual(64, result);
        }

        [TestMethod]
        public void CalcSmoothedDiameter_PassValidValues()
        {
            var result = Calculations.CalcSmoothedDiameter(0, 64, 0, 1024);
            Assert.AreEqual(0, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, 1, 1024);
            Assert.AreEqual(0.09375, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, 64, 1024);
            Assert.AreEqual(4.03125, result);

            result = Calculations.CalcSmoothedDiameter(0, 64, 18, 1024);
            Assert.AreEqual(1.15625, result);
        }

        [TestMethod]
        public void CalcRelativeDiviation_PassNull_ShallArgumentNullException()
        {
            try
            {
                var result = Calculations.CalcRelativeDeviation(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "values");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcRelativeDiviation_PassEmptyArray_ShallReturnZero()
        {
            var result = Calculations.CalcRelativeDeviation(new double[] { });
            Assert.AreEqual(0d, result);
        }

        [TestMethod]
        public void CalcRelativeDiviation_PassOnlyZeros_ShallReturnZero()
        {
            var result = Calculations.CalcRelativeDeviation(new double[] { 0d });
            Assert.AreEqual(0d, result);

            result = Calculations.CalcRelativeDeviation(new double[] { 0d, 0d, 0d, 0d });
            Assert.AreEqual(0d, result);
        }

        [TestMethod]
        public void CalcRelativeDiviation_PassValidValues()
        {
            var result = Calculations.CalcRelativeDeviation(new double[] { 1d, 2d, 3d, 4d, 5d });
            Assert.AreEqual(52.70, Math.Round(result, 2));
        }

        [TestMethod]
        public void CalcDeviation_PassNull_ShallArgumentNullException()
        {
            try
            {
                var result = Calculations.CalcDeviation(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "values");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcDeviation_PassEmptyArray_ShallReturnZero()
        {
            var result = Calculations.CalcDeviation(new double[] { });
            Assert.AreEqual(0d, result);
        }

        [TestMethod]
        public void CalcDeviation_PassOnlyZeros_ShallReturnZero()
        {
            var result = Calculations.CalcDeviation(new double[] { 0d });
            Assert.AreEqual(0d, result);

            result = Calculations.CalcDeviation(new double[] { 0d, 0d, 0d, 0d });
            Assert.AreEqual(0d, result);
        }

        [TestMethod]
        public void CalcDeviation_PassValidValues()
        {
            var result = Calculations.CalcDeviation(new double[] { 1d, 2d, 3d, 4d, 5d });
            Assert.AreEqual(1.58113883008, Math.Round(result, 11));
        }

        [TestMethod]
        public void CalcEffectiveMl_PassLessOneRepeats_ShallArgumentException()
        {
            try
            {
                var result = Calculations.CalcEffectiveMl(1d, 1d, 1d, 0);
            }
            catch (ArgumentException e)
            {
                StringAssert.Equals(e.ParamName, "repeats");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcEffectiveMl_PassZeroVolume_ShallDivideByZeroException()
        {
            try
            {
                var result = Calculations.CalcEffectiveMl(0d, 1d, 1d, 1);
            }
            catch (DivideByZeroException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcEffectiveMl_PassValidValues()
        {
            var result = Calculations.CalcEffectiveMl(1000d, 10000d, 1d, 1);
            Assert.AreEqual(1d, result);
        }

        [TestMethod]
        public void CalcChecksum_PassNull_ShallArgumentNullException()
        {
            try
            {
                var result = Calculations.CalcChecksum(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "source");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcChecksum_PassValidValues()
        {
            var result = Calculations.CalcChecksum(new byte[] { });
            Assert.AreEqual((uint)0, result);

            result = Calculations.CalcChecksum(new byte[] { 1, 2, 4, 5 });
            Assert.AreEqual((uint)12, result);
        }
    }
}
