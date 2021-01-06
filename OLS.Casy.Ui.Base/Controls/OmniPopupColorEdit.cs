using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniPopupColorEdit : PopupColorEdit
    {
        protected override PopupSettings CreatePopupSettings()
        {
            return new OmniPopupSettings(this);
        }

    }
}
