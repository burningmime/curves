using System;
using System.Windows.Controls;
using burningmime.util;
using burningmime.util.wpf;

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
    public partial class DrawingFrame
    {
        public DrawingFrame()
        {
            InitializeComponent();
            DataContext = _surface;
            
            Bindings.set(_pointDistText,  TextBlock.TextProperty.of<string>(), _surface, DrawingSurface.PointDistanceProperty, value => "Linearize point distance: " + value);
            Bindings.set(_rdpErrorText,   TextBlock.TextProperty.of<string>(), _surface, DrawingSurface.RdpErrorProperty,      value => "Ramer–Douglas–Peucker error: " + value);
            Bindings.set(_fitErrorText,   TextBlock.TextProperty.of<string>(), _surface, DrawingSurface.FittingErrorProperty,  value => "Curve fitting error: " + value);
            Bindings.set(_fitTimeText,    TextBlock.TextProperty.of<string>(), _surface, DrawingSurface.LastFitTimeProperty,   value => value.isNaN() ? string.Empty : string.Format("Time: {0:0.0000}ms", value));
            Bindings.set(_pointCountText, TextBlock.TextProperty.of<string>(), _surface, DrawingSurface.PointCountProperty,    value => string.IsNullOrEmpty(value) ? string.Empty : "Points before/after pre-processing: " + value);
            
            _accelText.Text = typeof(VECTOR).FullName;
#if SYSTEM_NUMERICS_VECTOR
            _accelText.Text += Environment.NewLine + "Vector.IsHardwareAccelerated = " + (IsHardwareAcceleratedHelper.IsHardwareAccelerated ? "true" : "false");
#endif
        }
    }
}
