using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Calculation.Normalization.Test
{
    [TestClass]
    public class NormalizationCalculationProviderTests
    {
        private NormalizationCalculationProvider _normalizationCalculationProvider;

        [TestInitialize]
        public void Initialize()
        {
            this._normalizationCalculationProvider = new NormalizationCalculationProvider();
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._normalizationCalculationProvider = null;
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNull_ShouldArgumentNullException()
        {
            try
            {
                var result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(null);
            }
            catch(ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "dataBlock");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassEmptyArray()
        {
            var result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(new double[] { });
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassArrayOnlyZero()
        {
            var result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(new double[] { 0d });
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNegativeData()
        {
            var result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(new double[] { -1.1875d, -2.375d, 0d, -4.75d, -3.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(75d, result[0]);
            Assert.AreEqual(50d, result[1]);
            Assert.AreEqual(100d, result[2]);
            Assert.AreEqual(0d, result[3]);
            Assert.AreEqual(25d, result[4]);

            result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(new double[] { -2.1875d, -3.375d, -1d, -5.75d, -4.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(75d, result[0]);
            Assert.AreEqual(50d, result[1]);
            Assert.AreEqual(100d, result[2]);
            Assert.AreEqual(0d, result[3]);
            Assert.AreEqual(25d, result[4]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData()
        {
            var result = this._normalizationCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1.1875d, 2.375d, 0d, 4.75d, 3.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(25d, result[0]);
            Assert.AreEqual(50d, result[1]);
            Assert.AreEqual(0d, result[2]);
            Assert.AreEqual(100d, result[3]);
            Assert.AreEqual(75d, result[4]);
        }
    }
}
