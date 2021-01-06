using OLS.Casy.Core;
using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultSubTreeViewModel))]
    public class MeasureResultSubTreeViewModel : ViewModelBase
    {
        private readonly SmartCollection<MeasureResultTreeItemViewModel> _measureResultTreeItemViewModels;
        private MeasureResultTreeItemType _treeItemType = MeasureResultTreeItemType.None;
        private string _navigateBackButtonText;
        private ICommand _navigateBackCommand;
        private ICommand _selectAllCommand;

        [ImportingConstructor]
        public MeasureResultSubTreeViewModel()
        {
            this._measureResultTreeItemViewModels = new SmartCollection<MeasureResultTreeItemViewModel>();
        }

        public ICommand NavigateBackCommand
        {
            get { return _navigateBackCommand; }
            set
            {
                if (value != _navigateBackCommand)
                {
                    this._navigateBackCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand SelectAllCommand
        {
            get { return _selectAllCommand; }
            set
            {
                if (value != _selectAllCommand)
                {
                    this._selectAllCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public MeasureResultTreeItemType MeasureResultTreeItemType
        {
            get { return _treeItemType; }
            set
            {
                if(value != _treeItemType)
                {
                    this._treeItemType = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string NavigateBackButtonText
        {
            get { return _navigateBackButtonText; }
            set
            {
                if (value != _navigateBackButtonText)
                {
                    this._navigateBackButtonText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public SmartCollection<MeasureResultTreeItemViewModel> MeasureResultTreeItemViewModels
        {
            get { return _measureResultTreeItemViewModels; }
        }
    }
}
