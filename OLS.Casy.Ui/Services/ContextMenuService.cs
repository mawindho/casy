using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;

namespace OLS.Casy.Ui.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IContextMenuService))]
    public class ContextMenuService : IContextMenuService
    {
        private readonly IList<IContextMenuItemViewModel> _contextMenuItems;
        private readonly IList<IContextMenuItemViewModel> _subContextMenuItems;

        private IList<IContextMenuItemViewModel> _activeContextMenuItems;
        private IList<IContextMenuItemViewModel> _activeSubContextMenuItems;

        private Dictionary<string, List<Type>> _unsubscriptions;

        private Point _tooltipLocation;

        [ImportingConstructor]
        public ContextMenuService(IEventAggregatorProvider eventAggregatorProvider)
        {
            
            _contextMenuItems = new List<IContextMenuItemViewModel>();
            _subContextMenuItems = new List<IContextMenuItemViewModel>();
            _unsubscriptions = new Dictionary<string, List<Type>>();
        }

        public IList<IContextMenuItemViewModel> ActiveContextMenuItems
        {
            get { return _activeContextMenuItems; }
        }

        public IList<IContextMenuItemViewModel> ActiveSubContextMenuItems
        {
            get { return _activeSubContextMenuItems; }
        }

        public IContextMenuItemViewModel AddContextMenuItem(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, Func<object, IList<IContextMenuItemViewModel>> populateSubMenu, int displayOrder)
        {
            var contextMenuItem = new ContextMenuItemViewModel(contextMenuItemText, onContextMenuItemPressed, activeForDataContextTypes, populateSubMenu, displayOrder);
            _contextMenuItems.Add(contextMenuItem);
            return contextMenuItem;
        }

        public IContextMenuItemViewModel CreateContextMenuItem(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, int displayOrder)
        {
            return new ContextMenuItemViewModel(contextMenuItemText, onContextMenuItemPressed, activeForDataContextTypes, displayOrder);
        }

        internal Point TooltipLocation
        {
            get { return _tooltipLocation; }
        }

        internal event Action<IContextMenuItemViewModel, object> OpenSubMenuRequest;

        public void OpenSubMenu(IContextMenuItemViewModel sender, object dataContext)
        {
            if(OpenSubMenuRequest != null)
            {
                OpenSubMenuRequest.Invoke(sender, dataContext);
            }
        }

        public bool PopulateContextMenu(Point clickPoint, FrameworkElement originalSource)
        {
            bool result = false;

            List<IContextMenuItemViewModel> activeItems = new List<IContextMenuItemViewModel>();

            if (originalSource != null)
            {
                var activeDataContext = originalSource.DataContext;

                //bool isAisInformation = activeDataContext is AisInformation || (activeDataContext is Vessel && originalSource is Polyline);

                // Wir iterieren alle Elternobjekte der "OriginalSource" und schauen, ob eventuell noch "tiefer" angesiedelte Kontextmenü Einträge
                // in Frage kommen.

                do
                {
                    if (originalSource.DataContext != null)
                    {
                        // Kontextmenüeinträge kommen nur in Frage, wenn diese für den Typen des DataContext des mit der Maus angklickten Objektes registriert ist.
                        var currentOriginalSource = originalSource;
                        foreach (var contextItem in _contextMenuItems)
                        {
                            bool doAvoid = IsInElements(originalSource, contextItem.AvoidEntryForElements);
                            bool doShow = IsInElements(originalSource, contextItem.EntryForElementsOnly);

                            /*
                            bool skip = false;
                            if (_unsubscriptions.ContainsKey(contextItem.ContextMenuItemText) && _unsubscriptions[contextItem.ContextMenuItemText].Contains(Configuration.Instance.ActiveNavigationViewModelType))
                            {
                                skip = true;
                            }
                            */

                            if (!activeItems.Contains(contextItem)
                                && IsOfAnyBasetype(contextItem.ActiveForDataContextTypes, currentOriginalSource.DataContext)
                                && (contextItem.AvoidEntryForElements == null || contextItem.AvoidEntryForElements.Count() == 0 || !doAvoid)
                                && (contextItem.EntryForElementsOnly == null || contextItem.EntryForElementsOnly.Count() == 0 || doShow)
                                && (!contextItem.HasSubMenu || (contextItem.PopulateSubMenu != null && contextItem.PopulateSubMenu(activeDataContext).Count() > 0))
                                //&& !skip)
                                )
                            {
                                /*
                                AppointmentItemProxy appointmentItemProxy = activeDataContext as AppointmentItemProxy;
                                
                                if (activeDataContext is IContextMenuBaseType)
                                {
                                    var contextMenuBaseType = activeDataContext as IContextMenuBaseType;
                                    var baseTypeDataContext = contextMenuBaseType.GetContextMenuObject();
                                    contextItem.DataContext = baseTypeDataContext;
                                }
                                else if (appointmentItemProxy != null)
                                {
                                    var contextMenuBaseType = appointmentItemProxy.Appointment as IContextMenuBaseType;
                                    var baseTypeDataContext = contextMenuBaseType.GetContextMenuObject();
                                    contextItem.DataContext = baseTypeDataContext;
                                }
                                else
                                {
                                    contextItem.DataContext = activeDataContext;
                                }
                                */
                                //if (UserIsAllowedToSeeContextItem(contextItem))
                                //{
                                //activeItems.Add(contextItem);
                                //}
                                contextItem.DataContext = activeDataContext;
                                activeItems.Add(contextItem);
                            }
                        }
                    }

                    originalSource = originalSource.Parent as FrameworkElement;
                }
                while (originalSource != null);
            }

            activeItems = activeItems.OrderBy(item => item.DisplayOrder).ToList();

            // Das Kontextmenü wird nur dann angezeigt, wenn es mindestens einen Eintrag gibt.
            if (activeItems.Count > 0)
            {
                _tooltipLocation = new Point(clickPoint.X - 10, clickPoint.Y - 10);
                RaiseTooltipPositionChanged();

                _activeContextMenuItems = activeItems;
                RaiseActiveContextMenuItemsChanged();

                result = true;
            }

            return result;
        }

        public void ClearContextMenuItems()
        {
            _activeContextMenuItems = null;
            _activeSubContextMenuItems = null;
            RaiseActiveContextMenuItemsChanged();
            RaiseActiveSubContextMenuItemsChanged();
        }

        public void UnsubscribeForContextMenuItem(string contextMenuItemName, Type navigationViewModelType)
        {
            throw new NotImplementedException();
        }

        public event EventHandler ActiveContextMenuItemsChanged;

        private void RaiseActiveContextMenuItemsChanged()
        {
            if (ActiveContextMenuItemsChanged != null)
            {
                ActiveContextMenuItemsChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ActiveSubContextMenuItemsChanged;

        private void RaiseActiveSubContextMenuItemsChanged()
        {
            if (ActiveSubContextMenuItemsChanged != null)
            {
                ActiveSubContextMenuItemsChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler TooltipPositionChanged;

        private void RaiseTooltipPositionChanged()
        {
            if (TooltipPositionChanged != null)
            {
                TooltipPositionChanged.Invoke(this, EventArgs.Empty);
            }
        }

        private static bool IsOfAnyBasetype(IEnumerable<Type> types, object testObject)
        {
            if (types != null)
            {
                /*
                IContextMenuBaseType contextMenuBaseType = testObject as IContextMenuBaseType;
                if (contextMenuBaseType != null)
                {
                    Type type = contextMenuBaseType.GetBaseTypeForContextMenu();
                    var contextmenuobject = Convert.ChangeType(contextMenuBaseType.GetContextMenuObject(), type, null);
                    return IsOfAnyBasetype(types, contextmenuobject);
                }

                AppointmentItemProxy appointmentItemProxy = testObject as AppointmentItemProxy;
                if (appointmentItemProxy != null)
                {
                    return IsOfAnyBasetype(types, appointmentItemProxy.Appointment);
                }

                var resourceTreeItem = testObject as IResourceTreeItem;
                if (resourceTreeItem != null && resourceTreeItem.ResourceBase != null)
                {
                    return IsOfAnyBasetype(types, resourceTreeItem.ResourceBase);
                }

                if (resourceTreeItem != null && resourceTreeItem.ResourceGroup != null)
                {
                    return IsOfAnyBasetype(types, resourceTreeItem.ResourceGroup);
               */

                return types.Any(type => type.IsInstanceOfType(testObject));
            }

            return false;
        }

        private static bool IsInElements(FrameworkElement element, IEnumerable<string> elementNames)
        {
            if (elementNames == null || elementNames.Count() <= 0)
            {
                return false;
            }

            bool result = elementNames.Contains(element.Name);

            if (!result && element.Parent != null && element.Parent is FrameworkElement)
            {
                return IsInElements(element.Parent as FrameworkElement, elementNames);
            }

            return result;
        }
    }
}
