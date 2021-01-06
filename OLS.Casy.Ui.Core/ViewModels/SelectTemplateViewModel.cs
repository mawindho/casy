using DevExpress.Mvvm;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ISelectTemplateViewModel))]
    public class SelectTemplateViewModel : Base.ViewModelBase, ISelectTemplateViewModel, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IMeasureController _measureController;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;

        private ObservableCollection<ITemplateViewModel> _templates;
        private ListCollectionView _templatesViewSource;
        private bool _showSettings;

        private SubscriptionToken _setupSavedSubscription;
        private ICommand _editTemplateCommand;

        [ImportingConstructor]
        public SelectTemplateViewModel(
            IDatabaseStorageService databaseStorageService,
            IMeasureController measureController,
            IAuthenticationService authenticationService,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService)
        {
            this._databaseStorageService = databaseStorageService;
            this._measureController = measureController;
            this._authenticationService = authenticationService;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._localizationService = localizationService;

            this._templates = new ObservableCollection<ITemplateViewModel>();
        }

        public ObservableCollection<ITemplateViewModel> TemplateViewModels
        {
            get { return _templates; }
        }

        public ListCollectionView Templates
        {
            get
            {
                if (this._templatesViewSource == null)
                {
                    _templatesViewSource = CollectionViewSource.GetDefaultView(this._templates) as ListCollectionView;
                    _templatesViewSource.SortDescriptions.Add(new SortDescription("IsFavorite", ListSortDirection.Descending));
                    _templatesViewSource.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
                    _templatesViewSource.IsLiveSorting = true;
                    _templatesViewSource.Filter = new Predicate<object>(FilterViewSource);
                    //_templatesViewSource.LiveFilteringProperties.Add("Filter");
                    _templatesViewSource.IsLiveFiltering = true;
                }
                return this._templatesViewSource;
            }
        }

        public string Filter
        {
            get { return _filter; }
            set
            {
                if (value != _filter)
                {
                    this._filter = value;
                    NotifyOfPropertyChange();

                    if (_templatesViewSource != null)
                    {
                        _templatesViewSource.Refresh();
                    }
                }
            }
        }

        public bool ShowSettings
        {
            get { return _showSettings; }
            set
            {
                if(value != _showSettings)
                {
                    this._showSettings = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand EditTemplateCommand
        {
            get { return this._editTemplateCommand; }
            set
            {
                if(value != this._editTemplateCommand)
                {
                    this._editTemplateCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand ToggleFavoriteCommand
        {
            get { return new OmniDelegateCommand<object>(OnToggleFavorite); }
        }

        public ICommand DeleteTemplateCommand
        {
            get { return this._deleteTemplateCommand; }
            set
            {
                if (value != this._deleteTemplateCommand)
                {
                    this._deleteTemplateCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public void OnImportsSatisfied()
        {
            _setupSavedSubscription = this._eventAggregatorProvider.Instance.GetEvent<TemplateSavedEvent>().Subscribe(OnSetupSaved);

            this._authenticationService.UserLoggedIn += OnUserLoggedIn;
            this._authenticationService.UserLoggedOut += OnUserLogedOut;

            LoadTemplates();
        }

        public void LoadTemplates()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this._templates.Clear();

                var recentIds = this._authenticationService.LoggedInUser.RecentTemplateIds;

                var templates = _databaseStorageService.GetMeasureSetupTemplates().Select(template => new TemplateViewModel(template, this._measureController, this._localizationService));

                foreach (var template in templates)
                {
                    if(recentIds.Contains(template.TemplateId))
                    {
                        template.Order = recentIds.IndexOf(template.TemplateId);
                    }
                    else
                    {
                        template.Order = int.MaxValue;
                    }

                    this._templates.Add(template);
                }

                if (this._measureController.SelectedTemplate != null)
                {
                    var selected = this._templates.FirstOrDefault(t => t.TemplateId == this._measureController.SelectedTemplate.MeasureSetupId);

                    if (selected != null)
                    {
                        selected.IsSelected = true;
                    }
                }

                OnUserLoggedIn(null, null);
            });
        }

        private void OnUserLogedOut(object sender, AuthenticationEventArgs e)
        {
            foreach (var template in this._templates)
            {
                template.IsFavorite = false;
            }
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            if(this._authenticationService.LoggedInUser != null)
            { 
                foreach (var id in _authenticationService.LoggedInUser.FavoriteTemplateIds.Distinct())
                {
                    var template = this._templates.FirstOrDefault(t => t.TemplateId == id);
                    if(template != null)
                    {
                        template.IsFavorite = true;
                    }
                }
            }
        }

        private void OnToggleFavorite(object obj)
        {
            TemplateViewModel templateViewModel = obj as TemplateViewModel;
            if(templateViewModel != null)
            {
                if(templateViewModel.IsFavorite)
                {
                    templateViewModel.IsFavorite = false;
                    _authenticationService.LoggedInUser.FavoriteTemplateIds.Remove(templateViewModel.TemplateId);
                }
                else
                {
                    templateViewModel.IsFavorite = true;
                    _authenticationService.LoggedInUser.FavoriteTemplateIds.Insert(0, templateViewModel.TemplateId);
                }

                if(_authenticationService.LoggedInUser.FavoriteTemplateIds.Count > 12)
                {
                    var lastId = _authenticationService.LoggedInUser.FavoriteTemplateIds.Last();
                    var template = this._templates.FirstOrDefault(t => t.TemplateId == lastId) as TemplateViewModel;
                    template.IsFavorite = false;
                    _authenticationService.LoggedInUser.FavoriteTemplateIds.Remove(lastId);
                }

                this._authenticationService.SaveUser(this._authenticationService.LoggedInUser);
                this._eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Publish();
            }
        }

        private void OnSetupSaved(MeasureSetup obj)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                LoadTemplates();
            });
        }

        private bool FilterViewSource(object obj)
        {
            TemplateViewModel templateViewModel = obj as TemplateViewModel;
            if (templateViewModel == null)
            {
                return true;
            }
            return string.IsNullOrEmpty(_filter) || templateViewModel.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 || templateViewModel.Capillary.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 || templateViewModel.ToDiameter.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool disposedValue = false; // To detect redundant calls
        private ICommand _deleteTemplateCommand;
        private string _filter;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._authenticationService.UserLoggedIn -= OnUserLoggedIn;
                    this._authenticationService.UserLoggedOut -= OnUserLogedOut;

                    this._eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Unsubscribe(_setupSavedSubscription);
                }

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
