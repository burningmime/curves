using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace burningmime.util.wpf
{
    public static partial class UiUtils
    {
        #region Shared instances (use when possible to reduce GC pressure)
        public static readonly TextDecorationCollection textDecorationsNone = new TextDecorationCollection();
        public static readonly TextDecorationCollection textDecorationsUnderline = new TextDecorationCollection { TextDecorations.Underline };
        public static readonly BrushConverter brushConverter = new BrushConverter();
        public static readonly ColorConverter colorConverter = new ColorConverter();
        public static readonly IEasingFunction quadraticEase = new QuadraticEase().frozen();
        public static readonly IEasingFunction cubicEase = new CubicEase().frozen();
        public static readonly IEasingFunction quarticEase = new QuarticEase().frozen();
        public static readonly IEasingFunction quinticEase = new QuinticEase().frozen();
        public static readonly IEasingFunction sineEase = new SineEase().frozen();
        public static readonly IEasingFunction circleEase = new CircleEase().frozen();
        #endregion

        #region Brush/pen/drawing/geometry helpers
        // these are hopefully self-explanatory
        // IMPORTANT: all of these freeze the results for performance so don't use them if modifying the result
        public static Brush getBrush(string str) { return ((Brush) brushConverter.ConvertFromString(str)).frozen(); }
        public static Brush getBrush(Color c) { return new SolidColorBrush(c).frozen(); }
        public static Brush getGrayBrush(byte lit, byte alpha = Byte.MaxValue) { return getBrush(Color.FromArgb(alpha, lit, lit, lit)); }
        public static Pen getPen(Brush brush, double thickness) { return new Pen(brush, thickness).frozen(); }
        public static Pen getPen(Color color, double thickness) { return getPen(getBrush(color), thickness); }
        public static GeometryDrawing getDrawing(Geometry geometry, Brush brush, Pen pen) { return new GeometryDrawing { Geometry = geometry, Pen = pen, Brush = brush }.frozen(); }
        public static DrawingBrush getDrawingBrush(Geometry geometry, Brush brush, Pen pen, Rect viewport, TileMode tileMode = TileMode.Tile, BrushMappingMode viewportUnits = BrushMappingMode.Absolute) { return getDrawingBrush(getDrawing(geometry, brush, pen), viewport, tileMode, viewportUnits); }
        public static DrawingBrush getDrawingBrush(Drawing drawing, Rect viewport, TileMode tileMode = TileMode.Tile, BrushMappingMode viewportUnits = BrushMappingMode.Absolute) { return new DrawingBrush { Drawing = drawing, Viewport = viewport, ViewportUnits = viewportUnits, TileMode = tileMode }.frozen(); }
        public static DrawingImage getImage(Geometry geometry, Brush brush, Pen pen) { return new DrawingImage(getDrawing(geometry, brush, pen)).frozen(); }
        public static ImageBrush getImageBrush(Geometry geometry, Brush brush, Pen pen, Rect viewport, TileMode tileMode = TileMode.Tile, BrushMappingMode viewportUnits = BrushMappingMode.Absolute) { return getImageBrush(getImage(geometry, brush, pen), viewport, tileMode, viewportUnits); }
        public static ImageBrush getImageBrush(ImageSource source, Rect viewport, TileMode tileMode = TileMode.Tile, BrushMappingMode viewportUnits = BrushMappingMode.Absolute) { return new ImageBrush { ImageSource = source, Viewport = viewport, ViewportUnits = viewportUnits, TileMode = tileMode }.frozen(); }
        #endregion

        #region Visual Tree Helpers
        /// <summary>
        /// Gets the window that owns the element (SLOW, so cache result).
        /// </summary>
        public static Window getOwningWindow(this Visual elem) { return elem.getAncestorOfType<Window>(); }
        
        /// <summary>
        /// Enumerates all ancestors of an element (SLOW!).
        /// </summary>
        public static IEnumerable<Visual> getAncestry(this Visual obj)
        {
            if(obj == null) throw new ArgumentNullException("obj");
            while(true)
            {
                FrameworkElement elem = obj as FrameworkElement;
                obj = (elem != null ? elem.Parent ?? elem.TemplatedParent : VisualTreeHelper.GetParent(obj)) as Visual;
                if(obj == null) yield break;
                yield return obj;
            }
        }

        /// <summary>
        /// Gets first ancestor of a given type (SLOW, so cache result).
        /// </summary>
        public static T getAncestorOfType<T>(this Visual obj) where T : DependencyObject { return obj.getAncestry().OfType<T>().FirstOrDefault(); }

        /// <summary>
        /// Enumerates all descendants of an element (SLOW!).
        /// </summary>
        public static IEnumerable<Visual> getDescendants(this Visual obj)
        {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for(int i = 0; i < count; i++)
            {
                Visual child = VisualTreeHelper.GetChild(obj, i) as Visual;
                if(child != null)
                {
                    yield return child;
                    foreach(Visual grandchild in child.getDescendants())
                        yield return grandchild;
                }
            }
        }

        /// <summary>
        /// Handling for DisableButtonFocus attached DP. Basically, if UiUtils.DisableButtonFocus is set on an element then all buttons in that
        /// element will be set to non-focusable. This might be bad for accessibility (ie users using keyboard to navigate), so use sparingly when
        /// there are focus issues.
        /// </summary>
        private static void onDisableButtonFocusSet(DependencyObject target, DependencyPropertyChangedEventArgs<bool> args)
        {
            if(!args.NewValue) throw new InvalidOperationException("Cannot re-enable focus with this DP, must do it manually");
            if(args.OldValue) return;
            FrameworkElement elem = target as FrameworkElement;
            if(elem == null) throw new InvalidOperationException("Can only set DisableButtonFocus on an object of type FrameworkElement");
            elem.whenLoaded(delegate
            {
                if(elem is ButtonBase) elem.Focusable = false;
                foreach(ButtonBase e in elem.getDescendants().OfType<ButtonBase>())
                    e.Focusable = false;
            });
        }
        #endregion
        
        #region Commands
        /// <summary>
        /// Creates a new RoutedUICommand.
        /// </summary>
        public static RoutedUICommand getCommand(string text, Type ownerType, params InputGesture[] gestures) { return new RoutedUICommand(text, text, ownerType, new InputGestureCollection(gestures)); }

        /// <summary>
        /// Helper to add a new menu item with the command to the menu. You can ignore the return value unless you want to do something special with the MenuItem.
        /// </summary>
        public static MenuItem addCommand(this MenuBase menu, RoutedCommand command) { MenuItem item = new MenuItem { Command = command }; menu.Items.Add(item); return item; }

        /// <summary>
        /// Creates a CommandBinding for the command using the supplied functions and binds it to the UIElement. You can ignore the return values unless you're doing
        /// something special with the binding. For the simple case where you don't care about the command arguments.
        /// </summary>
        public static CommandBinding bindCommand(this UIElement elem, RoutedCommand command, Action executed)
        {
            CommandBinding binding = new CommandBinding(command, createExecuteHandler(executed));
            elem.CommandBindings.Add(binding);
            return binding;
        }
        
        /// <summary>
        /// Creates a CommandBinding for the command using the supplied functions and binds it to the UIElement. You can ignore the return values unless you're doing
        /// something special with the binding. For the simple case where you don't care about the command arguments.
        /// </summary>
        public static CommandBinding bindCommand(this UIElement elem, RoutedCommand command, Func<bool> canExecute, Action executed)
        {
            CommandBinding binding = new CommandBinding(command, createExecuteHandler(executed), createCanExecuteHandler(canExecute));
            elem.CommandBindings.Add(binding);
            return binding;
        }
        
        /// <summary>
        /// Creates a CommandBinding for the command using the supplied functions and binds it to the UIElement. You can ignore the return values unless you're doing
        /// something special with the binding.
        /// </summary>
        public static CommandBinding bindCommand(this UIElement elem, RoutedCommand command, ExecutedRoutedEventHandler executed)
        {
            CommandBinding binding = new CommandBinding(command, executed);
            elem.CommandBindings.Add(binding);
            return binding;
        }

        /// <summary>
        /// Creates a CommandBinding for the command using the supplied functions and binds it to the UIElement. You can ignore the return values unless you're doing
        /// something special with the binding.
        /// </summary>
        public static CommandBinding bindCommand(this UIElement elem, RoutedCommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute)
        {
            CommandBinding binding = new CommandBinding(command, executed, canExecute);
            elem.CommandBindings.Add(binding);
            return binding;
        }

        /// <summary>
        /// Creates a CanExecuteRoutedEventHandler for when you don't care about the arguments.
        /// </summary>
        public static CanExecuteRoutedEventHandler createCanExecuteHandler(Func<bool> func)
        {
            return (_, args) => args.CanExecute = func();
        }

        /// <summary>
        /// Creates an ExecutedRoutedEventHandler for when you don't care about the arguments.
        /// </summary>
        public static ExecutedRoutedEventHandler createExecuteHandler(Action action)
        {
            return (_, args) => action();
        }
        #endregion

        #region DPI
        /// <summary>
        /// Default X DPI (usually 96).
        /// </summary>
        public static readonly int defaultDpiX = (int) ReflectionUtils.slowGetStaticProperty(typeof(SystemParameters), "DpiX");

        /// <summary>
        /// Depault Y DPI (usually 96).
        /// </summary>
        public static readonly int defaultDpiY = (int) ReflectionUtils.slowGetStaticProperty(typeof(SystemParameters), "Dpi");

        /// <summary>
        /// Gets the X/Y DPI for a visual. Use this instead of <see cref="defaultDpiX"/> and <see cref="defaultDpiY"/> when possible, since
        /// the user could have multiple monitors.
        /// </summary>
        public static void getDpi(Visual visual, out int dpiX, out int dpiY)
        {
            if(isInDesignMode || visual == null)
                goto Ldefault;
            PresentationSource source = PresentationSource.FromVisual(visual);
            if(source == null || source.CompositionTarget == null)
                goto Ldefault;
            Matrix m = source.CompositionTarget.TransformToDevice;
            dpiX = (int) (96 * m.M11);
            dpiY = (int) (96 * m.M22);
            return;
        Ldefault:
            dpiX = defaultDpiX;
            dpiY = defaultDpiY;
        }
        #endregion

        #region Loaded event helpers
        /// <summary>
        /// If the element is already loaded, runs the function. Otherwise attaches the handler to the Loaded event in a way where it will
        /// detatch itself after running (so it doesn't keep a reference). Useful for reducing memory leakage or if you don't know if an element
        /// is loaded or not, or if you want to avoid the Loaded event being called multiple times if the object is reloaded. If you have something
        /// you want to do on reloads, see <see cref="whenLoadedAndReloaded"/>.
        /// </summary>
        public static void whenLoaded(this FrameworkElement elem, RoutedEventHandler onFirstLoad)
        {
            if(elem == null) throw new ArgumentNullException("elem");
            if(onFirstLoad == null) throw new ArgumentNullException("onFirstLoad");
            if(elem.IsLoaded)
                onFirstLoad(elem, new RoutedEventArgs(FrameworkElement.LoadedEvent));
            else
            {
                // Create a loaded event that removes itself after it has executed to prevent memory leakage
                RoutedEventHandler[] handlers = new RoutedEventHandler[1];
                elem.Loaded += handlers[0] = (sender, args) =>
                {
                    FrameworkElement fe = (FrameworkElement) sender;
                    onFirstLoad(sender, args);
                    fe.Loaded -= handlers[0];
                };
            }
        }

        /// <summary>
        /// If the element is already loaded, runs the loaded, then attaches the reloaded event. If the element is not yet loaded,
        /// runs the loaded event once (when first loaded), and the reloaded event on subsequent loads. This is useful because
        /// the Loaded event is often called more than once, and you don't nesescarily want to detatch eveything in Unloaded,
        /// then re-attach everything later.
        /// </summary>
        public static void whenLoadedAndReloaded(this FrameworkElement elem, RoutedEventHandler onFirstLoad, RoutedEventHandler onSubsequentLoads)
        {
            if(elem == null) throw new ArgumentNullException("elem");
            if(onFirstLoad == null) throw new ArgumentNullException("onFirstLoad");
            if(onSubsequentLoads == null) throw new ArgumentNullException("onSubsequentLoads");
            if(elem.IsLoaded)
            {
                onFirstLoad(elem, new RoutedEventArgs(FrameworkElement.LoadedEvent));
                elem.Loaded += onSubsequentLoads;
            }
            else
            {
                RoutedEventHandler[] handlers = new RoutedEventHandler[1];
                elem.Loaded += handlers[0] = (sender, args) =>
                {
                    FrameworkElement fe = (FrameworkElement) sender;
                    onFirstLoad(sender, args);
                    fe.Loaded -= handlers[0];
                    fe.Loaded += onSubsequentLoads;
                };
            }
        }

        #endregion

        #region Size Changed Watcher
        /// <summary>
        /// Fires an event after a delay when the element is resized. This is useful since when the user is resizing, the
        /// resize event is fired *a lot*, so this wait for them to finish the whole resize before doing something. The event
        /// is fired *delay* seconds after the last resize.
        /// 
        /// You should call this after the control has loaded unless you want a callback *delay* seconds afterwards, since
        /// SizeChanged is fired once the control has loaded.
        /// </summary>
        public static void watchForResize(FrameworkElement elem, double delay, Action onResized)
        {
            if(elem == null) throw new ArgumentNullException("elem");
            if(onResized == null) throw new ArgumentNullException("onResized");
            if(delay <= 0)
            {
                // don't bother creating the timer if no delay
                elem.SizeChanged += (_, args) => onResized();
            }
            else
            {
                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background, elem.Dispatcher);
                ResizeWatcher watcher = new ResizeWatcher(timer, onResized);
                timer.Interval = TimeSpan.FromSeconds(delay);
                timer.Tick += watcher.onTimerTick;
                elem.SizeChanged += watcher.onSizeChanged;
            }
        }

        // probably could just use closure, but meh
        private sealed class ResizeWatcher
        {
            private readonly DispatcherTimer _timer;
            private readonly Action _onResized;
            public ResizeWatcher(DispatcherTimer timer, Action onResized) { _timer = timer; _onResized = onResized; }
            public void onSizeChanged(object sender, SizeChangedEventArgs e) { _timer.Stop(); _timer.Start(); }
            public void onTimerTick(object sender, EventArgs e) { _timer.Stop(); _onResized(); }
        }
        #endregion

        #region Misc
        /// <summary>
        /// To infinity... and beyond!
        /// </summary>
        public static readonly Size INFINITE_SIZE = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

        /// <summary>
        /// Freezes the element and returns it (for chaining).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="freezable"></param>
        /// <returns></returns>
        public static T frozen<T>(this T freezable) where T : Freezable { if(!freezable.IsFrozen) freezable.Freeze(); return freezable; }

        /// <summary>
        /// Finds a template part by name.
        /// </summary>
        public static T getTemplatePart<T>(this Control control, string name) { return (T) control.Template.FindName(name, control); }

        /// <summary>
        /// Checks if the modifier key is pressed.
        /// </summary>
        public static bool isKeyDown(ModifierKeys keys) { return (Keyboard.Modifiers & keys) != 0; }

        /// <summary>
        /// Adds a child to a panel.
        /// </summary>
        public static void addChild(this Panel p, UIElement e)
        {
            p.Children.Add(e);
        }

        /// <summary>
        /// Removes a child from a panel.
        /// </summary>
        public static void removeChild(this Panel p, UIElement e)
        {
            p.Children.Remove(e);
        }
        
        /// <summary>
        /// Performs a simple animation of a double property from its current value to the "to" value over the duration. This is the most
        /// simple case of animation. For more complex animations, you'll need to do it the old-fashioned way.
        /// </summary>
        public  static void animate<T>(this T target, DependencyProperty<double> dp, double to, double duration, IEasingFunction ease = null)
            where T : DependencyObject, IAnimatable
        {
            DoubleAnimation anim = new DoubleAnimation(dp.GetValue(target), to, new Duration(TimeSpan.FromSeconds(duration)));
            anim.EasingFunction = ease;
            anim.Completed += (_, args) => { target.BeginAnimation(dp, null); dp.SetValue(target, to); };
            anim.Freeze();
            target.BeginAnimation(dp, anim, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Are we running in the designer?
        /// </summary>
        public static readonly bool isInDesignMode = (bool) DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement)).Metadata.DefaultValue;

        /// <summary>
        /// Gets a resource from the APplication instance. Requires Application.Current is non-null.
        /// </summary>
        public static T getApplicationResource<T>(string name)
        {
            return ((T)Application.Current.FindResource(name));
        }

        /// <summary>
        /// Clones the current value of a resource in the Application instance. Requires Application.Current is non-null.
        /// </summary>
        public static T cloneApplicationResource<T>(string name) where T : Freezable
        {
            return (T) getApplicationResource<T>(name).CloneCurrentValue();
        }

        /// <summary>
        /// Gets thye Disptacher for the current thread if there is one, or returns null if there isn't. This is useful since
        /// <see cref="Dispatcher.CurrentDispatcher"/> always creates a dispatcher if one doesn't exist. Sometimes you don't
        /// want to do that.
        /// </summary>
        public static Dispatcher currentDispatcherOrNull()
        {
            return Dispatcher.FromThread(Thread.CurrentThread);
        }

        /// <summary>
        /// Starts a new dispatcher running in a background thread and returns it.
        /// </summary>
        /// <param name="threadName">Name of the thread to contain the dispatcher.</param>
        /// <returns>The brand new dispatcher, which should be running normally.</returns>
        public static Dispatcher startDispatcher(string threadName = "")
        {
            using(EventWaitHandle wait = new AutoResetEvent(false))
            {
                Dispatcher[] results = new Dispatcher[1];
                Thread thread = new Thread(() =>
                {
                    results[0] = Dispatcher.CurrentDispatcher;
                    // ReSharper disable once AccessToDisposedClosure
                    wait.Set();
                    Dispatcher.Run();
                });
                thread.Name = threadName;
                thread.IsBackground = true;
                thread.Start();
                wait.WaitOne();
                Dispatcher result = results[0];
                Debug.Assert(result != null);
                return result;
            }
        }
        #endregion
    }
}