using System;
using System.Collections.Generic;
using System.Windows;

namespace OLS.Casy.Ui.Api
{
    public enum ContextMenuItemState
    {
        Uncheckable,
        Checked,
        Unchecked
    }

    public interface IContextMenuItemViewModel
    {
        bool HasSubMenu { get; }
        Point CurrentMouseLocation { get; }
        IList<IContextMenuItemViewModel> SubContextMenuItems { get; }
        IContextMenuItemViewModel ParentViewModel { get; set; }

        string ContextMenuItemText { get; }
        IEnumerable<Type> ActiveForDataContextTypes { get; }
        int DisplayOrder { get; }
        object DataContext { get; set; }

        Func<object, IEnumerable<IContextMenuItemViewModel>> PopulateSubMenu { get; }

        IEnumerable<string> AvoidEntryForElements { get; set; }
        IEnumerable<string> EntryForElementsOnly { get; set; }

        Func<IContextMenuItemViewModel, object, bool> IsContextMenuItemChecked { get; set; }
    }
}
