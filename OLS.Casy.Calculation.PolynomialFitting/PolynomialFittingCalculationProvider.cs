using OLS.Casy.Calculation.Api;
using OLS.Casy.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OLS.Casy.Base;

namespace OLS.Casy.Calculation.PolynomialFitting
{
    /// <summary>
    /// Implementation od <see cref="IPolynomialFittingCalculationProvider"/>.
    /// Provides the functionality to execute an polynomial fitting on passed data block
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPolynomialFittingCalculationProvider))]
    public class PolynomialFittingCalculationProvider : IPolynomialFittingCalculationProvider
    {
        /// <summary>
        /// Returns the display name for the data calculation provider usable in UI. Shall be localized.
        /// </summary>
        public string Name
        {
            get { return "Polynomial Fitting"; }
        }

        /// <summary>
        /// Transforms the passed data block by executing a polynomial fitting.
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

            int numData = dataBlock.Length;

            double[] x = new double[numData];
            double[] y = new double[numData];

            int curIndex = 0;
            bool searchHigh = false;

            IList<Tuple<double, double>> temp = new List<Tuple<double, double>>();
            
            // It's not really good to use the same order of polynomial fitting for the whole data.
            // Therefore we're searching for regions and peaks, that:
            // a. Range between 0 to 5 - doing linear fitting (order 0)
            // b. Peaks - doing exponential fitting (order 4)
            double[] transformed = new double[0];
            for (int i = 0; i < numData; i++)
            {
                x[i] = i;

                if (!searchHigh)
                {
                    var toCheckCount = Math.Min(5, numData - i);
                    //Searching for values lower than 5
                    if (dataBlock.SubArray(i, toCheckCount).All(val => val <= 5))
                    {
                        temp.Add(new Tuple<double, double>(i, dataBlock[i]));
                    }
                    else
                    {
                        transformed = this.PolyFit(temp.Select(t => t.Item1).ToArray(), temp.Select(t => t.Item2).ToArray(), 0);
                        Array.Copy(transformed, 0, y, curIndex, transformed.Length);
                        curIndex += transformed.Length;
                        temp.Clear();
                        searchHigh = true;
                        temp.Add(new Tuple<double, double>(i, dataBlock[i]));
                    }
                }
                else
                {
                    var toCheckCount = Math.Min(5, numData - i);
                    if (dataBlock.SubArray(i, toCheckCount).Any(val => val > 5))
                    {
                        temp.Add(new Tuple<double, double>(i, dataBlock[i]));
                    }
                    else
                    {
                        transformed = this.PolyFit(temp.Select(t => t.Item1).ToArray(), temp.Select(t => t.Item2).ToArray(), 4);
                        Array.Copy(transformed, 0, y, curIndex, transformed.Length);
                        curIndex += transformed.Length;
                        temp.Clear();
                        searchHigh = false;
                        temp.Add(new Tuple<double, double>(i, dataBlock[i]));
                    }
                }
            }

            transformed = this.PolyFit(temp.Select(t => t.Item1).ToArray(), temp.Select(t => t.Item2).ToArray(), 0);
            Array.Copy(transformed, 0, y, curIndex, transformed.Length);

            return y;
        }

        private double[] PolyFit(double[] xValues, double[] yValues, int order = 3)
        {
            var polyfit = new PolyFit(xValues, yValues, order);
            var fitted = polyfit.Fit(xValues);
            return fitted;
        }
    }
}
