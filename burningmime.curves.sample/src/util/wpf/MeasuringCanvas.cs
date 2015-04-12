using System.Windows;
using System.Windows.Controls;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Canvas that measures its own maximum X/Y.
    /// </summary>
    public partial class MeasuringCanvas : Canvas
    {
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double minX = double.NaN;
            double minY = double.NaN;
            double maxX = double.NaN;
            double maxY = double.NaN;
            foreach(UIElement elem in InternalChildren)
            {
                Size size = elem.DesiredSize;
                double x = GetLeft(elem).zeroIfInvalid();
                double y = GetTop(elem).zeroIfInvalid();
                double w = size.Width.zeroIfInvalid();
                double h = size.Height.zeroIfInvalid();
                elem.Arrange(new Rect(x, y, w, h));
                if(w != 0 || h != 0)
                {
                    minX = nanMin(minX, x);
                    minY = nanMin(minY, y);
                    maxX = nanMax(maxX, x + w);
                    maxY = nanMax(maxY, y + h);
                }
            }
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            //MeasuredWidth = maxX.zeroIfNaN() - minX.zeroIfNaN();
            //MeasuredHeight = maxY.zeroIfNaN() - minY.zeroIfNaN();
            return arrangeSize;
        }

        // These assume b is always valid
        private static double nanMin(double a, double b) { return a.isNaN() ? b : (a < b ? a : b); }
        private static double nanMax(double a, double b) { return a.isNaN() ? b : (a > b ? a : b); }
    }
}