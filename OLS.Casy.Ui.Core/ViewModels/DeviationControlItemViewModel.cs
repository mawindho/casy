using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviationControlItemViewModel))]
    public class DeviationControlItemViewModel : ViewModelBase, IComparable, IComparable<DeviationControlItemViewModel>
    {
        private IUIProjectManager _uiProject;

        private DeviationControlItem _deviationControlItem;

        public DeviationControlItemViewModel()
        {
            this.DeviationControlItem = new DeviationControlItem();
        }

        [ImportingConstructor]
        public DeviationControlItemViewModel(IUIProjectManager uiProject)
        {
            this._uiProject = uiProject;
        }

        public IUIProjectManager UiProject
        {
            get { return _uiProject; }
            set { _uiProject = value; }
        }

        public DeviationControlItem DeviationControlItem
        {
            get { return _deviationControlItem; }
            set
            {
                _deviationControlItem = value;
            }
        }

        public MeasureResultItemTypes MeasureResultItemType
        {
            get { return _deviationControlItem.MeasureResultItemType; }
            set
            {
                this._uiProject.SendUIEdit(_deviationControlItem, "MeasureResultItemType", value);
                NotifyOfPropertyChange();
            }
        }

        public double? MinLimit
        {
            get { return _deviationControlItem.MinLimit; }
            set
            {
                this._uiProject.SendUIEdit(_deviationControlItem, "MinLimit", value);
                NotifyOfPropertyChange();
            }
        }

        public double? MaxLimit
        {
            get { return _deviationControlItem.MaxLimit; }
            set
            {
                this._uiProject.SendUIEdit(_deviationControlItem, "MaxLimit", value);
                NotifyOfPropertyChange();
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            DeviationControlItemViewModel other = obj as DeviationControlItemViewModel;
            if (other == null)
            {
                throw new ArgumentException("A DeviationControlItemViewModel object is required for comparison.", "obj");
            }
            return this.CompareTo(other);
        }

        public int CompareTo(DeviationControlItemViewModel other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }
            
            return other.DeviationControlItem.MeasureResultItemType.CompareTo(this.DeviationControlItem.MeasureResultItemType);
        }

        public static int Compare(DeviationControlItemViewModel left, DeviationControlItemViewModel right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }
            if (object.ReferenceEquals(left, null))
            {
                return -1;
            }
            return left.CompareTo(right);
        }

        public override bool Equals(object obj)
        {
            DeviationControlItemViewModel other = obj as DeviationControlItemViewModel;
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Maybe nullity checks, if these are objects not primitives!
                hash = hash * 23 + MeasureResultItemType.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(DeviationControlItemViewModel left, DeviationControlItemViewModel right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }
        public static bool operator !=(DeviationControlItemViewModel left, DeviationControlItemViewModel right)
        {
            return !(left == right);
        }
        public static bool operator <(DeviationControlItemViewModel left, DeviationControlItemViewModel right)
        {
            return (Compare(left, right) < 0);
        }
        public static bool operator >(DeviationControlItemViewModel left, DeviationControlItemViewModel right)
        {
            return (Compare(left, right) > 0);
        }

    }
}
