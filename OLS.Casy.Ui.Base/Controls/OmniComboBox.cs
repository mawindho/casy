using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniComboBox : ComboBox
    {
        private TextBox _textBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox = Template.FindName("PART_EditableTextBox", this) as TextBox;
            if (_textBox == null) return;
            _textBox.GotKeyboardFocus += _textBox_GotFocus;
            Unloaded += OmniComboBox_Unloaded;
        }

        void OmniComboBox_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _textBox.GotKeyboardFocus -= _textBox_GotFocus;
            Unloaded -= OmniComboBox_Unloaded;
        }

        void _textBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            _textBox.Select(_textBox.Text.Length, 0); // set caret to end of text
        }
    }
}
