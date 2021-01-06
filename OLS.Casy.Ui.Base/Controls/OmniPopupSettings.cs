using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniPopupSettings : PopupSettings
    {
        MethodInfo info = typeof(ListBox).GetMethod("OnMouseLeftButtonUp", BindingFlags.Instance | BindingFlags.NonPublic);

        public OmniPopupSettings(PopupBaseEdit editor) : base(editor) { }

        void popup_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            GalleryItemControl control = LayoutHelper.FindParentObject<GalleryItemControl>((DependencyObject)e.OriginalSource);

            if (control != null)
            {
                info.Invoke(control, new object[] { new MouseButtonEventArgs(Mouse.PrimaryDevice, e.Timestamp, MouseButton.Left) });
                e.Handled = true;
                return;
            }
        }

        protected override EditorPopupBase CreatePopup()
        {
            EditorPopupBase popup = base.CreatePopup();
            popup.PreviewTouchDown += popup_PreviewTouchDown;
            return popup;
        }

        protected override void UnsetupPopup(EditorPopupBase popup)
        {
            popup.PreviewTouchDown -= popup_PreviewTouchDown;
            base.UnsetupPopup(popup);
        }
    }
}
