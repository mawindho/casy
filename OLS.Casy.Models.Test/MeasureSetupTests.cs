using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Models.Test
{
    [TestClass]
    public class MeasureSetupTests
    {
        [TestMethod]
        public void Constructor_CorrectInitialization()
        {
            var measureSetup = new MeasureSetup();
            Assert.IsNotNull(measureSetup.Cursors);
            Assert.AreEqual(0, measureSetup.Cursors.Count);
            Assert.IsNotNull(measureSetup.DeviationControlItems);
            Assert.AreEqual(0, measureSetup.DeviationControlItems.Count);
            Assert.AreEqual(1024, measureSetup.ChannelCount);
            Assert.AreEqual(-1, measureSetup.MeasureSetupId);
            Assert.AreEqual(100d, measureSetup.DilutionSampleVolume);
            Assert.AreEqual(10d, measureSetup.DilutionCasyTonVolume);
        }

        [TestMethod]
        public void CalcSmoothedDiametersAndVolumeMapping_ToDiameterZero()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.CalcSmoothedDiametersAndVolumeMapping();
            Assert.IsNull(measureSetup.SmoothedDiameters);
            Assert.IsNull(measureSetup.VolumeMapping);
        }

        [TestMethod]
        public void CalcSmoothedDiametersAndVolumeMapping_ToDiameterLowerFromDiameter_ShallInvalidOperationException()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.FromDiameter = 11;

            try
            {
                measureSetup.ToDiameter = 10;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcSmoothedDiametersAndVolumeMapping_ChannelCountLowerZero_ShallArgumentOutOfRangeException()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.ToDiameter = 64;
            try
            {
                measureSetup.ChannelCount = -1;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.AreEqual(e.ActualValue, -1);
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void CalcSmoothedDiametersAndVolumeMapping_PassValidValues()
        {
            var measureSetup = new MeasureSetup();
            measureSetup.ToDiameter = 64;

            Assert.AreEqual(measureSetup.ChannelCount, measureSetup.SmoothedDiameters.Length);
            Assert.AreEqual(measureSetup.ChannelCount, measureSetup.VolumeMapping.Length);
            Assert.AreEqual(0, measureSetup.VolumeMapping[0]);
            Assert.AreEqual(0, measureSetup.SmoothedDiameters[0]);
            Assert.AreEqual(64, measureSetup.SmoothedDiameters[1023]);

            measureSetup.ChannelCount = 400;

            Assert.AreEqual(measureSetup.ChannelCount, measureSetup.SmoothedDiameters.Length);
            Assert.AreEqual(measureSetup.ChannelCount, measureSetup.VolumeMapping.Length);
            Assert.AreEqual(0, measureSetup.VolumeMapping[0]);
            Assert.AreEqual(0, measureSetup.SmoothedDiameters[0]);
            Assert.AreEqual(64, measureSetup.SmoothedDiameters[399]);
        }
    }
}
