using DevExpress.Xpf.Charts;
using System.Windows;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniAxisY2D : AxisY2D
    {
        public static readonly DependencyProperty IsShowVolumeProperty =
            DependencyProperty.Register(
                "IsShowVolume",
                typeof(bool),
                typeof(OmniAxisY2D),
                new PropertyMetadata(false, OnIsShowVolumeChanged)
                );

        public static readonly DependencyProperty IsNormalizationProperty =
            DependencyProperty.Register(
                "IsNormalization",
                typeof(bool),
                typeof(OmniAxisY2D),
                new PropertyMetadata(false, OnIsShowVolumeChanged)
                );

        private static void OnIsShowVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OmniAxisY2D axis = (OmniAxisY2D) d;

            if(axis != null && axis.Label != null)
            {
                var isIsShowVolume = GetIsShowVolume(axis);
                var isNormalization = GetIsNormalization(axis);

                if(isIsShowVolume && ! isNormalization)
                {
                    axis.Label.ElementTemplate = (DataTemplate)axis.Resources["AxisYVolumeLabelTemplate"];
                }
                else
                {
                    axis.Label.ElementTemplate = (DataTemplate)axis.Resources["AxisYLabelTemplate"];
                }
            }
        }

        public static bool GetIsShowVolume(DependencyObject target)
        {
            return (bool)target.GetValue(IsShowVolumeProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetIsShowVolume(DependencyObject target, bool value)
        {
            target.SetValue(IsShowVolumeProperty, value);
        }

        public static bool GetIsNormalization(DependencyObject target)
        {
            return (bool)target.GetValue(IsNormalizationProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetIsNormalization(DependencyObject target, bool value)
        {
            target.SetValue(IsNormalizationProperty, value);
        }
    }
}
