using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;
using NativeImage = UIKit.UIImage;

[assembly: ExportRenderer(typeof(Xamarin.Forms.MasterDetailPage), typeof(OLS.Casy.App.iOS.Renderers.TabletMasterDetailRenderer), UIUserInterfaceIdiom.Pad)]
namespace OLS.Casy.App.iOS.Renderers
{
    internal class ChildViewController : UIViewController
    {
        public override void ViewDidLayoutSubviews()
        {
            foreach (var vc in ChildViewControllers)
                vc.View.Frame = View.Bounds;
        }
    }

    internal class EventedViewController : ChildViewController
    {
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            var eh = WillAppear;
            if (eh != null)
                eh(this, EventArgs.Empty);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            var eh = WillDisappear;
            if (eh != null)
                eh(this, EventArgs.Empty);
        }

        public event EventHandler WillAppear;

        public event EventHandler WillDisappear;
    }

    class InnerDelegate : UISplitViewControllerDelegate
    {
        readonly MasterBehavior _masterPresentedDefaultState;

        public UIBarButtonItem PresentButton { get; set; }

        public InnerDelegate(MasterBehavior masterPresentedDefaultState)
        {
            _masterPresentedDefaultState = masterPresentedDefaultState;
        }

        public override bool ShouldHideViewController(UISplitViewController svc, UIViewController viewController, UIInterfaceOrientation inOrientation)
        {
            bool willHideViewController;
            switch (_masterPresentedDefaultState)
            {
                case MasterBehavior.Split:
                    willHideViewController = false;
                    break;
                case MasterBehavior.Popover:
                    willHideViewController = true;
                    break;
                case MasterBehavior.SplitOnPortrait:
                    willHideViewController = !(inOrientation == UIInterfaceOrientation.Portrait || inOrientation == UIInterfaceOrientation.PortraitUpsideDown);
                    break;
                default:
                    willHideViewController = inOrientation == UIInterfaceOrientation.Portrait || inOrientation == UIInterfaceOrientation.PortraitUpsideDown;
                    break;
            }
            return willHideViewController;
        }

        public override void WillHideViewController(UISplitViewController svc, UIViewController aViewController, UIBarButtonItem barButtonItem, UIPopoverController pc)
        {
            PresentButton = barButtonItem;
        }
    }

    public class TabletMasterDetailRenderer : UISplitViewController, IVisualElementRenderer, IEffectControlProvider
    {
        UIViewController _detailController;

        bool _disposed;

        EventTracker _events;

        InnerDelegate _innerDelegate;

        nfloat _masterWidth = 0;

        EventedViewController _masterController;

        MasterDetailPage _masterDetailPage;

        bool _masterVisible;

        VisualElementTracker _tracker;

        Page PageController => Element as Page;

        Element ElementController => Element as Element;

        protected MasterDetailPage MasterDetailPage => _masterDetailPage ?? (_masterDetailPage = (MasterDetailPage)Element);

        UIBarButtonItem PresentButton
        {
            get
            {
                if (_innerDelegate != null) return _innerDelegate.PresentButton;

                return null;
            }
        }

        public VisualElement Element { get; private set; }

        public UIView NativeView => View;

        public UIViewController ViewController => this;

        public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                if (Element != null)
                {
                    PageController.SendDisappearing();
                    Element.PropertyChanged -= HandlePropertyChanged;

                    if (MasterDetailPage?.Master != null)
                    {
                        MasterDetailPage.Master.PropertyChanged -= HandleMasterPropertyChanged;
                    }

                    Element = null;
                }

                if (_tracker != null)
                {
                    _tracker.Dispose();
                    _tracker = null;
                }

                if (_events != null)
                {
                    _events.Dispose();
                    _events = null;
                }

                if (_masterController != null)
                {
                    _masterController.WillAppear -= MasterControllerWillAppear;
                    _masterController.WillDisappear -= MasterControllerWillDisappear;
                }

                ClearControllers();
            }

            base.Dispose(disposing);
        }

        public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return NativeView.GetSizeRequest(widthConstraint, heightConstraint);
        }

        public void SetElement(VisualElement element)
        {
            var oldElement = Element;
            Element = element;

            ViewControllers = new[] {
                _masterController = new EventedViewController(),
                _detailController = new ChildViewController()
            };

            if (!Forms_iOS.IsiOS9OrNewer)
                Delegate = _innerDelegate = new InnerDelegate(MasterDetailPage.MasterBehavior);

            UpdateControllers();

            _masterController.WillAppear += MasterControllerWillAppear;
            _masterController.WillDisappear += MasterControllerWillDisappear;

            PresentsWithGesture = MasterDetailPage.IsGestureEnabled;
            OnElementChanged(new VisualElementChangedEventArgs(oldElement, element));

            EffectUtilities.RegisterEffectControlProvider(this, oldElement, element);
            var sendViewInitializedMethod = typeof(Forms).GetMethod("SendViewInitialized", BindingFlags.Static | BindingFlags.NonPublic);
            sendViewInitializedMethod?.Invoke(null, new object[] { element, NativeView });
        }

        public void SetElementSize(Size size)
        {
            Element.Layout(new Rectangle(Element.X, Element.Width, size.Width, size.Height));
        }

        public override void ViewDidAppear(bool animated)
        {
            PageController.SendAppearing();
            base.ViewDidAppear(animated);
            ToggleMaster();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            PageController?.SendDisappearing();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            bool layoutMaster = false;
            bool layoutDetails = false;

            if (Forms_iOS.IsiOS13OrNewer)
            {
                layoutMaster = _masterController?.View?.Superview != null;
                layoutDetails = _detailController?.View?.Superview != null;
            }
            else if (View.Subviews.Length < 2)
            {
                return;
            }
            else
            {
                layoutMaster = true;
                layoutDetails = true;
            }

            if (layoutMaster)
            {
                var masterBounds = _masterController.View.Frame;

                if (Forms_iOS.IsiOS13OrNewer)
                    _masterWidth = masterBounds.Width;
                else
                    _masterWidth = (nfloat)Math.Max(_masterWidth, masterBounds.Width);

                if (!masterBounds.IsEmpty)
                    MasterDetailPage.MasterBounds = new Rectangle(0, 0, _masterWidth, masterBounds.Height);
            }

            if (layoutDetails)
            {
                var detailsBounds = _detailController.View.Frame;
                if (!detailsBounds.IsEmpty)
                    MasterDetailPage.DetailBounds = new Rectangle(0, 0, detailsBounds.Width, detailsBounds.Height);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            UpdateBackground();
            UpdateFlowDirection();
            UpdateMasterBehavior(View.Bounds.Size);
            _tracker = new VisualElementTracker(this);
            _events = new EventTracker(this);
            _events.LoadEvents(NativeView);
        }

        void UpdateMasterBehavior(CGSize newBounds)
        {
            MasterDetailPage masterDetailPage = _masterDetailPage ?? Element as MasterDetailPage;

            if (masterDetailPage == null)
                return;

            bool isPortrait = newBounds.Height > newBounds.Width;
            switch (masterDetailPage.MasterBehavior)
            {
                case MasterBehavior.Split:
                    PreferredDisplayMode = UISplitViewControllerDisplayMode.AllVisible;
                    break;
                case MasterBehavior.Popover:
                    PreferredDisplayMode = UISplitViewControllerDisplayMode.PrimaryHidden;
                    break;
                case MasterBehavior.SplitOnPortrait:
                    PreferredDisplayMode = (isPortrait) ? UISplitViewControllerDisplayMode.AllVisible : UISplitViewControllerDisplayMode.PrimaryHidden;
                    break;
                case MasterBehavior.SplitOnLandscape:
                    PreferredDisplayMode = (!isPortrait) ? UISplitViewControllerDisplayMode.AllVisible : UISplitViewControllerDisplayMode.PrimaryHidden;
                    break;
                default:
                    PreferredDisplayMode = UISplitViewControllerDisplayMode.Automatic;
                    break;
            }

            if (!MasterDetailPage.ShouldShowSplitMode)
                MasterDetailPage.CanChangeIsPresented = true;

            MasterDetailPage.UpdateMasterBehavior();
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (_masterVisible)
                PerformButtonSelector();

            base.ViewWillDisappear(animated);
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            _masterController.View.BackgroundColor = UIColor.White;
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            // I tested this code on iOS9+ and it's never called
            if (!Forms_iOS.IsiOS9OrNewer)
            {
                if (!MasterDetailPage.ShouldShowSplitMode && _masterVisible)
                {
                    MasterDetailPage.CanChangeIsPresented = true;
                    PreferredDisplayMode = UISplitViewControllerDisplayMode.PrimaryHidden;
                    PreferredDisplayMode = UISplitViewControllerDisplayMode.Automatic;
                }

                MasterDetailPage.UpdateMasterBehavior();
                MessagingCenter.Send<IVisualElementRenderer>(this, "Xamarin.UpdateToolbarButtons");
            }

            base.WillRotate(toInterfaceOrientation, duration);
        }

        public override UIViewController ChildViewControllerForStatusBarHidden()
        {
            if (((MasterDetailPage)Element).Detail != null)
                return (UIViewController)Platform.GetRenderer(((MasterDetailPage)Element).Detail);

            return base.ChildViewControllerForStatusBarHidden();
        }

        public override UIViewController ChildViewControllerForHomeIndicatorAutoHidden
        {
            get
            {
                if (((MasterDetailPage)Element).Detail != null)
                    return (UIViewController)Platform.GetRenderer(((MasterDetailPage)Element).Detail);

                return base.ChildViewControllerForHomeIndicatorAutoHidden;
            }
        }

        protected virtual void OnElementChanged(VisualElementChangedEventArgs e)
        {
            if (e.OldElement != null)
                e.OldElement.PropertyChanged -= HandlePropertyChanged;

            if (e.NewElement != null)
                e.NewElement.PropertyChanged += HandlePropertyChanged;

            var changed = ElementChanged;
            if (changed != null)
                changed(this, e);

            _masterWidth = 0;
        }

        void ClearControllers()
        {
            foreach (var controller in _masterController.ChildViewControllers)
            {
                controller.View.RemoveFromSuperview();
                controller.RemoveFromParentViewController();
            }

            foreach (var controller in _detailController.ChildViewControllers)
            {
                controller.View.RemoveFromSuperview();
                controller.RemoveFromParentViewController();
            }
        }

        void HandleMasterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Page.IconProperty.PropertyName || e.PropertyName == Page.TitleProperty.PropertyName)
                MessagingCenter.Send<IVisualElementRenderer>(this, "Xamarin.UpdateToolbarButtons");
        }

        void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_tracker == null)
                return;

            if (e.PropertyName == "Master" || e.PropertyName == "Detail")
                UpdateControllers();
            else if (e.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
                ToggleMaster();
            else if (e.PropertyName == MasterDetailPage.IsGestureEnabledProperty.PropertyName)
                base.PresentsWithGesture = MasterDetailPage.IsGestureEnabled;
            else if (e.PropertyName == VisualElement.FlowDirectionProperty.PropertyName)
                UpdateFlowDirection();
            else if (e.PropertyName == MasterDetailPage.MasterBehaviorProperty.PropertyName)
                UpdateMasterBehavior(View.Bounds.Size);

            MessagingCenter.Send<IVisualElementRenderer>(this, "Xamarin.UpdateToolbarButtons");
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);
            UpdateMasterBehavior(toSize);
        }

        void MasterControllerWillAppear(object sender, EventArgs e)
        {
            _masterVisible = true;
            if (MasterDetailPage.CanChangeIsPresented)
                ElementController.SetValueFromRenderer(MasterDetailPage.IsPresentedProperty, true);
        }

        void MasterControllerWillDisappear(object sender, EventArgs e)
        {
            _masterVisible = false;
            if (MasterDetailPage.CanChangeIsPresented)
                ElementController.SetValueFromRenderer(MasterDetailPage.IsPresentedProperty, false);
        }

        void PerformButtonSelector()
        {
            DisplayModeButtonItem.Target.PerformSelector(DisplayModeButtonItem.Action, DisplayModeButtonItem, 0.0);
        }

        void ToggleMaster()
        {
            if (_masterVisible == MasterDetailPage.IsPresented || MasterDetailPage.ShouldShowSplitMode)
                return;

            PerformButtonSelector();
        }

        void UpdateBackground()
        {
            _ = this.ApplyNativeImageAsync(Page.BackgroundImageProperty, bgImage => {
                if (bgImage != null)
                    View.BackgroundColor = UIColor.FromPatternImage(bgImage);
                else if (Element.BackgroundColor == Color.Default)
                    View.BackgroundColor = UIColor.White;
                else
                    View.BackgroundColor = Element.BackgroundColor.ToUIColor();
            });
        }

        void UpdateControllers()
        {
            MasterDetailPage.Master.PropertyChanged -= HandleMasterPropertyChanged;

            if (Platform.GetRenderer(MasterDetailPage.Master) == null)
                Platform.SetRenderer(MasterDetailPage.Master, Platform.CreateRenderer(MasterDetailPage.Master));
            if (Platform.GetRenderer(MasterDetailPage.Detail) == null)
                Platform.SetRenderer(MasterDetailPage.Detail, Platform.CreateRenderer(MasterDetailPage.Detail));

            ClearControllers();

            MasterDetailPage.Master.PropertyChanged += HandleMasterPropertyChanged;

            var master = Platform.GetRenderer(MasterDetailPage.Master).ViewController;
            var detail = Platform.GetRenderer(MasterDetailPage.Detail).ViewController;

            _masterController.View.AddSubview(master.View);
            _masterController.AddChildViewController(master);

            _detailController.View.AddSubview(detail.View);
            _detailController.AddChildViewController(detail);
        }

        void UpdateFlowDirection()
        {
            if (UpdateFlowDirection(NativeView, Element) && Forms_iOS.IsiOS13OrNewer && NativeView.Superview != null)
            {
                var view = NativeView.Superview;
                NativeView.RemoveFromSuperview();
                view.AddSubview(NativeView);
            }
        }

        void IEffectControlProvider.RegisterEffect(Effect effect)
        {
            VisualElementRenderer<VisualElement>.RegisterEffect(effect, View);
        }

        bool UpdateFlowDirection(UIView view, IVisualElementController controller)
        {
            if (controller != null && view != null && Forms_iOS.IsiOS9OrNewer)
            {
                if (controller.EffectiveFlowDirection.IsRightToLeft())
                {
                    view.SemanticContentAttribute = UISemanticContentAttribute.ForceRightToLeft;
                }
                else
                {
                    view.SemanticContentAttribute = UISemanticContentAttribute.ForceLeftToRight;
                }

                return true;
            }

            return false;
        }
    }

    internal static class Forms_iOS
    {
        static bool? s_isiOs9OrNewer;
        static bool? s_isiOs13OrNewer;

        internal static bool IsiOS9OrNewer
        {
            get
            {
                if (!s_isiOs9OrNewer.HasValue)
                    s_isiOs9OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(9, 0);
                return s_isiOs9OrNewer.Value;
            }
        }

        internal static bool IsiOS13OrNewer
        {
            get
            {
                if (!s_isiOs13OrNewer.HasValue)
                    s_isiOs13OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
                return s_isiOs13OrNewer.Value;
            }
        }
    }

    public static class ImageElementManager
    {
        public static void Init(IImageVisualElementRenderer renderer)
        {
            renderer.ElementPropertyChanged += OnElementPropertyChanged;
            renderer.ElementChanged += OnElementChanged;
            renderer.ControlChanged += OnControlChanged;
        }

        public static void Dispose(IImageVisualElementRenderer renderer)
        {
            renderer.ElementPropertyChanged -= OnElementPropertyChanged;
            renderer.ElementChanged -= OnElementChanged;
            renderer.ControlChanged -= OnControlChanged;
        }

        static void OnControlChanged(object sender, EventArgs e)
        {
            var renderer = sender as IImageVisualElementRenderer;
            var imageElement = renderer.Element as IImageElement;
            SetAspect(renderer, imageElement);
            SetOpacity(renderer, imageElement);
        }

        static void OnElementChanged(object sender, VisualElementChangedEventArgs e)
        {
            if (e.NewElement != null)
            {
                var renderer = sender as IImageVisualElementRenderer;
                var imageElement = renderer.Element as IImageElement;

                SetAspect(renderer, imageElement);
                SetOpacity(renderer, imageElement);
            }
        }

        static void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var renderer = sender as IImageVisualElementRenderer;
            var imageElement = renderer.Element as IImageElement;

            if (e.PropertyName == Image.IsOpaqueProperty.PropertyName)
                SetOpacity(renderer, renderer.Element as IImageElement);
            else if (e.PropertyName == Image.AspectProperty.PropertyName)
                SetAspect(renderer, renderer.Element as IImageElement);
        }

        public static void SetAspect(IImageVisualElementRenderer renderer, IImageElement imageElement)
        {
            var Element = renderer.Element;
            var Control = renderer.GetImage();


            if (renderer.IsDisposed || Element == null || Control == null)
            {
                return;
            }

            Control.ContentMode = imageElement.Aspect.ToUIViewContentMode();
        }

        public static void SetOpacity(IImageVisualElementRenderer renderer, IImageElement imageElement)
        {
            var Element = renderer.Element;
            var Control = renderer.GetImage();

            if (renderer.IsDisposed || Element == null || Control == null)
            {
                return;
            }

            Control.Opaque = imageElement.IsOpaque;
        }

        public static async Task SetImage(IImageVisualElementRenderer renderer, IImageElement imageElement, Image oldElement = null)
        {
            _ = renderer ?? throw new ArgumentNullException(nameof(renderer), $"{nameof(ImageElementManager)}.{nameof(SetImage)} {nameof(renderer)} cannot be null");
            _ = imageElement ?? throw new ArgumentNullException(nameof(imageElement), $"{nameof(ImageElementManager)}.{nameof(SetImage)} {nameof(imageElement)} cannot be null");

            var Element = renderer.Element;
            var Control = renderer.GetImage();

            if (renderer.IsDisposed || Element == null || Control == null)
            {
                return;
            }

            var imageController = imageElement as IImageController;

            var source = imageElement.Source;

            if (Control.Image?.Images != null && Control.Image.Images.Length > 1)
            {
                renderer.SetImage(null);
            }
            else if (oldElement != null)
            {
                var oldSource = oldElement.Source;
                if (Equals(oldSource, source))
                    return;

                if (oldSource is FileImageSource oldFile && source is FileImageSource newFile && oldFile == newFile)
                    return;

                renderer.SetImage(null);
            }

            imageController?.SetIsLoading(true);
            try
            {
                var uiimage = await source.GetNativeImageAsync();

                if (renderer.IsDisposed)
                    return;

                // only set if we are still on the same image
                if (Control != null && imageElement.Source == source)
                    renderer.SetImage(uiimage);
                else
                    uiimage?.Dispose();
            }
            finally
            {
                // only mark as finished if we are still on the same image
                if (imageElement.Source == source)
                    imageController?.SetIsLoading(false);
            }

            (imageElement as IViewController)?.NativeSizeChanged();
        }

        internal static async Task<NativeImage> GetNativeImageAsync(this ImageSource source, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (source == null)
                return null;

            var handler = Xamarin.Forms.Internals.Registrar.Registered.GetHandlerForObject<IImageSourceHandler>(source);
            if (handler == null)
                return null;

            try
            {
                float scale = (float)UIScreen.MainScreen.Scale;

                return await handler.LoadImageAsync(source, scale: scale, cancelationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Image loading", "Image load cancelled");
            }
            catch (Exception ex)
            {
                Log.Warning("Image loading", $"Image load failed: {ex}");
            }

            return null;
        }

        internal static Task ApplyNativeImageAsync(this IShellContext shellContext, BindableObject bindable, BindableProperty imageSourceProperty, Action<UIImage> onSet, Action<bool> onLoading = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _ = shellContext ?? throw new ArgumentNullException(nameof(shellContext));
            var renderer = shellContext as IVisualElementRenderer ?? throw new InvalidOperationException($"The shell context {shellContext.GetType()} must be a {typeof(IVisualElementRenderer)}.");

            return renderer.ApplyNativeImageAsync(bindable, imageSourceProperty, onSet, onLoading, cancellationToken);
        }

        internal static Task ApplyNativeImageAsync(this IVisualElementRenderer renderer, BindableProperty imageSourceProperty, Action<NativeImage> onSet, Action<bool> onLoading = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return renderer.ApplyNativeImageAsync(null, imageSourceProperty, onSet, onLoading, cancellationToken);
        }

        internal static async Task ApplyNativeImageAsync(this IVisualElementRenderer renderer, BindableObject bindable, BindableProperty imageSourceProperty, Action<NativeImage> onSet, Action<bool> onLoading = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _ = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _ = imageSourceProperty ?? throw new ArgumentNullException(nameof(imageSourceProperty));
            _ = onSet ?? throw new ArgumentNullException(nameof(onSet));

            // TODO: it might be good to make sure the renderer has not been disposed

            // makse sure things are good before we start
            var element = bindable ?? renderer.Element;

            var nativeRenderer = renderer as IVisualNativeElementRenderer;

            if (element == null || renderer.NativeView == null || (nativeRenderer != null && nativeRenderer.Control == null))
                return;

            onLoading?.Invoke(true);
            if (element.GetValue(imageSourceProperty) is ImageSource initialSource && initialSource != null)
            {
                try
                {
                    using (var drawable = await initialSource.GetNativeImageAsync(cancellationToken))
                    {
                        // TODO: it might be good to make sure the renderer has not been disposed

                        // we are back, so update the working element
                        element = bindable ?? renderer.Element;

                        // makse sure things are good now that we are back
                        if (element == null || renderer.NativeView == null || (nativeRenderer != null && nativeRenderer.Control == null))
                            return;

                        // only set if we are still on the same image
                        if (element.GetValue(imageSourceProperty) == initialSource)
                            onSet(drawable);
                    }
                }
                finally
                {
                    if (element != null && onLoading != null)
                    {
                        // only mark as finished if we are still on the same image
                        if (element.GetValue(imageSourceProperty) == initialSource)
                            onLoading.Invoke(false);
                    }
                }
            }
            else
            {
                onSet(null);
                onLoading?.Invoke(false);
            }
        }

        internal static async Task ApplyNativeImageAsync(this BindableObject bindable, BindableProperty imageSourceProperty, Action<NativeImage> onSet, Action<bool> onLoading = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _ = bindable ?? throw new ArgumentNullException(nameof(bindable));
            _ = imageSourceProperty ?? throw new ArgumentNullException(nameof(imageSourceProperty));
            _ = onSet ?? throw new ArgumentNullException(nameof(onSet));

            onLoading?.Invoke(true);
            if (bindable.GetValue(imageSourceProperty) is ImageSource initialSource)
            {
                try
                {
                    using (var nsimage = await initialSource.GetNativeImageAsync(cancellationToken))
                    {
                        // only set if we are still on the same image
                        if (bindable.GetValue(imageSourceProperty) == initialSource)
                            onSet(nsimage);
                    }
                }
                finally
                {
                    if (onLoading != null)
                    {
                        // only mark as finished if we are still on the same image
                        if (bindable.GetValue(imageSourceProperty) == initialSource)
                            onLoading.Invoke(false);
                    }
                }
            }
            else
            {
                onSet(null);
                onLoading?.Invoke(false);
            }
        }
    }
}