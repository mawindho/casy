using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniSlider : Slider
    {
        protected override void OnTouchDown(TouchEventArgs e)
        {
            e.Handled = true;
        }
    }
}
