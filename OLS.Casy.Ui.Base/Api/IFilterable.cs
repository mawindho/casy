using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.Api
{
    public interface IFilterable
    {
        bool IsVisible { get; }
    }
}
