using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Links to an icon that is a packed resource (ie "Resource" -NOT- "Embedded Resource" in VS) in (project root)/res/icons/(filename).png
    /// and returns an Image with taht icon and the correct width/height. Optionally can include some text to the right of it, in which case
    /// the returned value will be a StackPanel with them both in it. The resource must be in the LOCAL ASSEMBLY (ie not the assembly of the
    /// call site!). For this reason, this class is internal.
    /// 
    /// TODO could use IUriContext from the service provider to make this less hacky
    /// </summary>
    [MarkupExtensionReturnType(typeof(FrameworkElement))]
    internal sealed class IconExtension : MarkupExtension
    {
        private const string ICONS_PATH = "res/icons/";
        private static readonly string _resourceUriPrefix = @"pack://application:,,,/" + typeof(IconExtension).Assembly.FullName + ";component/";
        private static readonly string _iconsUriPrefix = _resourceUriPrefix + ICONS_PATH;

        public string text { get; set; }
        public string filename { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }

        public IconExtension() { }
        public IconExtension(string filename) : this() { this.filename = filename; }
        public IconExtension(string filename, string text) : this(filename) { this.text = text; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            BitmapImage src = loadIcon(filename);
            Image img = new Image { Width = width ?? src.PixelWidth, Height = height ?? src.PixelHeight, Source = src, VerticalAlignment = VerticalAlignment.Center };
            if(string.IsNullOrWhiteSpace(text))
                return img;
            StackPanel root = new StackPanel { Orientation = Orientation.Horizontal };
            root.Children.Add(img);
            root.Children.Add(new TextBlock { Text = text, Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            return root;
        }

        public static BitmapImage loadIcon(string filename)
        {
            Uri uri = new Uri(_iconsUriPrefix + filename + ".png", UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}