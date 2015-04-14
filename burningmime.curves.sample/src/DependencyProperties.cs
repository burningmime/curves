using System;
using System.Windows;
using System.Windows.Media;
using burningmime.util.wpf;

namespace burningmime.util.wpf
{
    public partial class AsyncProgressViewer
    {
        public string task { get { return (string) GetValue(taskProperty); } private set { SetValue(taskPropertyKey, value); } }
        private static readonly DependencyPropertyKey taskPropertyKey = DependencyProperty.RegisterReadOnly("task", typeof(string), typeof(AsyncProgressViewer), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty<string> taskProperty = taskPropertyKey.DependencyProperty.of<string>();
        
        public int totalWork { get { return (int) GetValue(totalWorkProperty); } private set { SetValue(totalWorkPropertyKey, value); } }
        private static readonly DependencyPropertyKey totalWorkPropertyKey = DependencyProperty.RegisterReadOnly("totalWork", typeof(int), typeof(AsyncProgressViewer), new PropertyMetadata(-1));
        public static readonly DependencyProperty<int> totalWorkProperty = totalWorkPropertyKey.DependencyProperty.of<int>();
        
        public int workComplete { get { return (int) GetValue(workCompleteProperty); } private set { SetValue(workCompletePropertyKey, value); } }
        private static readonly DependencyPropertyKey workCompletePropertyKey = DependencyProperty.RegisterReadOnly("workComplete", typeof(int), typeof(AsyncProgressViewer), new PropertyMetadata(default(int)));
        public static readonly DependencyProperty<int> workCompleteProperty = workCompletePropertyKey.DependencyProperty.of<int>();
        
        public int workRemaining { get { return (int) GetValue(workRemainingProperty); } private set { SetValue(workRemainingPropertyKey, value); } }
        private static readonly DependencyPropertyKey workRemainingPropertyKey = DependencyProperty.RegisterReadOnly("workRemaining", typeof(int), typeof(AsyncProgressViewer), new PropertyMetadata(default(int)));
        public static readonly DependencyProperty<int> workRemainingProperty = workRemainingPropertyKey.DependencyProperty.of<int>();
        
        public double ratioComplete { get { return (double) GetValue(ratioCompleteProperty); } private set { SetValue(ratioCompletePropertyKey, value); } }
        private static readonly DependencyPropertyKey ratioCompletePropertyKey = DependencyProperty.RegisterReadOnly("ratioComplete", typeof(double), typeof(AsyncProgressViewer), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty<double> ratioCompleteProperty = ratioCompletePropertyKey.DependencyProperty.of<double>();
        
        public bool started { get { return (bool) GetValue(startedProperty); } private set { SetValue(startedPropertyKey, value); } }
        private static readonly DependencyPropertyKey startedPropertyKey = DependencyProperty.RegisterReadOnly("started", typeof(bool), typeof(AsyncProgressViewer), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty<bool> startedProperty = startedPropertyKey.DependencyProperty.of<bool>();
        
        public bool completed { get { return (bool) GetValue(completedProperty); } private set { SetValue(completedPropertyKey, value); } }
        private static readonly DependencyPropertyKey completedPropertyKey = DependencyProperty.RegisterReadOnly("completed", typeof(bool), typeof(AsyncProgressViewer), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty<bool> completedProperty = completedPropertyKey.DependencyProperty.of<bool>();
        
        public bool cancelled { get { return (bool) GetValue(cancelledProperty); } private set { SetValue(cancelledPropertyKey, value); } }
        private static readonly DependencyPropertyKey cancelledPropertyKey = DependencyProperty.RegisterReadOnly("cancelled", typeof(bool), typeof(AsyncProgressViewer), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty<bool> cancelledProperty = cancelledPropertyKey.DependencyProperty.of<bool>();
        
    }
    
    public partial class UiUtils
    {
        public static bool GetDisableButtonFocus(DependencyObject obj) { return (bool) obj.GetValue(DisableButtonFocusProperty); }
        public static void SetDisableButtonFocus(DependencyObject obj, bool value) { obj.SetValue(DisableButtonFocusProperty, value); }
        public static readonly DependencyProperty<bool> DisableButtonFocusProperty = DependencyProperty.RegisterAttached("DisableButtonFocus", typeof(bool), typeof(UiUtils), new PropertyMetadata(default(bool), onDisableButtonFocusSetInternal)).of<bool>();
        private static void onDisableButtonFocusSetInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { onDisableButtonFocusSet(obj, args.of<bool>()); }
        
    }
    
    public partial class MeasuringCanvas
    {
        public double MinX { get { return (double) GetValue(MinXProperty); } private set { SetValue(MinXPropertyKey, value); } }
        private static readonly DependencyPropertyKey MinXPropertyKey = DependencyProperty.RegisterReadOnly("MinX", typeof(double), typeof(MeasuringCanvas), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty<double> MinXProperty = MinXPropertyKey.DependencyProperty.of<double>();
        
        public double MinY { get { return (double) GetValue(MinYProperty); } private set { SetValue(MinYPropertyKey, value); } }
        private static readonly DependencyPropertyKey MinYPropertyKey = DependencyProperty.RegisterReadOnly("MinY", typeof(double), typeof(MeasuringCanvas), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty<double> MinYProperty = MinYPropertyKey.DependencyProperty.of<double>();
        
        public double MaxX { get { return (double) GetValue(MaxXProperty); } private set { SetValue(MaxXPropertyKey, value); } }
        private static readonly DependencyPropertyKey MaxXPropertyKey = DependencyProperty.RegisterReadOnly("MaxX", typeof(double), typeof(MeasuringCanvas), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty<double> MaxXProperty = MaxXPropertyKey.DependencyProperty.of<double>();
        
        public double MaxY { get { return (double) GetValue(MaxYProperty); } private set { SetValue(MaxYPropertyKey, value); } }
        private static readonly DependencyPropertyKey MaxYPropertyKey = DependencyProperty.RegisterReadOnly("MaxY", typeof(double), typeof(MeasuringCanvas), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty<double> MaxYProperty = MaxYPropertyKey.DependencyProperty.of<double>();
        
    }
    
    public partial class ImageArgb
    {
        public ColorArgb BackgroundColor { get { return (ColorArgb) GetValue(BackgroundColorProperty); } set { SetValue(BackgroundColorProperty, value); } }
        public static readonly DependencyProperty<ColorArgb> BackgroundColorProperty = DependencyProperty.Register("BackgroundColor", typeof(ColorArgb), typeof(ImageArgb), new PropertyMetadata(default(ColorArgb))).of<ColorArgb>();
        
    }
    
}

namespace burningmime.curves.sample
{
    public partial class DrawingSurface
    {
        /// <summary>
        /// Mode used to preprocess the points.
        /// </summary>
        public PreprocessModes PreprocessMode { get { return (PreprocessModes) GetValue(PreprocessModeProperty); } set { SetValue(PreprocessModeProperty, value); } }
        public static readonly DependencyProperty<PreprocessModes> PreprocessModeProperty = DependencyProperty.Register("PreprocessMode", typeof(PreprocessModes), typeof(DrawingSurface), new PropertyMetadata(PreprocessModes.RDP, onPreprocessModeChangedInternal)).of<PreprocessModes>();
        private static void onPreprocessModeChangedInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { ((DrawingSurface) obj).onPreprocessModeChanged(); }
        
        /// <summary>
        /// Mode used to render the points.
        /// </summary>
        public RenderModes RenderMode { get { return (RenderModes) GetValue(RenderModeProperty); } set { SetValue(RenderModeProperty, value); } }
        public static readonly DependencyProperty<RenderModes> RenderModeProperty = DependencyProperty.Register("RenderMode", typeof(RenderModes), typeof(DrawingSurface), new PropertyMetadata(RenderModes.WPF_DRAW, onRenderModeChangedInternal)).of<RenderModes>();
        private static void onRenderModeChangedInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { ((DrawingSurface) obj).onRenderModeChanged(); }
        
        /// <summary>
        /// Distance between points for Linearization preprocessing.
        /// </summary>
        public double PointDistance { get { return (double) GetValue(PointDistanceProperty); } set { SetValue(PointDistanceProperty, value); } }
        public static readonly DependencyProperty<double> PointDistanceProperty = DependencyProperty.Register("PointDistance", typeof(double), typeof(DrawingSurface), new PropertyMetadata(DEFAULT_POINT_DIST, onPointDistanceChangedInternal)).of<double>();
        private static void onPointDistanceChangedInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { ((DrawingSurface) obj).onPointDistanceChanged(); }
        
        /// <summary>
        /// Maximum error for RDP reduction algorithm.
        /// </summary>
        public double RdpError { get { return (double) GetValue(RdpErrorProperty); } set { SetValue(RdpErrorProperty, value); } }
        public static readonly DependencyProperty<double> RdpErrorProperty = DependencyProperty.Register("RdpError", typeof(double), typeof(DrawingSurface), new PropertyMetadata(DEFAULT_RDP_ERROR, onPointDistanceChangedInternal)).of<double>();
        
        /// <summary>
        /// Maximum error for fitting algorithm.
        /// </summary>
        public double FittingError { get { return (double) GetValue(FittingErrorProperty); } set { SetValue(FittingErrorProperty, value); } }
        public static readonly DependencyProperty<double> FittingErrorProperty = DependencyProperty.Register("FittingError", typeof(double), typeof(DrawingSurface), new PropertyMetadata(DEFAULT_FIT_ERROR, onErrorChangedInternal)).of<double>();
        private static void onErrorChangedInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { ((DrawingSurface) obj).onErrorChanged(); }
        
        /// <summary>
        /// Time (in milliseconds) of last operation, or NaN if no info.
        /// </summary>
        public double LastFitTime { get { return (double) GetValue(LastFitTimeProperty); } set { SetValue(LastFitTimeProperty, value); } }
        public static readonly DependencyProperty<double> LastFitTimeProperty = DependencyProperty.Register("LastFitTime", typeof(double), typeof(DrawingSurface), new PropertyMetadata(double.NaN)).of<double>();
        
        /// <summary>
        /// # of points before/after preprocessing
        /// </summary>
        public string PointCount { get { return (string) GetValue(PointCountProperty); } set { SetValue(PointCountProperty, value); } }
        public static readonly DependencyProperty<string> PointCountProperty = DependencyProperty.Register("PointCount", typeof(string), typeof(DrawingSurface), new PropertyMetadata(string.Empty)).of<string>();
        
        /// <summary>
        /// Should curves have different colors when displayed?
        /// </summary>
        public bool ShowCurveColors { get { return (bool) GetValue(ShowCurveColorsProperty); } set { SetValue(ShowCurveColorsProperty, value); } }
        public static readonly DependencyProperty<bool> ShowCurveColorsProperty = DependencyProperty.Register("ShowCurveColors", typeof(bool), typeof(DrawingSurface), new PropertyMetadata(true, onColorizeChangedInternal)).of<bool>();
        private static void onColorizeChangedInternal(DependencyObject obj, DependencyPropertyChangedEventArgs args) { ((DrawingSurface) obj).onColorizeChanged(); }
        
    }
    
}

 
