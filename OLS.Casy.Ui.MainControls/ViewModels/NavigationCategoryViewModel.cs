using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System;
using System.Windows.Input;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    public class NavigationCategoryViewModel : ViewModelBase
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private bool _isSelected;
        private UserRole _minRequiredRole;
        private string _glyph;
        private PackIconFontAwesomeKind _awesomeGlyph = PackIconFontAwesomeKind.None;
        private ChevronState _chevronState;
        private bool _isVisible = true;
        private bool _canSelect;
        private bool _isSelectedState;

        public NavigationCategoryViewModel(
            NavigationCategory ribbonPage, 
            UserRole minRequiredRole,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            NavigationCategory = ribbonPage;
            _minRequiredRole = minRequiredRole;
            _eventAggregatorProvider = eventAggregatorProvider;

            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateTo);
        }

        public string Name => $"NavigationCategory_{Enum.GetName(typeof(NavigationCategory), NavigationCategory)}";

        public NavigationCategory NavigationCategory { get; }

        public UserRole MinRequiredRole
        {
            get => _minRequiredRole;
            set
            {
                if (value == _minRequiredRole) return;
                _minRequiredRole = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!CanSelect || value == _isSelected) return;
                _isSelected = value;
                NotifyOfPropertyChange();

                if(CanSelect)
                {
                    IsSelectedState = value;
                }
            }
        }

        public bool IsSelectedState
        {
            get => _isSelectedState;
            set
            {
                if (value == _isSelectedState) return;
                IsSelected = value;

                if (!CanSelect) return;
                _isSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanSelect
        {
            get => _canSelect;
            set
            {
                if (value == _canSelect) return;
                _canSelect = value;
                NotifyOfPropertyChange();
            }
        }

        public string Glyph
        {
            get => _glyph;
            set
            {
                if (value == _glyph) return;
                _glyph = value;
                NotifyOfPropertyChange();
            }
        }

        public PackIconFontAwesomeKind AwesomeGlyph
        {
            get => _awesomeGlyph;
            set
            {
                if (value == _awesomeGlyph) return;
                _awesomeGlyph = value;
                NotifyOfPropertyChange();
            }
        }

        public ChevronState ChevronState
        {
            get => _chevronState;
            set
            {
                if (value == _chevronState) return;
                _chevronState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                this._isVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand SelectCommand => new OmniDelegateCommand(OnSelected);

        private void OnSelected()
        {
            IsSelected = true;
            _eventAggregatorProvider.Instance.GetEvent<ActiveNavigationCategoryChangedEvent>().Publish(NavigationCategory);
        }

        private void OnNavigateTo(object argument)
        {
            var navigationArgs = (NavigationArgs)argument;

            if (navigationArgs.NavigationCategory != NavigationCategory) return;
            if (CanSelect)
            {
                IsSelectedState = true;
            }
        }

    }
}
