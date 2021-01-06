using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.App.Services.Settings;
using OLS.Casy.App.ViewModels;
using OLS.Casy.App.ViewModels.Base;
using OLS.Casy.App.Views;
using OLS.Casy.App.Views.Tablet;
using Xamarin.Forms;

namespace OLS.Casy.App.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly ISettingsService _settingsService;

        public NavigationService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public ViewModelBase PreviousPageViewModel
        {
            get
            {
                var mainPage = Application.Current.MainPage as CustomNavigationView;
                var viewModel = mainPage.Navigation.NavigationStack[mainPage.Navigation.NavigationStack.Count - 2].BindingContext;
                return viewModel as ViewModelBase;
            }
        }

        public Task InitializeAsync()
        {
            //TODO: Temp
            _settingsService.AuthAccessToken = string.Empty;
            _settingsService.AuthIdToken = string.Empty;

            if (string.IsNullOrEmpty(_settingsService.AuthAccessToken))
                return NavigateToAsync<LoginViewModel>();
            else
                return NavigateToAsync<MainViewModel>();
        }

        public Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase
        {
            return InternalNavigateToAsync(typeof(TViewModel), null);
        }

        public Task NavigateToAsync(Type viewModelType)
        {
            return InternalNavigateToAsync(viewModelType, null);
        }

        public Task NavigateToAsync<TViewModel>(Dictionary<string, object> parameter) where TViewModel : ViewModelBase
        {
            return InternalNavigateToAsync(typeof(TViewModel), parameter);
        }

        public Task NavigateToAsync(Type viewModelType, Dictionary<string, object> parameter)
        {
            return InternalNavigateToAsync(viewModelType, parameter);
        }

        public Task NavigateBackAsync()
        {
            return InternalNavigateBack();
        }

        public Task RemoveLastFromBackStackAsync()
        {
            if (Application.Current.MainPage is CustomNavigationView mainPage)
            {
                mainPage.Navigation.RemovePage(
                    mainPage.Navigation.NavigationStack[mainPage.Navigation.NavigationStack.Count - 2]);
            }

            return Task.FromResult(true);
        }

        public Task RemoveBackStackAsync()
        {
            if (!(Application.Current.MainPage is CustomNavigationView mainPage)) return Task.FromResult(true);
            for (var i = 0; i < mainPage.Navigation.NavigationStack.Count - 1; i++)
            {
                var page = mainPage.Navigation.NavigationStack[i];
                mainPage.Navigation.RemovePage(page);
            }

            return Task.FromResult(true);
        }

        private async Task InternalNavigateToAsync(Type viewModelType, Dictionary<string, object> parameter)
        {
            var page = CreatePage(viewModelType, parameter);

            if (page is LoginView)
            {
                Application.Current.MainPage = new CustomNavigationView(page);
            }
            else
            {                
                if(page is MainView)
                {
                    if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
                    {
                        Application.Current.MainPage = new CustomNavigationView(new MainTabletView()
                        {
                            Master = page,
                            Detail = new DashboardView()
                        });
                    }
                    else
                    {
                        Application.Current.MainPage = new CustomNavigationView(page);
                    }
                }
                else
                {
                    var navigationPage = Application.Current.MainPage as CustomNavigationView;

                    if (navigationPage.CurrentPage is LoginView)
                    {
                        await navigationPage.PushAsync(page);
                    }
                    else
                    {
                        if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
                        {
                            ((MainTabletView)navigationPage.CurrentPage).Detail = page;
                        }
                        else
                        {
                            await navigationPage.PushAsync(page);
                        }
                    }
                }
            }

            await (page.BindingContext as ViewModelBase).InitializeAsync(parameter);
        }

        private async Task InternalNavigateBack()
        {
            if (Application.Current.MainPage is CustomNavigationView navigationPage)
            {
                await navigationPage.PopAsync();
            }
            //else
            //{
            //  Application.Current.MainPage = new CustomNavigationView(page);
            //}
            // await (page.BindingContext as ViewModelBase).InitializeAsync(parameter);
        }

        private Type GetPageTypeForViewModel(Type viewModelType)
        {
            var viewName = viewModelType.FullName.Replace("Model", string.Empty);
            var viewModelAssemblyName = viewModelType.GetTypeInfo().Assembly.FullName;
            var viewAssemblyName = string.Format(CultureInfo.InvariantCulture, "{0}, {1}", viewName, viewModelAssemblyName);

            Type viewType;
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                //Try to get tablet view
                var lastIndex = viewName.LastIndexOf("View", StringComparison.InvariantCulture);
                if (lastIndex > -1)
                {
                    var tabletViewName = viewName.Remove(lastIndex, 4);
                    tabletViewName = tabletViewName.Insert(lastIndex, "TabletView");
                    tabletViewName = tabletViewName.Replace(".Views.", ".Views.Tablet.");
                    var tabletViewAssemblyName = string.Format(CultureInfo.InvariantCulture, "{0}, {1}", tabletViewName,
                        viewModelAssemblyName);
                    try
                    {
                        //IOS: OLS.Casy.App.Views.Tablet.MainTabletView, OLS.Casy.App, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
                        viewType = Type.GetType(tabletViewAssemblyName);

                        if (viewType != null)
                            return viewType;
                    }
                    catch
                    {
                        //Ignore
                    }
                }
            }

            viewType = Type.GetType(viewAssemblyName);
            return viewType;
        }

        private Page CreatePage(Type viewModelType, object parameter)
        {
            var pageType = GetPageTypeForViewModel(viewModelType);
            if (pageType == null)
            {
                throw new Exception($"Cannot locate page type for {viewModelType}");
            }

            var page = Activator.CreateInstance(pageType) as Page;
            return page;
        }
    }
}
