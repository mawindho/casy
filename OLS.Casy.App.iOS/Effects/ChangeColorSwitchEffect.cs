﻿using OLS.Casy.App.Effects;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using ChangeColorSwitchEffect = OLS.Casy.App.iOS.Effects.ChangeColorSwitchEffect;

[assembly: ExportEffect(typeof(ChangeColorSwitchEffect), nameof(ChangeColorSwitchEffect))]
namespace OLS.Casy.App.iOS.Effects
{
    public class ChangeColorSwitchEffect : PlatformEffect
    {
        private Color _trueColor;
        private Color _falseColor;

        public ChangeColorSwitchEffect()
        {
        }

        protected override void OnAttached()
        {
            _trueColor = (Color)Element.GetValue(ChangeColorEffect.TrueColorProperty);
            _falseColor = (Color)Element.GetValue(ChangeColorEffect.FalseColorProperty);

            if (_falseColor != Color.Transparent)
            {
                (Control as UISwitch).TintColor = _falseColor.ToUIColor();
                (Control as UISwitch).Layer.CornerRadius = 16;
                (Control as UISwitch).BackgroundColor = _falseColor.ToUIColor();
            }

            if (_trueColor != Color.Transparent)
                (Control as UISwitch).OnTintColor = _trueColor.ToUIColor();
        }

        protected override void OnDetached()
        {
        }
    }
}