using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ChartCursorViewModel))]
    public class ChartCursorViewModel : CursorViewModelBase, IDisposable
    {
        private readonly IAuthenticationService _authenticationService;

        [ImportingConstructor]
        public ChartCursorViewModel(IUIProjectManager uiProject, ILocalizationService localizationService, IMeasureResultManager measureResultManager, IAuthenticationService authenticationService)
            : base(uiProject, measureResultManager)
        {
            this._authenticationService = authenticationService;
            this.LocalizationService = localizationService;

            MinModificationHandleViewModel = new RangeMinModificationHandleViewModel(this);
            MaxModificationHandleViewModel = new RangeMaxModificationHandleViewModel(this);
        }

        public ILocalizationService LocalizationService { get; private set; }

        public RangeModificationHandleViewModel MinModificationHandleViewModel { get; set; }

        public RangeModificationHandleViewModel MaxModificationHandleViewModel { get; set; }

        public bool CanModifyRange
        {
            get
            {
                return this._authenticationService.LoggedInUser.UserRole.Priority > 1 && !this.IsReadOnly;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var disposable = this.MinModificationHandleViewModel as IDisposable;
                    if(disposable != null)
                    {
                        disposable.Dispose();
                    }
                    this.MinModificationHandleViewModel = null;

                    disposable = this.MaxModificationHandleViewModel as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                    this.MaxModificationHandleViewModel = null;
                    //if(_minTimer != null)
                    //{
                    //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}