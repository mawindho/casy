using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(SelectRangeParentDialogModel))]
    public class SelectRangeParentDialogModel : DialogModelBase
    {
        public List<Tuple<string, MeasureResult>> RangeOptions { get; set; }

        public MeasureResult SelectedRangeOption { get; set; }
    }
}
