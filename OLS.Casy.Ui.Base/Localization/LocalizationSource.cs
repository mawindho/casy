using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Linq;
using System.Windows;

namespace OLS.Casy.Ui.Base.Localization
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal sealed class LocalizationSource : Source
    {
        private bool disposedValue = false; // To detect redundant calls

        protected override void Initialize()
        {
            if (this.Attributes != null && this.Attributes.Any())
            {
                WeakEventManager<ILocalizationService, LocalizationEventArgs>.AddHandler(Localization.LocalizationService, "LanguageChanged", HandleLanguageChangedEvent);
            }
            this.Localize();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Localization.LocalizationService.LanguageChanged -= HandleLanguageChangedEvent;
                }
            }
            base.Dispose(disposing);
        }

        private void HandleLanguageChangedEvent(object sender, LocalizationEventArgs authenticationEventArgs)
        {
            this.Localize();
        }

        private Result Localize()
        {
            this.Result = this.LocalizeCore();
            return this.Result;
        }

        private LocalizationResult LocalizeCore()
        {
            LocalizationAttribute firstAttribute = Attributes.FirstOrDefault() as LocalizationAttribute;
            if (firstAttribute != null)
            {
                return firstAttribute.Localize();
            }

            return null;
        }
    }
}
