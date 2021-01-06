using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please select max X-Range: ");
            var xRange = Console.ReadLine();
            Console.WriteLine("Please select the channel [0-1023]: ");
            var channel = Console.ReadLine();
            Console.WriteLine("Please select the counts value");
            var counts = Console.ReadLine();

            double iMaxXRange = double.Parse(xRange);
            double dChannel = int.Parse(channel);
            double dCounts = double.Parse(counts);

            double slope = iMaxXRange / 1024d;
            var mue = slope * (dChannel + 0.5d);
            var volumeMapping = dChannel == 0d ? 0d : (Math.Pow(mue, 3) * (Math.PI / 6d));

            Console.WriteLine($"Volume-Muliplication-Factor: {volumeMapping}");
            Console.WriteLine($"Volume: {volumeMapping * dCounts}");
            Console.ReadLine();

        }
    }
}
