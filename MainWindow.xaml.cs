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
            /*
            FormatConvertedBitmap bitmapGreyscale = new FormatConvertedBitmap(); bitmapGreyscale.BeginInit(); bitmapGreyscale.Source = new BitmapImage(imageURI); bitmapGreyscale.DestinationFormat = PixelFormats.Gray16; bitmapGreyscale.EndInit();
            ImageGray = new Image()
            {
                Source = bitmapGreyscale,
                OpacityMask = opacityMask,
            };
            */
            ImageGray = new Image()
            {
                Source = GetTransparentGrayscale(imageUri)
            };
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
