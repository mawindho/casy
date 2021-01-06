using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLS.Casy.Models;

namespace OLS.Casy.Calculation.Volume.Test
{
    [TestClass]
    public class VolumeCalculationProviderTests
    {
        private VolumeCalculationProvider _volumeCalculationProvider;

        [TestInitialize]
        public void Initialize()
        {
            this._volumeCalculationProvider = new VolumeCalculationProvider();
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._volumeCalculationProvider = null;
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNullMeasureSetup_ShouldArgumentNullException()
        {
            try
            {
                var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(null, new double[] { 0d });
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "measureSetup");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassNullDataBlock_ShouldArgumentNullException()
        {
            try
            {
                var measureSetup = new MeasureSetup();
                var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "dataBlock");
                return;
            }
            Assert.Fail("No exception was thrown");
        }


        [TestMethod]
        public void TransformMeasureResultDataBlock_PassMeasureSetupNoVolumeMapping_ShouldArgumentNullException()
        {
            try
            {
                var measureSetup = new MeasureSetup();
                var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, new double[] { 0d });
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "measureSetup.VolumeMapping");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassEmptyArray()
        {
            var measureSetup = new MeasureSetup();
            var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, new double[] { });
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassArrayOnlyZero()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.ToDiameter = 20;
            var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, new double[] { 0d });
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0]);
        }

        [TestMethod]
        public void TransformMeasureResultDataBlock_PassValidData()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.ToDiameter = 20;
            var result = this._volumeCalculationProvider.TransformMeasureResultDataBlock(measureSetup, new double[] { 1d, 2d, -3d, 5.6, -3.12 });
            Assert.AreEqual(5, result.Length);
            Assert.IsTrue(result[0] == 0d);
            Assert.IsTrue(result[1] == 2.6332525426808871E-05);
            Assert.IsTrue(result[2] == -0.00018286475990839495);
            Assert.IsTrue(result[3] == 0.0009366576822187867);
            Assert.IsTrue(result[4] == -0.0011091259709771896);
        }
    }
}
