using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using OLS.Casy.Core;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISettingsCategoryViewModel))]
    public class MeanDocumentSettingsViewModel : ViewModelBase, ISettingsCategoryViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IDocumentSettingsManager _documentSettingsManager;

        private bool _isActive;
        private bool _isSelectedState;

        private ListCollectionView _availableDocumentSectionParameterSource;
        private readonly SmartCollection<DocumentSectionViewModel> _availableDocumentSectionParameterViewModels;

        private ListCollectionView _availableDocumentSectionResultsSource;
        private readonly SmartCollection<DocumentSectionViewModel> _availableDocumentSectionResultsViewModels;

        [ImportingConstructor]
        public MeanDocumentSettingsViewModel(
            IAuthenticationService authenticationService,
            ILocalizationService localizationService,
            IDocumentSettingsManager documentSettingsManager
        )
        {
            _authenticationService = authenticationService;
            _localizationService = localizationService;
            _documentSettingsManager = documentSettingsManager;

            _availableDocumentSectionParameterViewModels = new SmartCollection<DocumentSectionViewModel>();
            _availableDocumentSectionResultsViewModels = new SmartCollection<DocumentSectionViewModel>();
        }

        public ListCollectionView AvailableDocumentSectionParameterSource
        {
            get
            {
                if (_availableDocumentSectionParameterSource != null) return _availableDocumentSectionParameterSource;
                _availableDocumentSectionParameterSource =
                    CollectionViewSource.GetDefaultView(_availableDocumentSectionParameterViewModels) as ListCollectionView;
                _availableDocumentSectionParameterSource.SortDescriptions.Add(new SortDescription("DisplayOrder",
                    ListSortDirection.Ascending));
                _availableDocumentSectionParameterSource.IsLiveSorting = true;
                return _availableDocumentSectionParameterSource;
            }
        }

        public ListCollectionView AvailableDocumentSectionResultsSource
        {
            get
            {
                if (_availableDocumentSectionResultsSource != null) return _availableDocumentSectionResultsSource;
                _availableDocumentSectionResultsSource =
                    CollectionViewSource.GetDefaultView(_availableDocumentSectionResultsViewModels) as ListCollectionView;
                _availableDocumentSectionResultsSource.SortDescriptions.Add(new SortDescription("DisplayOrder",
                    ListSortDirection.Ascending));
                _availableDocumentSectionResultsSource.IsLiveSorting = true;
                return _availableDocumentSectionResultsSource;
            }
        }

        public bool IsVisible { get; } = true;
        public UserRole MinRequiredRole => _authenticationService.GetRoleByName("Supervisor");
        public PackIconFontAwesomeKind Glyph => PackIconFontAwesomeKind.None;
        public int Order => 4;
        public string Name => _localizationService.GetLocalizedString("MeanDocumentSettingsViewModel_Title");
        public ICommand SelectCommand => new OmniDelegateCommand(OnSelected);
        private void OnSelected()
        {
            IsActive = true;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                IsSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSelectedState
        {
            get => _isSelectedState;
            set
            {
                if (value == _isSelectedState) return;
                IsActive = value;
                _isSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public ChevronState ChevronState
        {
            get => ChevronState.Hide;
            set
            {
            }
        }

        public bool CanOk
        {
            get
            {
                var result = new List<ValidationResult>();
                return Validator.TryValidateObject(this, new ValidationContext(this, null, null), result);
            }
        }

        public void OnCancel()
        {
            _documentSettingsManager.LoadDocumentSettings(DocumentType.Mean);
            _availableDocumentSectionParameterViewModels.Clear();
            foreach (var documentSetting in _documentSettingsManager.MeanDocumentSettings)
            {
                if (documentSetting.Category == DocumentSettingCategory.Parameter)
                {
                    _availableDocumentSectionParameterViewModels.Add(new DocumentSectionViewModel(documentSetting, PerformParameterDrop));
                }
                else
                {
                    _availableDocumentSectionResultsViewModels.Add(new DocumentSectionViewModel(documentSetting, PerformResultsDrop));
                }
            }
        }

        public void OnOk()
        {
            _documentSettingsManager.SaveDocumentSettings(DocumentType.Mean);
        }

        private void PerformParameterDrop(IDraggable draggable, IDroppable droppable)
        {
            var origOrder = draggable.DisplayOrder;
            var newOrder = droppable.DisplayOrder;

            if (origOrder == newOrder) return;

            ((DocumentSectionViewModel)draggable).DisplayOrder = newOrder;

            var items = _availableDocumentSectionParameterViewModels.Where(item => item != draggable)
                .OrderBy(x => x.DisplayOrder).ToList();
            var order = 0;
            foreach (var item in items)
            {
                if (order == newOrder)
                {
                    order++;
                }

                item.DisplayOrder = order++;
            }
        }

        private void PerformResultsDrop(IDraggable draggable, IDroppable droppable)
        {
            var origOrder = draggable.DisplayOrder;
            var newOrder = droppable.DisplayOrder;

            if (origOrder == newOrder) return;

            ((DocumentSectionViewModel)draggable).DisplayOrder = newOrder;

            var items = _availableDocumentSectionResultsViewModels.Where(item => item != draggable)
                .OrderBy(x => x.DisplayOrder).ToList();
            var order = 0;
            foreach (var item in items)
            {
                if (order == newOrder)
                {
                    order++;
                }

                item.DisplayOrder = order++;
            }
        }

        public void OnImportsSatisfied()
        {
            foreach (var documentSetting in _documentSettingsManager.MeanDocumentSettings)
            {
                if (documentSetting.Category == DocumentSettingCategory.Parameter)
                {
                    _availableDocumentSectionParameterViewModels.Add(new DocumentSectionViewModel(documentSetting, PerformParameterDrop));
                }
                else
                {
                    _availableDocumentSectionResultsViewModels.Add(new DocumentSectionViewModel(documentSetting, PerformResultsDrop));
                }
            }
        }
    }
}
