using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Ui.Api
{
    public interface ITabTipAutomation
    {
        HardwareKeyboardIgnoreOptions IgnoreHardwareKeyboard { get; set; }
        void BindTo<T>() where T : UIElement;
    }
}
