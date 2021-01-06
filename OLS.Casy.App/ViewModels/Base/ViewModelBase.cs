using OLS.Casy.App.Services.Dialog;
using OLS.Casy.App.Services.Navigation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.App.ViewModels.Base
{
    public abstract class ViewModelBase : ExtendedBindableObject
    {
        protected readonly IDialogService DialogService;
        protected readonly INavigationService NavigationService;

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        protected ViewModelBase()
        {
            DialogService = ViewModelLocator.Resolve<IDialogService>();
            NavigationService = ViewModelLocator.Resolve<INavigationService>();
        }

        public virtual Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            return Task.FromResult(false);
        }

        public bool IsUserLoggedIn
        {
            get { return false; }
        }
    }
}
