using OLS.Casy.Calculation.Api;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace OLS.Casy.Calculation.Normalization
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(INormalizationCalculationProvider))]
    public class NormalizationCalculationProvider : INormalizationCalculationProvider
    {
        public string Name
        {
            get
            {
                return "Normalization";
            }
        }

        public double[] TransformMeasureResultDataBlock(double[] dataBlock)
        {
            if(dataBlock == null)
            {
                throw new ArgumentNullException("dataBlock");
            }

            if(dataBlock.Length == 0)
            {
                return dataBlock;
            }

            var dataMax = dataBlock.Max();

            if (dataMax <= 0d)
            {
                dataMax = dataBlock.Min();
                var dataBlockTemp = dataBlock.Select(d => d - dataMax).ToArray();

                var multiplicator = dataMax == 0 ? 1 : 100 / (dataMax - dataBlock.Max())  * -1;
                return dataBlockTemp.Select(d => d * multiplicator).ToArray();
            }
            else
            {
                var multiplicator = dataMax == 0 ? 1 : 100 / dataMax;
                return dataBlock.Select(d => d * multiplicator).ToArray();
            }
            //var dataMin = dataBlock.Min();
            //var range = dataMax - dataMin;

            //return dataBlock.Select(d => (d - dataMin) / range).Select(n => ((1 - n) * dataMin + n * dataMax)).ToArray();
        }
    }
}
