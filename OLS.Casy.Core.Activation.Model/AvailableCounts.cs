using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Activation.Model
{
    public class AvailableCounts
    {
        public string ActivationKey { get; set; }
        public int Counts { get; set; }
        public string ValidationError { get; set; }
    }
}
