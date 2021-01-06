using OLS.Casy.App.Services;
using System;
using System.Globalization;
using System.Reflection;
using OLS.Casy.App.Services.Settings;
using TinyIoC;
using Xamarin.Forms;
using OLS.Casy.App.Services.Navigation;
using OLS.Casy.App.Services.Dialog;
using OLS.Casy.App.Services.MeasureResults;
using OLS.Casy.App.Services.RequestProvider;
using OLS.Casy.App.Services.Detection;

namespace OLS.Casy.App.ViewModels.Base
{
    public static class ViewModelLocator
    {
        private static TinyIoCContainer _container;

        public static readonly BindableProperty AutoWireViewModelProperty =
            BindableProperty.CreateAttached("AutoWireViewModel", typeof(bool), typeof(ViewModelLocator), default(bool),
                propertyChanged: OnAutoWireViewModelChanged);

        public static bool GetAutoWireViewModel(BindableObject bindable)
        {
            return (bool) bindable.GetValue(ViewModelLocator.AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(BindableObject bindable, bool value)
        {
            bindable.SetValue(ViewModelLocator.AutoWireViewModelProperty, value);
        }

        static ViewModelLocator()
        {
            _container = new TinyIoCContainer();

            //ViewModels
            _container.Register<LoginViewModel>();
            _container.Register<SettingsViewModel>().AsSingleton();
            _container.Register<MainViewModel>().AsSingleton();
            _container.Register<SelectMeasureResultsViewModel>().AsSingleton();
            _container.Register<SingleMeasurementViewModel>();
            _container.Register<OverlayViewModel>();
            _container.Register<MeanViewModel>();
            _container.Register<DashboardViewModel>();

            //Services
            _container.Register<INavigationService, NavigationService>().AsSingleton();
            _container.Register<IDialogService, DialogService>().AsSingleton();
            _container.Register<ISettingsService, SettingsService>().AsSingleton();
            _container.Register<IRequestProvider, RequestProvider>().AsSingleton();
            _container.Register<IMeasureResultsService, MeasureResultsService>().AsSingleton();
            _container.Register<IDetectionService, DetectionService>().AsSingleton();
        }

        public static void UpdateDependencies(bool useMockServices)
        {
        }

        public static void RegisterSingleton<TInterface, T>() where TInterface : class where T : class, TInterface
        {
            _container.Register<TInterface, T>().AsSingleton();
        }

        public static T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }

        private static void OnAutoWireViewModelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var view = bindable as Element;
            if (view == null)
            {
                return;
            }

            var viewType = view.GetType();
            var viewName = viewType.FullName.Replace(".Views.", ".ViewModels.");
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;

            if (viewName.EndsWith("TabletView"))
            {
                viewName = viewName.Replace(".Tablet.", ".");
                viewName = viewName.Replace("Tablet", "");
            }
            var viewModelName = string.Format(CultureInfo.InvariantCulture, "{0}Model, {1}", viewName, viewAssemblyName);

            var viewModelType = Type.GetType(viewModelName);
            if (viewModelType == null)
            {
                return;
            }
            var viewModel = _container.Resolve(viewModelType);
            view.BindingContext = viewModel;
        }
    }
}
