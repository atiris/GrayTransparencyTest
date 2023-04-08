using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Resources;
using SysDraw = System.Drawing;

namespace GrayTransparencyTest
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private Image imageDefault = new Image();
        public Image ImageDefault
        {
            get { return imageDefault; }
            private set { imageDefault = value; PropertyChangedInvoke(); }
        }

        private Image imageGray = new Image();
        public Image ImageGray
        {
            get { return imageGray; }
            private set { imageGray = value; PropertyChangedInvoke(); }
        }

        private Image imageGraySolution1 = new Image();
        public Image ImageGraySolution1
        {
            get { return imageGraySolution1; }
            private set { imageGraySolution1 = value; PropertyChangedInvoke(); }
        }

        private Image imageGraySolution2 = new Image();
        public Image ImageGraySolution2
        {
            get { return imageGraySolution2; }
            private set { imageGraySolution2 = value; PropertyChangedInvoke(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void PropertyChangedInvoke([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            InitializeImages();
        }

        private void InitializeImages()
        {
            string imageKey = "priority";
            Uri imageUri = new Uri($"pack://application:,,,/GrayTransparencyTest;component/Media/{imageKey}.png", UriKind.Absolute);
            BitmapImage bitmapImage = new BitmapImage(imageUri);
            ImageBrush opacityMask = new ImageBrush()
            {
                ImageSource = bitmapImage
            };
            ImageDefault = new Image()
            {
                Source = bitmapImage,
            };
            FormatConvertedBitmap bitmapGreyscale = new FormatConvertedBitmap();
            bitmapGreyscale.BeginInit();
            bitmapGreyscale.Source = new BitmapImage(imageUri);
            bitmapGreyscale.DestinationFormat = PixelFormats.Gray16;
            bitmapGreyscale.EndInit();
            ImageGray = new Image()
            {
                Source = bitmapGreyscale,
                OpacityMask = opacityMask,
            };

            // https://stackoverflow.com/a/75962106/659223
            ImageGraySolution1 = new Image()
            {
                Source = GetTransparentGrayscale(imageUri)
            };

            // https://stackoverflow.com/a/75963505/659223
            ImageGraySolution2 = new Image()
            {
                Source = ConvertToGrayscale(bitmapImage)
            };
        }

        public static BitmapSource ConvertToGrayscale(BitmapSource source)
        {
            var stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[stride * source.PixelWidth];

            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                // this works for PixelFormats.Bgra32
                var blue = pixels[i];
                var green = pixels[i + 1];
                var red = pixels[i + 2];
                var gray = (byte)(0.2126 * red + 0.7152 * green + 0.0722 * blue);
                pixels[i] = gray;
                pixels[i + 1] = gray;
                pixels[i + 2] = gray;
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);
        }

        public ImageSource? GetTransparentGrayscale(Uri imageUri)
        {
            StreamResourceInfo sri = Application.GetResourceStream(imageUri);
            ImageSource? imageSource = default;
            if (sri != null)
            {
                using (Stream s = sri.Stream)
                {
                    imageSource = ConvertToTransparentGrayscale(s);
                }
            }
            return imageSource;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        public static unsafe ImageSource? ConvertToTransparentGrayscale(Stream png)
        {
            SysDraw.Bitmap? bmp = SysDraw.Bitmap.FromStream(png) as SysDraw.Bitmap;
            if (bmp == null) { return null; }
            SysDraw.Imaging.BitmapData bits = bmp.LockBits(
                new SysDraw.Rectangle(0, 0, (int)bmp.Width, (int)bmp.Height),
                SysDraw.Imaging.ImageLockMode.ReadWrite,
                SysDraw.Imaging.PixelFormat.Format32bppArgb);

            byte* buf = (byte*)bits.Scan0;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * bits.Stride + x * 4;
                    byte gray = (byte)(0.2126 * buf[offset + 2] + 0.7152 * buf[offset + 1] + 0.0722 * buf[offset]);
                    buf[offset + 1] = gray; // Blue channel
                    buf[offset + 2] = gray; // Green channel
                    buf[offset] = gray; // Red channel
                }
            }

            bmp.UnlockBits(bits);

            IntPtr hBmp = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                   hBmp,
                   IntPtr.Zero,
                   System.Windows.Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBmp);
            }
        }
    }
}
