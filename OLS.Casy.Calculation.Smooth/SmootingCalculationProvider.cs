using OLS.Casy.Calculation.Api;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Calculation.Smooth
{
    /// <summary>
    /// Implementation of <see cref="IDataCalculationProvider"/> for smooting the data
    /// like the old casy application does it
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISmootingCalculationProvider))]
    public class SmootingCalculationProvider : ISmootingCalculationProvider
    {
        [ImportingConstructor]
        public SmootingCalculationProvider()
        {
            this.Width = 1;
        }

        /// <summary>
        /// Returns the display name for the data calculation provider usable in UI. Shall be localized.
        /// </summary>
        public string Name
        {
            get { return "Smoothing"; }
        }

        /// <summary>
        /// Property for the width used by smooting alogirthm
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Transforms the passed data block by smooth the data
        /// </summary>
        /// <param name="dataBlock">Data to be transformed</param>
        /// <returns>The transformed data</returns>
        public double[] TransformMeasureResultDataBlock(double[] dataBlock)
        {
            if (dataBlock == null)
            {
                throw new ArgumentNullException("dataBlock");
            }

            if (dataBlock.Length == 0)
            {
                return dataBlock;
            }

            var smoothWidth = this.Width <= 0 ? 1 : this.Width;

            var maxRange = smoothWidth / 2;
            var maxIndex = dataBlock.Length - 1;

            int inIndex = 0;
            int outIndex = 0;

            double[] result = new double[dataBlock.Length];

            int rangeLeft, rangeRight;

            while (outIndex <= maxIndex)
            {
                inIndex = rangeLeft = Math.Max(0, outIndex - maxRange);
                rangeRight = Math.Min(maxIndex, outIndex + maxRange);
                while (inIndex <= rangeRight)
                {
                    result[outIndex] += dataBlock[inIndex++];
                }
                result[outIndex++] /= rangeRight - rangeLeft + 1;
            }
            
            return result;
        }
    }
}
