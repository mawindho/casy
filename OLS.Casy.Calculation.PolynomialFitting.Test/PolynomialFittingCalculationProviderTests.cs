using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Calculation.PolynomialFitting.Test
{
    [TestClass]
    public class PolynomialFittingCalculationProviderTests
    {
        private PolynomialFittingCalculationProvider _polynomialFittingCalculationProvider;

        [TestInitialize]
        public void Initialize()
        {
            this._polynomialFittingCalculationProvider = new PolynomialFittingCalculationProvider();
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._polynomialFittingCalculationProvider = null;
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNull_ShouldArgumentNullException()
        {
            try
            {
                var result = this._polynomialFittingCalculationProvider.TransformMeasureResultDataBlock(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "dataBlock");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassEmptyArray()
        {
            var result = this._polynomialFittingCalculationProvider.TransformMeasureResultDataBlock(new double[] { });
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassArrayOnlyZero()
        {
            var result = this._polynomialFittingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 0d });
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData()
        {
            var result = this._polynomialFittingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1.1875d, 2.375d, 0d, 4.75d, 3.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(2.375, result[0]);
            Assert.AreEqual(2.375, result[1]);
            Assert.AreEqual(2.375, result[2]);
            Assert.AreEqual(2.375, result[3]);
            Assert.AreEqual(2.375, result[4]);
        }
    }
}
