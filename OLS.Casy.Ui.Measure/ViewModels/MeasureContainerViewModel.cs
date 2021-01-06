using OLS.Casy.Controller.Measure;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Ui.Measure.Views;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OLS.Casy.Controller.Api;

namespace OLS.Casy.Ui.Measure.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(HostViewModel))]
    public class MeasureContainerViewModel : HostViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IMeasureController _measureController;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IEnvironmentService _environmentService;
        private readonly ILocalizationService _localizationService;
        
        [ImportingConstructor]
        public MeasureContainerViewModel(
            ICompositionFactory compositionFactory,
            IEventAggregatorProvider eventAggregatorProvider,
            IMeasureController measureController,
            IMeasureResultManager measureResultManager,
            IEnvironmentService environmentService,
            ILocalizationService localizationService
            )
        {
            _measureController = measureController;
            _measureResultManager = measureResultManager;
            _environmentService = environmentService;
            this._compositionFactory = compositionFactory;
            this._eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
        }

        public void OnImportsSatisfied()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);
            this._eventAggregatorProvider.Instance.GetEvent<KeyDownEvent>().Subscribe(OnKeyDown);
        }

        [Export("CheckIdentifier")]
        public bool CheckIdentifier(int identifier)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"Software\Casy", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
            var identifiers = key.GetValue("Identifiers", string.Empty).ToString();

            var containsIdentifier = identifiers.Split(';').Contains(identifier.ToString());

            if (!containsIdentifier)
            {
                key.SetValue("Identifiers", identifiers + ";" + identifier.ToString());
                return true;
            }
            return false;
        }

        private void OnNavigateToEvent(object argument)
        {
            NavigationArgs navigationArgs = (NavigationArgs)argument;

            Task.Factory.StartNew(() =>
            {
                switch (navigationArgs.NavigationCategory)
                {
                    case NavigationCategory.Measurement:
                        if (!this.IsActive)
                        {
                            //_isActive = true;
                            var awaiter = new System.Threading.ManualResetEvent(false);
                            var viewModelExport = this._compositionFactory.GetExport<StartMeasureDialogViewModel>();
                            var viewModel = viewModelExport.Value;

                            if (navigationArgs.Parameter != null && navigationArgs.Parameter is string parameter)
                            {
                                viewModel.CustomRepeats = int.Parse(parameter);
                            }

                            ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper()
                            {
                                Awaiter = awaiter,
                                DataContext = viewModel,
                                DialogType = typeof(StartMeasureDialog)
                            };

                            this._eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                            if (awaiter.WaitOne())
                            {
                                if (!viewModel.IsCancel)
                                {
                                    this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
                                    {
                                        Parameter = true
                                    });
                                }
                                //IsActive = false;
                            }

                            this._compositionFactory.ReleaseExport(viewModelExport);
                        }

                        break;
                }
            });
        }

        private void OnKeyDown(object obj)
        {
            var keyEventArgs = obj as KeyEventArgs;

            if (keyEventArgs.Key == Key.F5)
            {
                if (_measureController.SelectedTemplate != null)
                {
                    RemoteMeasure();
                }
            }
        }

        private async void RemoteMeasure()
        {
            var result = await Task.Run(async () => await _measureResultManager.SaveChangedMeasureResults());
            if (result == ButtonResult.Cancel)
            {
                return;
            }

            await Task.Run(async () =>
            {
                this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(
                    new NavigationArgs(NavigationCategory.AnalyseGraph)
                    {
                        Parameter = true
                    });

                var now = DateTime.Now;

                var colorName = "ChartColor1";

                var measureSetup = _measureController.SelectedTemplate;
                if (_measureController.SelectedTemplate.MeasureSetupId != -1)
                {
                    measureSetup = _measureResultManager.CloneTemplate(_measureController.SelectedTemplate);
                }

                var isCfrObject = _environmentService.GetEnvironmentInfo("cfrEnabled");

                var measureResult = new MeasureResult
                {
                    MeasureResultGuid = Guid.NewGuid(),
                    MeasureSetup = measureSetup,
                    OriginalMeasureSetup = measureSetup,
                    Name = string.Format(_localizationService.GetLocalizedString(
                        "MeasureController_TemporaryMeasureResultName",
                        (string.IsNullOrEmpty(_measureController.SelectedTemplate.Name)
                            ? _localizationService.GetLocalizedString("MeasureController_TemporaryTemplateName")
                            : _measureController.SelectedTemplate.Name).ToUpper(), now.ToString("yyyy-MM-dd HH-mm"))),
                    Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString(),
                    MeasuredAt = DateTime.Now,
                    MeasuredAtTimeZone = TimeZoneInfo.Local,
                    IsCfr = isCfrObject != null
                };
                measureSetup.MeasureResult = measureResult;

                await _measureResultManager.AddSelectedMeasureResults(new[] { measureResult });

                var storedMeasureResult = await _measureController.Measure(measureResult);

                await _measureResultManager.ReplaceSelectedMeasureResult(measureResult, storedMeasureResult);

                if (!_measureController.SelectedTemplate.IsTemplate)
                {
                    _measureController.SelectedTemplate = null;
                }
            });
        }
    }
}
