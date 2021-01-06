using System;
using System.Collections.Generic;
using System.Windows;

namespace OLS.Casy.Ui.Api
{
    public interface IContextMenuService
    {
        IContextMenuItemViewModel AddContextMenuItem(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, Func<object, IList<IContextMenuItemViewModel>> populateSubMenu, int displayOrder);
        IContextMenuItemViewModel CreateContextMenuItem(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, int displayOrder);

        //bool PopulateContextMenu(Point clickPoint, FrameworkElement originalSource);
        void ClearContextMenuItems();
        void UnsubscribeForContextMenuItem(string contextMenuItemName, Type navigationViewModelType);

        void OpenSubMenu(IContextMenuItemViewModel sender, object dataContext);

        //Point TooltipLocation { get; }

        //IList<IContextMenuItemViewModel> ActiveContextMenuItems { get; }
        //IList<IContextMenuItemViewModel> ActiveSubContextMenuItems { get; }

        //event Action<IContextMenuItemViewModel, object> OpenSubMenuRequest;
        //event EventHandler ActiveContextMenuItemsChanged;
        //event EventHandler ActiveSubContextMenuItemsChanged;
        //event EventHandler TooltipPositionChanged;
    }
}
