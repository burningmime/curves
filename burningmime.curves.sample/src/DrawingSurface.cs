using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using burningmime.util.wpf;
using burningmime.util;

#if SYSTEM_WINDOWS_VECTOR
using VECTOR = System.Windows.Vector;
using FLOAT = System.Double;
#elif SYSTEM_NUMERICS_VECTOR
using VECTOR = System.Numerics.Vector2;
using FLOAT = System.Single;
#elif UNITY
using VECTOR = UnityEngine.Vector2;
using FLOAT = System.Single;
#else
#error Unknown vector type -- must define one of SYSTEM_WINDOWS_VECTOR, SYSTEM_NUMERICS_VECTOR or UNITY
#endif

namespace burningmime.curves.sample
{
    /// <summary>
    /// The surface that handles capturing user input, drawing stuff, and fitting the curves themselves. Basically
    /// the heart of the WPF app.
    /// </summary>
    public sealed partial class DrawingSurface : UserControl
    {
        private static readonly Log _log = LogManager.getLog(typeof(DrawingSurface));

        // parameter min/max/defaults
        public  const double SLIDER_MIN               = 1; 
        public  const double SLIDER_MAX               = 25;
        public  const int    DEFAULT_FIT_ERROR        = 8;
        public  const int    DEFAULT_POINT_DIST       = 8;
        public  const int    DEFAULT_RDP_ERROR        = 2;

        // other constants
        private const int    SAMPLES_PER_CURVE        = 64;   // samples used in the spline
        private const FLOAT  SPLINE_POINT_DISTANCE    = 8;    // distance between points in the spline render mode
        private const double PEN_THICKNESS            = 3;    // thickness of pen when drawing WPF curves
        private const double RESIZE_DELAY             = .2;   // delay in seconds after last user resize to resize the image
        private const double DEFAULT_POINT_RADIUS     = 1.5;  // radius of drawn points
        private const double CONTROL_POINT_RADIUS     = 2.5;  // radius of control points

        private readonly DrawingGroup _drawing;        // the thing we actually draw to
        private readonly RectangleGeometry _clipRect;  // Rectangle used to clip the drawing
        private readonly Spline _spline;               // Spline used for spline draw
        private readonly List<VECTOR> _points;         // points the user has drawn so far
        private readonly Stopwatch _stopwatch;         // stopwatch used for performance timing

        // colors
        private static readonly Brush[] _partBrushes = { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Magenta, Brushes.Cyan, Brushes.Black, Brushes.White };
        private static readonly Pen[] _partPens = _partBrushes.Select(b => UiUtils.getPen(b, PEN_THICKNESS)).ToArray();
        private static readonly Brush _backgroundBrush = UiUtils.getBrush("#FFCCCCCC");
        private static readonly Brush _pointBrush = Brushes.Red;

        public DrawingSurface()
        {
            Image image = new Image();
            Content = image;
            _drawing = new DrawingGroup();
            image.Source = new DrawingImage(_drawing);
            _points = new List<VECTOR>(1024);
            _stopwatch = new Stopwatch();
            _clipRect = new RectangleGeometry();
            _spline = new Spline(SAMPLES_PER_CURVE);
            this.whenLoadedAndReloaded(onLoaded, onReloaded);
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize with test data
            clearDrawing();
            _points.AddRange(TestData.data);
            doRender();

            // when the user resizes, redo the rendering
            // we need a delay here because if they're dragging the window, we don't want to be 
            // constantly resizing -- wait until they're done with the drag, then render
            UiUtils.watchForResize(this, RESIZE_DELAY, doRender);

            // attach mouse events
            MouseDown += onMouseDown;
            MouseMove += onMouseMove;
            MouseUp += onMouseUp;
            LostMouseCapture += onMouseLostCapture;
        }

        private void onReloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            doRender();
        }

        private void onMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Mouse.Captured == null)
            {
                Mouse.Capture(this);
                clearDrawing();
                _points.Clear();
                hidePerformanceCounter();
                addPoint(e.GetPosition(this)); // first point
            }
        }

        private void onMouseMove(object sender, MouseEventArgs e)
        {
            if(Mouse.Captured == this)
                addPoint(e.GetPosition(this));
        }

        private void onMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(Mouse.Captured == this)
                Mouse.Capture(null); // will eventually call onMouseLostCapture, which does the render
        }

        private void onMouseLostCapture(object sender, MouseEventArgs e)
        {
            // we want to do this here instead of in onMouseUp in case mouse capture is lost for some other reason
            doRender();
        }

        // When one of these properties changes, redo the fit/render if the property applies to the current render mode
        private void onRenderModeChanged() { doRender(); }
        private void onErrorChanged() { if(RenderMode >= RenderModes.CONTROL_POINTS) doRender(); }
        private void onPointDistanceChanged() { if(RenderMode >= RenderModes.PREPROCESS) doRender(); }
        private void onPreprocessModeChanged() { if(RenderMode >= RenderModes.PREPROCESS) doRender(); }

        private void addPoint(Point p)
        {
            // Check if we're on screen
            int width = (int) ActualWidth;
            int height = (int) ActualHeight;
            if(p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height)
                return;
            VECTOR v = toVector(p);
            if(_points.Count > 0 && _points[_points.Count - 1] == v)
                return;
            _points.Add(v);
            drawPoint(v);
        }

        /// <summary>
        /// This is called when the user releases the mouse or changes any of the parameters. It does the actual curve fitting (if needed)
        /// then draws the result in whatever mode the user currently has selected.
        /// </summary>
        private void doRender()
        {
            if(UiUtils.isInDesignMode) return; // don't do this if we're running in the designer
            if(!IsLoaded) return;              // this might happen with some of the property bindings
            if(Mouse.Captured == this) return; // if the user is adding points, wait until they're done
            switch(RenderMode)
            {
                case RenderModes.POINTS:
                    hidePerformanceCounter();
                    clearAndDrawPointGroup(_points);
                    break;
                case RenderModes.PREPROCESS:
                    clearAndDrawPointGroup(preprocessOnly());
                    break;
                case RenderModes.CONTROL_POINTS:
                    clearAndDrawControlPoints(fitCurves());
                    break;
                case RenderModes.SPLINE_DRAW:
                    clearAndDrawSpline(fitCurves());
                    break;
                case RenderModes.WPF_DRAW:
                    clearAndDrawCurves(fitCurves(), true);
                    break;
                default:
                    _log.error("Invalid PointRenderMode");
                    clearDrawing();
                    break;
            }
        }

        private static List<VECTOR> preprocess(List<VECTOR> pts, PreprocessModes ppMode, FLOAT linDist, FLOAT rdpError)
        {
            switch(ppMode)
            {
                case PreprocessModes.NONE:
                    return pts;
                case PreprocessModes.LINEAR:
                    return CurvePreprocess.Linearize(pts, linDist);
                case PreprocessModes.RDP:
                    return CurvePreprocess.RdpReduce(pts, rdpError);
                default:
                    _log.error("Invalid PreprocessMode");
                    return CurvePreprocess.RemoveDuplicates(pts);
            }
        }

        private List<VECTOR> preprocessOnly()
        {
            // Access the dependency properties outside the stopwatch area
            FLOAT linDist = ((FLOAT) PointDistance).clamp((FLOAT) SLIDER_MIN, (FLOAT) SLIDER_MAX);
            FLOAT rdpError = ((FLOAT) RdpError).clamp((FLOAT) SLIDER_MIN, (FLOAT) SLIDER_MAX);
            PreprocessModes ppMode = PreprocessMode;
            List<VECTOR> inPts = _points;

            _stopwatch.Reset();
            _stopwatch.Start();
            List<VECTOR> ppPts = preprocess(inPts, ppMode, linDist, rdpError);
            _stopwatch.Stop();
            LastFitTime = ppMode == PreprocessModes.NONE ? double.NaN : _stopwatch.Elapsed.TotalMilliseconds;
            PointCount = inPts.Count + "/" + ppPts.Count;
            return ppPts;
        }

        private CubicBezier[] fitCurves()
        {
            // Access the dependency properties outside the stopwatch area
            FLOAT fitError = ((FLOAT) FittingError).clamp((FLOAT) SLIDER_MIN, (FLOAT) SLIDER_MAX);
            FLOAT linDist = ((FLOAT) PointDistance).clamp((FLOAT) SLIDER_MIN, (FLOAT) SLIDER_MAX);
            FLOAT rdpError = ((FLOAT) RdpError).clamp((FLOAT) SLIDER_MIN, (FLOAT) SLIDER_MAX);
            PreprocessModes ppMode = PreprocessMode;
            List<VECTOR> inPts = _points;

            _stopwatch.Reset();
            _stopwatch.Start();
            List<VECTOR> ppPts = preprocess(inPts, ppMode, linDist, rdpError);
            CubicBezier[] curves = CurveFit.Fit(ppPts, fitError);
            _stopwatch.Stop();
            LastFitTime = _stopwatch.Elapsed.TotalMilliseconds;
            PointCount = inPts.Count + "/" + ppPts.Count;
            return curves;
        }

        private void hidePerformanceCounter()
        {
            LastFitTime = double.NaN;
            PointCount = string.Empty;
        }
        
        #region Drawing

        private void clearAndDrawPointGroup(IEnumerable<VECTOR> pts)
        {
            using(DrawingContext ctx = _drawing.Open())
            {
                clearDrawing(ctx);
                foreach(VECTOR p in pts)
                    drawPoint(ctx, p, _pointBrush, DEFAULT_POINT_RADIUS);
            }
        }

        private void clearAndDrawControlPoints(IEnumerable<CubicBezier> curves)
        {
            using(DrawingContext ctx = _drawing.Open())
            {
                clearDrawing(ctx);
                int i = 0;
                foreach(CubicBezier curve in curves)
                {
                    Brush brush = _partBrushes[i++ % _partBrushes.Length];
                    if(i == 1) // only draw first point on the first curve, since the rest will be coincident with previous curve
                        drawPoint(ctx, curve.p0, brush, CONTROL_POINT_RADIUS);
                    drawPoint(ctx, curve.p1, brush, CONTROL_POINT_RADIUS);
                    drawPoint(ctx, curve.p2, brush, CONTROL_POINT_RADIUS);
                    drawPoint(ctx, curve.p3, brush, CONTROL_POINT_RADIUS);
                }
                
            }
        }

        private void clearAndDrawSpline(IEnumerable<CubicBezier> curves)
        {
            try
            {
                // Spline throws exceptions sometimes, so wrap it in a try/catch
                _spline.Clear();
                foreach(CubicBezier c in curves)
                    _spline.Add(c);
            }
            catch(Exception e)
            {
                _log.error(e);
                clearDrawing();
                return;
            }

            if(_spline.Length == 0)
            {
                clearDrawing();
            }
            else
            {
                using(DrawingContext ctx = _drawing.Open())
                {
                    clearDrawing(ctx);
                    int nPoints = (int) Math.Round(_spline.Length / SPLINE_POINT_DISTANCE);
                    float ratio = 1f / (nPoints - 1);
                    for(int i = 0; i < nPoints; i++)
                    {
                        float u = i * ratio;
                        Spline.SamplePos pos = _spline.GetSamplePosition(u);
                        VECTOR p = _spline.Curves[pos.Index].Sample(pos.Time);
                        Brush brush = _partBrushes[pos.Index % _partBrushes.Length];
                        drawPoint(ctx, p, brush, DEFAULT_POINT_RADIUS);
                    }
                }
            }

            
        }

        private void clearAndDrawCurves(IEnumerable<CubicBezier> curves, bool colorize)
        {
            using(DrawingContext dctx = _drawing.Open())
            {
                clearDrawing(dctx);
                int i = 0;
                foreach(CubicBezier curve in curves)
                {
                    // each curve segment is a seperate StreamGeometry since they use different pens
                    Pen pen = colorize ? _partPens[i++ % _partPens.Length] : _partPens[0];
                    StreamGeometry geo = new StreamGeometry();
                    using(StreamGeometryContext gctx = geo.Open())
                    {
                        gctx.BeginFigure(toWpfPoint(curve.p0), false, false);
                        gctx.BezierTo(toWpfPoint(curve.p1), toWpfPoint(curve.p2), toWpfPoint(curve.p3), true, false);
                    }
                    geo.Freeze();
                    dctx.DrawGeometry(null, pen, geo);
                }
            }
        }

        /// <summary>
        /// Clears the drawing and closes the context.
        /// </summary>
        private void clearDrawing()
        {
            using(DrawingContext ctx = _drawing.Open())
                clearDrawing(ctx);
        }

        /// <summary>
        /// Clears the drawing using an open context. This lets you do other things with the drawing before
        /// closing the context.
        /// </summary>
        private void clearDrawing(DrawingContext ctx)
        {
            int width = (int) ActualWidth;
            int height = (int) ActualHeight;
            Rect rect = new Rect(0, 0, width, height);
            _clipRect.Rect = rect;
            ctx.DrawRectangle(_backgroundBrush, null, rect);
            ctx.PushClip(_clipRect);
        }

        /// <summary>
        /// Draws a point on the existing drawing using the default point brush/radius.
        /// </summary>
        private void drawPoint(VECTOR p)
        {
            using(DrawingContext ctx = _drawing.Append())
                drawPoint(ctx, p, _pointBrush, DEFAULT_POINT_RADIUS);
        }

        /// <summary>
        /// Draws a point to the context using the specified brush/radius.
        /// </summary>
        private static void drawPoint(DrawingContext ctx, VECTOR p, Brush brush, double radius)
        {
            ctx.DrawEllipse(brush, null, toWpfPoint(p), radius, radius);
        }

        private static Point toWpfPoint(VECTOR p)
        {
            return new Point(VectorHelper.GetX(p), VectorHelper.GetY(p));
        }

        private static VECTOR toVector(Point p)
        {
            return new VECTOR((FLOAT) p.X, (FLOAT) p.Y);
        }
        #endregion
    }

    public enum RenderModes
    {
        POINTS,
        PREPROCESS,
        CONTROL_POINTS,
        SPLINE_DRAW,
        WPF_DRAW,
    }

    public enum PreprocessModes
    {
        NONE,
        LINEAR,
        RDP
    }
}