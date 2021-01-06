using DevExpress.Xpf.Charts;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base
{
    public class MapKeyEventToCommand : MapEventToCommandBase<KeyEventArgs> { }

    public class MapRoutedEventToCommand : MapEventToCommandBase<RoutedEventArgs> { }

    public class MapMouseButtonEventToCommand : MapEventToCommandBase<MouseButtonEventArgs> { }

    public class MapTouchEventToCommand : MapEventToCommandBase<TouchEventArgs> { }

    public class MapXYDiagram2DZoomEventToCommand : MapEventToCommandBase<XYDiagram2DZoomEventArgs> { }
    public class MapXYDiagram2DScrollEventToCommand : MapEventToCommandBase<XYDiagram2DScrollEventArgs> { }

    public class MapCustomDrawSeriesEventToCommand : MapEventToCommandBase<CustomDrawSeriesEventArgs> { }

    public abstract class MapEventToCommandBase<TEventArgsType> : TriggerAction<FrameworkElement>
        where TEventArgsType : EventArgs
    {
        /// <summary>
        ///     Dependecy property for the command to be executed when the specific evet is raised.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(OmniDelegateCommand<EventInformation<TEventArgsType>>),
            typeof(MapEventToCommandBase<TEventArgsType>),
            new PropertyMetadata(null, OnCommandPropertyChanged));

        /// <summary>
        ///     Dependecy property for the parameter for the command passed when the specific evet is raised.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter",
            typeof(object),
            typeof(MapEventToCommandBase<TEventArgsType>),
            new PropertyMetadata(null, OnCommandParameterPropertyChanged));

        /// <summary>
        ///     Getter/Setter for the 'Command' dependency property
        /// </summary>
        public OmniDelegateCommand<EventInformation<TEventArgsType>> Command
        {
            get { return this.GetValue(CommandProperty) as OmniDelegateCommand<EventInformation<TEventArgsType>>; }
            set { this.SetValue(CommandProperty, value); }
        }

        /// <summary>
        ///     Getter/Setter for the 'CommandParameter' dependency property
        /// </summary>
        public object CommandParameter
        {
            get { return this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        private static void OnCommandParameterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var invokeCommand = d as MapEventToCommandBase<TEventArgsType>;
            if (invokeCommand != null)
            {
                invokeCommand.SetValue(CommandParameterProperty, e.NewValue);
            }
        }

        private static void OnCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var invokeCommand = d as MapEventToCommandBase<TEventArgsType>;
            if (invokeCommand != null)
            {
                invokeCommand.SetValue(CommandProperty, e.NewValue);
            }
        }

        /// <summary>
        ///     Invokes the action.
        /// </summary>
        /// <param name="parameter">Param for the action. If te action does no require any parameter, the parameter is set null.</param>
        protected override void Invoke(object parameter)
        {
            if (this.Command == null)
            {
                return;
            }

            object commmandParam = this.GetValue(CommandParameterProperty);
            var eventArgs = parameter as TEventArgsType;

            if (this.AssociatedObject != null)
            {
                var commandEventArgs = new EventInformation<TEventArgsType>(this.AssociatedObject, eventArgs, commmandParam);

                if (this.Command.CanExecute(commandEventArgs))
                {
                    this.Command.Execute(commandEventArgs);
                }
            }
        }
    }
}
