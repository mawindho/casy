using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Calculation.Smooth.Test
{
    [TestClass]
    public class SmootingCalculationProviderTests
    {
        private SmootingCalculationProvider _smootingCalculationProvider;

        [TestInitialize]
        public void Initialize()
        {
            this._smootingCalculationProvider = new SmootingCalculationProvider();
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._smootingCalculationProvider = null;
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNull_ShouldArgumentNullException()
        {
            try
            {
                var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(null);
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
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { });
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassArrayOnlyZero()
        {
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 0d });
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData_NoWidth()
        {
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1.1875d, 2.375d, 0d, 4.75d, 3.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(1.1875, result[0]);
            Assert.AreEqual(2.375, result[1]);
            Assert.AreEqual(0d, result[2]);
            Assert.AreEqual(4.75, result[3]);
            Assert.AreEqual(3.5625, result[4]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData_NegativeWidth()
        {
            _smootingCalculationProvider.Width = -1;
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1.1875d, 2.375d, 0d, 4.75d, 3.5625d });
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(1.1875, result[0]);
            Assert.AreEqual(2.375, result[1]);
            Assert.AreEqual(0d, result[2]);
            Assert.AreEqual(4.75, result[3]);
            Assert.AreEqual(3.5625, result[4]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNegativeData()
        {
            _smootingCalculationProvider.Width = 3;
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { -1d, -4d, -7d, -4d, -1d, -4d, -7d, -4d, -1d });
            Assert.AreEqual(9, result.Length);
            Assert.AreEqual(-2.5, result[0]);
            Assert.AreEqual(-4d, result[1]);
            Assert.AreEqual(-5d, result[2]);
            Assert.AreEqual(-4d, result[3]);
            Assert.AreEqual(-3d, result[4]);
            Assert.AreEqual(-4d, result[5]);
            Assert.AreEqual(-5d, result[6]);
            Assert.AreEqual(-4d, result[7]);
            Assert.AreEqual(-2.5, result[8]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData()
        {
            _smootingCalculationProvider.Width = 2;
            var result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1d, 4d, 7d, 4d, 1d, 4d, 7d, 4d, 1d });
            Assert.AreEqual(9, result.Length);
            Assert.AreEqual(2.5, result[0]);
            Assert.AreEqual(4d, result[1]);
            Assert.AreEqual(5d, result[2]);
            Assert.AreEqual(4d, result[3]);
            Assert.AreEqual(3d, result[4]);
            Assert.AreEqual(4d, result[5]);
            Assert.AreEqual(5d, result[6]);
            Assert.AreEqual(4d, result[7]);
            Assert.AreEqual(2.5, result[8]);

            _smootingCalculationProvider.Width = 3;
            result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1d, 4d, 7d, 4d, 1d, 4d, 7d, 4d, 1d });
            Assert.AreEqual(9, result.Length);
            Assert.AreEqual(2.5, result[0]);
            Assert.AreEqual(4d, result[1]);
            Assert.AreEqual(5d, result[2]);
            Assert.AreEqual(4d, result[3]);
            Assert.AreEqual(3d, result[4]);
            Assert.AreEqual(4d, result[5]);
            Assert.AreEqual(5d, result[6]);
            Assert.AreEqual(4d, result[7]);
            Assert.AreEqual(2.5, result[8]);

            _smootingCalculationProvider.Width = 5;
            result = this._smootingCalculationProvider.TransformMeasureResultDataBlock(new double[] { 1d, 4d, 7d, 4d, 1d, 4d, 7d, 4d, 1d });
            Assert.AreEqual(9, result.Length);
            Assert.AreEqual(4d, result[0]);
            Assert.AreEqual(4d, result[1]);
            Assert.AreEqual(3.4d, result[2]);
            Assert.AreEqual(4d, result[3]);
            Assert.AreEqual(4.6d, result[4]);
            Assert.AreEqual(4d, result[5]);
            Assert.AreEqual(3.4d, result[6]);
            Assert.AreEqual(4d, result[7]);
            Assert.AreEqual(4d, result[8]);
        }
    }
}
