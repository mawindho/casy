using OLS.Casy.Calculation.Api;
using OLS.Casy.Models;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Calculation.Volume
{
    /// <summary>
    /// Implementation of <see cref="IDataCalculationProvider"/> for transforming measured counts
    /// to volume
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVolumeCalculationProvider))]
    public class VolumeCalculationProvider : IVolumeCalculationProvider
    {
        /// <summary>
        /// Importing constructor
        /// </summary>
        /// <param name="calibrationController">Implementation of <see cref="ICalibrationController"/> </param>
        [ImportingConstructor]
        public VolumeCalculationProvider()
        {
        }

        /// <summary>
        /// Returns the display name for the data calculation provider usable in UI. Shall be localized.
        /// </summary>
        public string Name
        {
            get
            {
                return "Volume Calculation";
            }
        }

        /// <summary>
        /// Transforms the passed data block by turning counts into volume
        /// </summary>
        /// <param name="dataBlock">Data to be transformed</param>
        /// <returns>The transformed data</returns>
        public double[] TransformMeasureResultDataBlock(MeasureSetup measureSetup, double[] dataBlock)
        {

            if(measureSetup == null)
            {
                throw new ArgumentNullException("measureSetup");
            }

            if (dataBlock == null)
            {
                throw new ArgumentNullException("dataBlock");
            }

            if (dataBlock.Length == 0)
            {
                return dataBlock;
            }

            if (measureSetup.VolumeMapping == null)
            {
                throw new ArgumentNullException("measureSetup.VolumeMapping");
            }

            double[] result = new double[dataBlock.Length];

            for (int i = 0; i < dataBlock.Length; i++)
            {
                if (measureSetup.VolumeMapping.Length >= i)
                {
                    result[i] = measureSetup.VolumeMapping[i] * dataBlock[i];
                }
            }

            return result;
        }
    }
}
