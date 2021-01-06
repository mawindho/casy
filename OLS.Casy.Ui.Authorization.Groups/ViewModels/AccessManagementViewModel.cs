using DevExpress.Mvvm;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Authorization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace OLS.Casy.Ui.Authorization.Access.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IAccessManagementViewModel))]
    public class AccessManagementViewModel : DialogModelBase, IAccessManagementViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IAccessManager _accessManager;
        private readonly IMeasureResultManager _measureResultManager;

        private bool _isGroupsChecked;
        private bool _isUsersChecked = true;

        private MeasureResult _measureResult;
        private List<MeasureResultAccessMapping> _accessMappings;

        private ObservableCollection<object> _availableOptions;
        private ObservableCollection<MeasureResultAccessMapping> _selectedOptions;

        private ListCollectionView _availableOptionsViewSource;
        private ListCollectionView _selectedOptionsViewSource;

        [ImportingConstructor]
        public AccessManagementViewModel(IAuthenticationService authenticationService,
            IAccessManager accessManager,
            IMeasureResultManager measureResultManager)
        {
            this._authenticationService = authenticationService;
            this._accessManager = accessManager;
            this._measureResultManager = measureResultManager;

            this._availableOptions = new ObservableCollection<object>();
            this._selectedOptions = new ObservableCollection<MeasureResultAccessMapping>();
        }

        public ListCollectionView AvailableOptionsViewSource
        {
            get
            {
                if (_availableOptionsViewSource == null)
                {
                    _availableOptionsViewSource = CollectionViewSource.GetDefaultView(this._availableOptions) as ListCollectionView;
                    _availableOptionsViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _availableOptionsViewSource.IsLiveSorting = true;
                }
                return _availableOptionsViewSource;
            }
        }

        public ListCollectionView SelectedOptionsViewSource
        {
            get
            {
                if (_selectedOptionsViewSource == null)
                {
                    _selectedOptionsViewSource = CollectionViewSource.GetDefaultView(this._selectedOptions) as ListCollectionView;
                    _selectedOptionsViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _selectedOptionsViewSource.IsLiveSorting = true;
                }
                return _selectedOptionsViewSource;
            }
        }

        public bool IsGroupsChecked
        {
            get { return _isGroupsChecked; }
            set
            {
                if(value != this._isGroupsChecked)
                {
                    this._isGroupsChecked = value;
                    NotifyOfPropertyChange();

                    if (value)
                    {
                        this.IsUsersChecked = false;
                        FillAvailableOptions();
                    }
                }
            }
        }

        public bool IsUsersChecked
        {
            get { return _isUsersChecked; }
            set
            {
                if (value != this._isUsersChecked)
                {
                    this._isUsersChecked = value;
                    NotifyOfPropertyChange();

                    if (value)
                    {
                        this.IsGroupsChecked = false;
                        FillAvailableOptions();
                    }
                }
            }
        }

        public void SetAssociatedMeasureResult(MeasureResult measureResult)
        {
            this._measureResult = measureResult;
            this._accessMappings = measureResult.AccessMappings.ToList();

            foreach (var accessMapping in _accessMappings)
            {
                if(accessMapping.UserId != null)
                {
                    accessMapping.User = this._authenticationService.GetUser(accessMapping.UserId.Value);
                }
                else if(accessMapping.UserGroupId != null)
                {
                    accessMapping.UserGroup = this._accessManager.GetUserGroup(accessMapping.UserGroupId.Value);
                }

                this._selectedOptions.Add(accessMapping);
            }
            FillAvailableOptions();
        }

        public ICommand AddOptionCommand
        {
            get { return new OmniDelegateCommand<object>(OnAddOption); }
        }

        public ICommand RemoveOptionCommand
        {
            get { return new OmniDelegateCommand<MeasureResultAccessMapping>(OnRemoveOption); }
        }

        public void OnImportsSatisfied()
        {
            this.Title = "Berechtigungsverwaltung";
            FillAvailableOptions();
        }

        protected override void OnOk()
        {
            var toRemoves = this._measureResult.AccessMappings.Where(am => !this._accessMappings.Any(am2 => am2.MeasureResultAccessMappingId == am.MeasureResultAccessMappingId)).ToList();
            foreach (var toRemove in toRemoves)
            {
                this._measureResult.AccessMappings.Remove(toRemove);
            }

            foreach(var accessMapping in this._accessMappings)
            {
                if(accessMapping.MeasureResultAccessMappingId == 0)                
                {
                    this._measureResult.AccessMappings.Add(accessMapping);
                }
                else
                {
                    var existing = this._measureResult.AccessMappings.FirstOrDefault(item => item.MeasureResultAccessMappingId == accessMapping.MeasureResultAccessMappingId);
                    if (existing == null)
                    {
                        existing.CanRead = accessMapping.CanRead;
                        existing.CanWrite = accessMapping.CanWrite;
                    }
                }
            }
            
            this._measureResultManager.SaveMeasureResults(new [] { this._measureResult }, showConfirmationScreen: false);
            base.OnOk();
        }

        protected override void OnCancel()
        {
            base.OnCancel();
        }

        private void FillAvailableOptions()
        {
            this._availableOptions.Clear();
            if(this.IsGroupsChecked)
            {
                foreach(var userGroup in this._accessManager.UserGroups)
                {
                    if (!this._selectedOptions.Any(am => am.UserGroupId == userGroup.Id))
                    {
                        this._availableOptions.Add(userGroup);
                    }
                }
            }
            else if(this.IsUsersChecked)
            {
                foreach(var user in this._authenticationService.UsersList)
                {
                    if (!user.IsEmergencyUser && !this._selectedOptions.Any(am => am.UserId == user.Id))
                    {
                        this._availableOptions.Add(user);
                    }
                }
            }
        }

        private void OnAddOption(object option)
        {
            MeasureResultAccessMapping accessMapping = new MeasureResultAccessMapping()
            {
                AccessMode = AccessMode.Read,
                MeasureResult = this._measureResult
            };

            User user = option as User;
            if(user != null)
            {
                accessMapping.User = user;
                accessMapping.UserId = user.Id;
            }

            UserGroup userGroup = option as UserGroup;
            if(userGroup != null)
            {
                accessMapping.UserGroup = userGroup;
                accessMapping.UserGroupId = userGroup.Id;                
            }

            this._accessMappings.Add(accessMapping);
            this._availableOptions.Remove(option);
            this._selectedOptions.Add(accessMapping);
        }

        private void OnRemoveOption(MeasureResultAccessMapping measureResultAccessMapping)
        {
            this._accessMappings.Remove(measureResultAccessMapping);
            this._selectedOptions.Remove(measureResultAccessMapping);
            this.FillAvailableOptions();
        }
    }
}
