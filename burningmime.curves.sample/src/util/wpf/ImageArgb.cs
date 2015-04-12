using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Control containing and Image backed by a writeable array of pixels. Manages resizing by cropping/padding. Need to call update() for
    /// any changes to be reflected. For example, a typical update will look like:
    /// 
    ///     ColorArgb[] pixels;
    ///     int width, height;
    ///     if(_bitmap.tryGetPixels(out pixels, out width, out height))
    ///     {
    ///         // modify the pixels array itself
    ///         _bitmap.update();
    ///     }
    /// 
    /// Note that this is a UserControl instead of an Image because of layout/sizing issues with Image. So this control is actually the container
    /// within which the image is shown.
    /// </summary>
    public sealed partial class ImageArgb : UserControl
    {
        /// <summary>
        /// Default interval in seconds after the control is resized that the underlying bitmap is resized.
        /// </summary>
        public const double DEFAULT_RESIZE_DELAY = .25;

        private readonly Image _image;
        private readonly BitmapArgb _bitmap;
        private double _resizeDelay = DEFAULT_RESIZE_DELAY;

        public ImageArgb()
        {
            _image = new Image();
            _bitmap = new BitmapArgb();
            this.whenLoadedAndReloaded(onLoaded, onReloaded);
            Content = _image;
        }

        /// <summary>
        /// Called after the underlying bitmap is resized. Remember it could have been "resized away" (ie hasImage will be false).
        /// </summary>
        public EventHandler afterResize;

        /// <summary>
        /// Interval in seconds after the control is resized that the underlying bitmap is resized. Can only set this before the control is loaded.
        /// </summary>
        public double resizeDelay
        {
            get { return _resizeDelay; }
            set
            {
                if(IsLoaded)
                    throw new InvalidOperationException("Cannot set resizeDelay after image has been loaded");
                _resizeDelay = value;
            }
        }

        /// <summary>
        /// Do we currently have a bitmap created?
        /// </summary>
        public bool hasImage { get { return _bitmap.hasImage; } }

        /// <summary>
        /// Width of the bitmap.
        /// </summary>
        public int bitmapWidth { get { return _bitmap.width; } }

        /// <summary>
        /// Height of the bitmap.
        /// </summary>
        public int bitmapHeight { get { return _bitmap.height; } }

        /// <summary>
        /// Clears the array with default color (<see cref="Control.Background"/> if it is set, otherwise transparent black). Does not update.
        /// </summary>
        public void clear() { _bitmap.clear(BackgroundColor); }

        /// <summary>
        /// Clears the array with default color (<see cref="Control.Background"/> if it is set, otherwise transparent black), then pushes the changes to the underlying bitmap.
        /// </summary>
        public void clearAndUpdate() { _bitmap.clearAndUpdate(BackgroundColor); }

        /// <summary>
        /// Clears the array. Does not update.
        /// </summary>
        /// <param name="color">Color to clear with.</param>
        public void clear(ColorArgb color) { _bitmap.clear(color); }

        /// <summary>
        /// Clears the array, then pushes the changes to the underlying bitmap.
        /// </summary>
        /// <param name="color">Color to clear with.</param>
        public void clearAndUpdate(ColorArgb color) { _bitmap.clearAndUpdate(color); }

        /// <summary>
        /// Updates the udnerlying bitmap. Call this when you're done modifying color values.
        /// </summary>
        public void update() { _bitmap.update(); }

        /// <summary>
        /// Tries to get the underlying array if there is an existing image. If it can't, returns false. Don't cache this
        /// array for long periods in case the image is resized.
        /// </summary>
        public bool tryGetPixels(out ColorArgb[] pixels, out int width, out int height) { return _bitmap.tryGetPixels(out pixels, out width, out height); }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            recreateBitmap();
            UiUtils.watchForResize(this, .5f, onResized);
        }

        private void onReloaded(object sender, RoutedEventArgs e)
        {
            // This is needed since Loaded is sometimes called multiple times. On subsequent loads, we still
            // want to recreate the bitmap in case there was a resize we missed
            onResized();
        }

        private void onResized()
        {
            if(recreateBitmap())
            {
                EventHandler handler = afterResize;
                if(handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        private bool recreateBitmap()
        {
            int width = (int) ActualWidth;
            int height = (int) ActualHeight;
            int dpiX, dpiY;
            UiUtils.getDpi(this, out dpiX, out dpiY);
            ImageSource old = _image.Source;
            ImageSource @new = _bitmap.createOrResize(width, height, dpiX, dpiY, BackgroundColor);
            if(old == @new)
                return false; // nothing was recreated
            _image.Source = @new;
            return true;
        }
    }

    /// <summary>
    /// Manages a resizable <see cref="WriteableBitmap"/> backed by an array of ARGB colors. Typically, you'll want to
    /// use <see cref="ImageArgb"/> instead of using this class directly.
    /// </summary>
    public sealed class BitmapArgb
    {
        private WriteableBitmap _source;
        private int _width;
        private int _height;
        private ColorArgb[] _pixels;

        /// <summary>
        /// Do we have an image?
        /// </summary>
        public bool hasImage { get { return _source != null; } }

        /// <summary>
        /// Width of the image.
        /// </summary>
        public int width { get { return _width; } }

        /// <summary>
        /// Height of the image.
        /// </summary>
        public int height { get { return _height; } }

        /// <summary>
        /// Current ImageSource in use (when resized, this will change).
        /// </summary>
        public WriteableBitmap source { get { return _source; } }

        // ReSharper disable ParameterHidesMember
        /// <summary>
        /// Creates the bitmap if none exists, or changes the size of an existing bitmap. Doesn't scale the image, just crops/pads
        /// the edges.
        /// </summary>
        /// <param name="width">Width of the new image.</param>
        /// <param name="height">Height of the new image.</param>
        /// <param name="dpiX">DPI of the image in X direction (if unsure, get from <see cref="UiUtils.getDpi"/> or just use 96).</param>
        /// <param name="dpiY">DPI of the image in Y direction (if unsure, get from <see cref="UiUtils.getDpi"/> or just use 96).</param>
        /// <param name="backgroundColor">Color with which to pad the edges if the new size is bigger than the old.</param>
        /// <returns>The new ImageSource.</returns>
        public WriteableBitmap createOrResize(int width, int height, int dpiX, int dpiY, ColorArgb backgroundColor)
        {
            if(width <= 0 || height <= 0)
            {
                _source = null;
                _pixels = null;
                _width = 0;
                _height = 0;
                return null;
            }

            if(_source != null)
            {
                _source.VerifyAccess(); // we don't want to recreate on a new thread
                if(_source.PixelWidth == width && _source.PixelHeight == height)
                    return _source;
            }
                
            
            int oldWidth = _width;
            int oldHeight = _height;
            ColorArgb[] oldColors = _pixels;
            ColorArgb[] newColors = new ColorArgb[width * height];
            _width = width;
            _height = height;
            _pixels = newColors;

            // Resizing should keep as much as possible
            if(oldColors == null)
            {
                clear(backgroundColor);
            }
            else
            {
                // Copy old data
                int minWidth = Math.Min(width, oldWidth);
                int minHeight = Math.Min(height, oldHeight);
                int y = 0;
                for(; y < minHeight; y++)
                {
                    int x = 0;
                    for(; x < minWidth; x++)
                    {
                        int oldIndex = (y * oldWidth) + x;
                        int newIndex = (y * width) + x;
                        newColors[newIndex] = oldColors[oldIndex];
                    }
                    for(; x < width; x++)
                    {
                        int newIndex = (y * width) + x;
                        newColors[newIndex] = backgroundColor;
                    }
                }

                // Set the rest to the background color
                for(; y < height; y++)
                {
                    for(int x  = 0; x < width; x++)
                    {
                        int newIndex = (y * width) + x;
                        newColors[newIndex] = backgroundColor;
                    }
                }
            }

            _source = new WriteableBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);
            update();
            return _source;
        }

        /// <summary>
        /// Tries to get the underlying array if there is an existing image. If it can't, returns false. Don't cache this
        /// array for long periods in case the image is resized.
        /// </summary>
        public bool tryGetPixels(out ColorArgb[] pixels, out int width, out int height)
        {
            if(hasImage)
            {
                _source.VerifyAccess(); // only allow this to be called in the WPF thread
                pixels = _pixels;
                width = this.width;
                height = this.height;
                return true;
            }
            else
            {
                pixels = null;
                width = 0;
                height = 0;
                return false;
            }
        }

        /// <summary>
        /// Clears the array. Does not update.
        /// </summary>
        /// <param name="backgroundColor">Color to clear with.</param>
        public void clear(ColorArgb backgroundColor)
        {
            if(hasImage)
            {
                _source.VerifyAccess();
                ColorArgb[] c = _pixels;
                for(int i = 0; i < c.Length; i++)
                    c[i] = backgroundColor;
            }
        }

        /// <summary>
        /// Clears the array, then pushes the changes to the underlying bitmap.
        /// </summary>
        /// <param name="backgroundColor">Color to clear with.</param>
        public void clearAndUpdate(ColorArgb backgroundColor)
        {
            clear(backgroundColor);
            update();
        }

        /// <summary>
        /// Updates the udnerlying bitmap. Call this when you're done modifying color values.
        /// </summary>
        public void update()
        {
            if(hasImage)
            {
                _source.VerifyAccess();
                _source.WritePixels(new Int32Rect(0, 0, _width, _height), _pixels, _width * 4, 0);
            }
        }
    }
}