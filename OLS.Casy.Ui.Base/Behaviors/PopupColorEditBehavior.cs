using DevExpress.Xpf.Editors;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class PopupColorEditBehavior : Behavior<PopupColorEdit>
    {
        public string ColorResourceBaseName
        {
            get { return (string)GetValue(ColorResourceBaseNameProperty); }
            set { SetValue(ColorResourceBaseNameProperty, value); }
        }

        public static readonly DependencyProperty ColorResourceBaseNameProperty =
            DependencyProperty.RegisterAttached("ColorResourceBaseName", typeof(string), typeof(PopupColorEditBehavior),
                new FrameworkPropertyMetadata(null));


        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.PopupOpening += OnPopupOpening;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.PopupOpening -= OnPopupOpening;
            }
        }

        private void OnPopupOpening(object sender, OpenPopupEventArgs e)
        {
            var popupColorEdit = sender as PopupColorEdit;

            if (popupColorEdit != null)
            {
                var colorResourceBaseName = this.ColorResourceBaseName;

                var standardColors = popupColorEdit.Palettes.FirstOrDefault(palette => palette.Name != null && palette.Name.ToLower().StartsWith("standard"));
                var name = standardColors.Name;
                popupColorEdit.Palettes.Remove(standardColors);

                List<Color> colors = new List<Color>();
                for (int i = 1; i <= 10; i++)
                {
                    colors.Add(((SolidColorBrush)Application.Current.Resources[string.Format("{0}{1}", colorResourceBaseName, i)]).Color);
                }

                popupColorEdit.Palettes.Add(new CustomPalette(name, colors));
            }
        }
    }
}
