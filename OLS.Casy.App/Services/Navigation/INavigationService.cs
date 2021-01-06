using OLS.Casy.App.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.App.Services.Navigation
{
    public interface INavigationService
    {
        ViewModelBase PreviousPageViewModel { get; }

        Task InitializeAsync();

        Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase;

        Task NavigateToAsync<TViewModel>(Dictionary<string, object> parameter) where TViewModel : ViewModelBase;

        Task NavigateToAsync(Type viewModelType);

        Task NavigateToAsync(Type viewModelType, Dictionary<string, object> parameter);
        Task NavigateBackAsync();

        Task RemoveLastFromBackStackAsync();

        Task RemoveBackStackAsync();
    }
}
