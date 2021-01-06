using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Activation.KeyGenerator
{
    public class ActivatedMachineViewModel : ViewModelBase
    {
        public ActivatedMachineViewModel(ActivationKeyViewModel parent)
        {
            Parent = parent;
        }

        public ActivationKeyViewModel Parent { get; }
        public int Id { get; set; }
        public string MacAddress { get; set; }
        public DateTime ActivatedOn { get; set; }
        public string SerialNumber { get; set; }
        public string CurrentVersion { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string ComputerName { get; set; }
    }
}
