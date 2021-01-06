using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniMenuButton : RadioButton
    {
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
                "Glyph",
                typeof(string),
                typeof(OmniMenuButton),
                new PropertyMetadata(null)
                );

        public static string GetGlyph(DependencyObject target)
        {
            return (string)target.GetValue(GlyphProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetGlyph(DependencyObject target, string value)
        {
            target.SetValue(GlyphProperty, value);
        }

        public static readonly DependencyProperty AwesomeGlyphProperty =
            DependencyProperty.Register(
                "AwesomeGlyph",
                typeof(PackIconFontAwesomeKind),
                typeof(OmniMenuButton),
                new PropertyMetadata(PackIconFontAwesomeKind.None)
            );

        public static PackIconFontAwesomeKind GetAwesomeGlyph(DependencyObject target)
        {
            return (PackIconFontAwesomeKind)target.GetValue(AwesomeGlyphProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetAwesomeGlyph(DependencyObject target, PackIconFontAwesomeKind value)
        {
            target.SetValue(AwesomeGlyphProperty, value);
        }
    }
}
