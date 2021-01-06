using OLS.Casy.Models;
using Prism.Events;
using System;

namespace OLS.Casy.Core.Events
{
    public class KeyDownEvent : PubSubEvent<object> { }
    public class KeyUpEvent : PubSubEvent<object> { }
    public class ShowMultiButtonMessageBoxEvent : PubSubEvent<ShowMultiButtonMessageBoxDialogWrapper> { }
    public class ShowMessageBoxEvent : PubSubEvent<ShowMessageBoxDialogWrapper> { }
    public class ShowProgressEvent : PubSubEvent<ShowProgressDialogWrapper> { }
    public class ShowInputEvent : PubSubEvent<ShowInputDialogWrapper> { }
    public class ShowCustomDialogEvent : PubSubEvent<ShowCustomDialogWrapper> { }
    public class ShowLoginScreenEvent : PubSubEvent { }
    public class ErrorResultEvent : PubSubEvent<ErrorResult> { }
    public class ConfigurationChangedEvent : PubSubEvent { }
    public class TemplateSavedEvent : PubSubEvent<MeasureSetup> { }
    public class MeasureResultsDeletedEvent : PubSubEvent { }
    public class MeasureResultStoredEvent : PubSubEvent { }
    //public class ChartImageCapturedEvent : PubSubEvent<byte[]> { }
    public class ActiveNavigationCategoryChangedEvent : PubSubEvent<object> { }
    public class NavigateToEvent : PubSubEvent<object> { }
    public class ShowMeasureResultEvent : PubSubEvent<MeasureResult> { }
    public class AddMainControlsOverlayEvent : PubSubEvent<object> { }
    public class RemoveMainControlsOverlayEvent : PubSubEvent<object> { }
    public class ShowReportEvent : PubSubEvent<object> { }
    public class ExpandEvent : PubSubEvent { }
    public class ShowSettingsEvent : PubSubEvent { }
    public class ShowServiceEvent : PubSubEvent { }
    public class DeleteCursorEvent : PubSubEvent<Tuple<object, object>> { }
    public class RemoteCommandEvent: PubSubEvent<RemoteCommand> { }
    public class PrintAllMeasurementsEvent : PubSubEvent { }
}
