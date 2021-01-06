using System;
using System.Linq;

namespace OLS.Casy.Base
{
    public static class Calculations
    {
        public static int CalcChannel(int fromDiameter, int toDiameter, double smoothDiameter, int maxChannel)
        {
            if(maxChannel <= 0)
            {
                throw new ArgumentOutOfRangeException("maxChannel", "maxChannel must be greater 0");
            }

            double mue = toDiameter - fromDiameter;

            if(mue == 0)
            {
                throw new DivideByZeroException("toDiameter - fromDiameter must not result 0");
            }

            int value = (int)(((smoothDiameter - (double)fromDiameter) / mue * (double)maxChannel) + 0.5d);

            if(value < 0)
            {
                return 0;
            }
            else if(value >= maxChannel)
            {
                return maxChannel - 1;
            }
            return value;
        }

        public static double CalcSmoothedDiameter(int fromDiameter, int toDiameter, int channel, int maxChannel)
        {
            if (maxChannel <= 0)
            {
                return 0;
            }

            double mue = toDiameter - fromDiameter;

            if (mue == 0)
            {
                return 0;
            }
            
            if (channel <= 0)
            {
                return 0d;
            }

            if(channel >= maxChannel-1)
            {
                return toDiameter;
            }

            return (channel + 0.5d) * mue / maxChannel + fromDiameter;
        }

        /*
        public static double CalcRelativeDiviation(double[] valuesPerRepeat)
        {
            if (valuesPerRepeat == null)
            {
                throw new ArgumentNullException("valuesPerRepeat");
            }

            if(valuesPerRepeat.Length == 0)
            {
                return 0d;
            }

            var sum = valuesPerRepeat.Sum();
            if(sum == 0d)
            {
                return 0d;
            }

            double average = valuesPerRepeat.Average();
            var max = valuesPerRepeat.Max();
            var temp = (max - average) * Math.Sqrt(valuesPerRepeat.Length);
            return temp / sum;
        }
        */

        public static double CalcRelativeDeviation(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (values.Length <= 1)
            {
                return 0d;
            }

            var deviation = CalcDeviation(values) * 100;
            //var step1 = deviation * 100;
            var mean = values.Sum() / values.Length;
            //var step2 = step1 / Math.Abs(mean);
            //return step2;
            return mean == 0 ? 0d : deviation / mean / Math.Sqrt(values.Length);
        }

        public static double CalcDeviation(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (values.Length <= 1)
            {
                return 0d;
            }

            var mean = values.Sum() / values.Length;
            var t = values.Select(d => Math.Pow(d - mean, 2)).Sum();
            var t2 = 1d / (values.Length - 1) * t;
            return Math.Sqrt(t2);

            //var sum = values.Sum();
            //var step1 = Math.Pow(sum, 2) / values.Length;
            //var step2 = values.Select(d => Math.Pow(d, 2)).Sum();
            //var step3 = step2 - step1;
            //var step4 = step3 / (values.Length - 1);
            //return Math.Sqrt(step4);
        }

        public static double CalcEffectiveMl(double volume, double volumeCorrectionFactor, double dilutionFactor, int repeats)
        {
            if(repeats < 1)
            {
                throw new ArgumentException("Repeats must not be less then 1", "repeats");
            }

            if(volume == 0)
            {
                throw new DivideByZeroException("Volume must not be zero");
            }
            return ((volumeCorrectionFactor / 10000d) * dilutionFactor) / ((repeats * volume) / 1000d);
        }

        public static uint CalcChecksum(byte[] source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            uint checksum = 0;

            for (int i = 0; i < source.Length; i++)
            {
                checksum += (uint)source[i];
            }

            return checksum;
        }
    }
}
