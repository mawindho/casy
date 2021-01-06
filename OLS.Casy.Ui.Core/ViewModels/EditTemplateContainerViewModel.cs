using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Views;
using OLS.Casy.Ui.MainControls.Api;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(HostViewModel))]
    public class EditTemplateContainerViewModel : HostViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;

        [ImportingConstructor]
        public EditTemplateContainerViewModel(
            IEventAggregatorProvider eventAggregatorProvider,
            ICompositionFactory compositionFactory
            )
        {
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._compositionFactory = compositionFactory;
        }

        public void OnImportsSatisfied()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateTo);
        }

        private void OnNavigateTo(object argument)
        {
            NavigationArgs navigationCatgory = (NavigationArgs)argument;
            Task.Factory.StartNew(() =>
            {
                switch (navigationCatgory.NavigationCategory)
                {
                    case NavigationCategory.Template:
                        var awaiter = new System.Threading.ManualResetEvent(false);
                        var viewModelExport = this._compositionFactory.GetExport<EditTemplateDialogModel>();
                        var viewModel = viewModelExport.Value;

                        ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper()
                        {
                            Awaiter = awaiter,
                            DataContext = viewModel,
                            DialogType = typeof(EditTemplateDialog)
                        };

                        this._eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                        awaiter.WaitOne();

                        this._compositionFactory.ReleaseExport(viewModelExport);

                        break;
                }
            });
        }
    }
}
