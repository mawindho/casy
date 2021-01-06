using OLS.Casy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.Base;

namespace OLS.Casy.IO.ImportExport.TT
{
    public class Interpolations
    {
        public static double[] LinearInterpolation(int fromDiameter, int toDiameter, int maxChannel, int m, double[] y_n)
        {
            var x_n = new double[y_n.Length];
            var x_m = new double[m];

            for(int i = 0; i < y_n.Length; i++)
            {
                x_n[i] = Calculations.CalcSmoothedDiameter(fromDiameter, toDiameter, i, y_n.Length);
            }
            var deltaN = x_n[1];

            for(int i = 0; i < m; i++)
            {
                x_m[i] = Calculations.CalcSmoothedDiameter(fromDiameter, toDiameter, i, m);
            }
            var deltaM = x_m[1];
            var deltaM2 = (x_n[x_n.Length - 1] - x_n[0]) / m - 1;

            var y_m = new double[m];

            int l = 0;
            for(int k = 0; k < m; k++)
            {
                if(x_m[k] >= x_n[l+1])
                {
                    l++;
                }

                var index = Array.IndexOf(x_n, x_m[k]);
                if (index != -1)
                {
                    y_m[k] = y_n[index];
                }
                else
                {
                    y_m[k] = (((y_n[l + 1] - y_n[l]) * (x_m[k] - x_n[l])) / (x_n[l + 1] - x_n[l])) + y_n[l];
                }
            }

            return y_m;
        }

        public static double[] CubicInterpolation(int fromDiameter, int toDiameter, int m, double[] y_n)
        {
            var x_n = new double[y_n.Length];
            var x_m = new double[m];

            for (int i = 0; i < y_n.Length; i++)
            {
                x_n[i] = Calculations.CalcSmoothedDiameter(fromDiameter, toDiameter, i, y_n.Length);
            }
            var deltaN = x_n[1];

            for (int i = 0; i < m; i++)
            {
                x_m[i] = Calculations.CalcSmoothedDiameter(fromDiameter, toDiameter, i, m);
            }
            var deltaM = x_m[1];

            var y_m = new double[m];

            int l = 0;
            for (int k = 0; k < m; k++)
            {
                if (x_m[k] >= x_n[l + 1])
                {
                    l++;
                }

                var index = Array.IndexOf(x_n, x_m[k]);
                if (index != -1)
                {
                    y_m[k] = y_n[index];
                }
                else if(x_n[l] == x_n[0] || x_n[l+1] == x_n[x_n.Length-1])
                {
                    y_m[k] = (((y_n[l + 1] - y_n[l]) * (x_m[k] - x_n[l])) / (x_n[l + 1] - x_n[l])) + y_n[l];
                }
                else
                {
                    var p1 = new Point(x_n[l - 1], y_n[l - 1]);
                    var p2 = new Point(x_n[l], y_n[l]);
                    var p3 = new Point(x_n[l + 1], y_n[l + 1]);
                    var p4 = new Point(x_n[l + 2], y_n[l + 2]);

                    var a0 = y_n[l];
                    var a1 = Coeff(p1, p2);
                    var a2 = Coeff(p1, p2, p3);
                    var a3 = Coeff(p1, p2, p3, p4);

                    y_m[k] = a0 + (a1 * (x_m[k] - x_n[l-1])) + (a2 * (x_m[k] - x_n[l-1]) * (x_m[k] - x_n[l])) + (a3 * (x_m[k] - x_n[l-1]) * (x_m[k] - x_n[l]) * (x_m[k] - x_n[l + 1])); 
                }
            }

            return y_m;
        }

        public static double Coeff(Point p1, Point p2)
        {
            return (p1.Y - p2.Y) / (p1.X - p2.X);
        }

        public static double Coeff(Point p1, Point p2, Point p3)
        {
            return (Coeff(p1, p2) - Coeff(p2, p3)) / (p1.X - p3.X);
        }

        public static double Coeff(Point p1, Point p2, Point p3, Point p4)
        {
            return (Coeff(p1, p2, p3) - Coeff(p2, p3, p4)) / (p1.X - p4.X);
        }

        public class Point
        {
            public Point(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }

            public double X;
            public double Y;

            public override string ToString()
            {
                return "X: " + this.X.ToString() + " - Y: " + this.Y.ToString(); 
            }
        }
    }
}
