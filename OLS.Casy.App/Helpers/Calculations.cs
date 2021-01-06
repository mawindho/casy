using System;

namespace OLS.Casy.App.Helpers
{
    public static class Calculations
    {
        public static int CalcChannel(int fromDiameter, int toDiameter, double smoothDiameter, int maxChannel)
        {
            if (maxChannel <= 0)
            {
                throw new ArgumentOutOfRangeException("maxChannel", "maxChannel must be greater 0");
            }

            double mue = toDiameter - fromDiameter;

            if (mue == 0)
            {
                throw new DivideByZeroException("toDiameter - fromDiameter must not result 0");
            }

            int value = (int)(((smoothDiameter - (double)fromDiameter) / mue * (double)maxChannel) + 0.5d);

            if (value < 0)
            {
                return 0;
            }
            if (value >= maxChannel)
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

            if (channel >= maxChannel - 1)
            {
                return toDiameter;
            }

            return (channel + 0.5d) * mue / maxChannel + fromDiameter;
        }
    }
}
