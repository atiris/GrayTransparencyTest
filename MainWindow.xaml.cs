using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using d = System.Drawing;
using System.IO;
using System.Windows.Interop;

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

        private BitmapImage Gray(BitmapImage src)
        {
            d.Bitmap b = BitmapImage2Bitmap(src);
            d.Bitmap c = gr(b);
            return Bitmap2BitmapImage(c);
        }

        private void InitializeImages()
        {
            string imageKey = "priority";
            Uri imageURI = new Uri($"pack://application:,,,/GrayTransparencyTest;component/Media/{imageKey}.png", UriKind.Absolute);
            BitmapImage bitmapImage = new BitmapImage(imageURI);
            ImageBrush opacityMask = new ImageBrush()
            {
                ImageSource = bitmapImage
            };
            ImageDefault = new Image()
            {
                Source = bitmapImage,
                // OpacityMask = opacityMask, // this is not mandatory for default image
            };
            ImageGray = new Image()
            {
                Source = GetGrayscaledImage("priority.png"),
            };

            /*
            FormatConvertedBitmap bitmapGreyscale = new FormatConvertedBitmap();
            bitmapGreyscale.BeginInit();
            bitmapGreyscale.Source = new BitmapImage(imageURI);
            bitmapGreyscale.DestinationPalette = BitmapPalettes.Gray16Transparent;
            bitmapGreyscale.EndInit();
            ImageGray = new Image()
            {
                Source = bitmapGreyscale,
                OpacityMask = opacityMask,
            };
            */
        }


        private BitmapImage GetGrayscaledImage(string imageName)
        {
            Uri imageUri = new Uri($"pack://application:,,,/GrayTransparencyTest;component/Media/{imageName}");
            BitmapImage originalImage = new BitmapImage(imageUri);

            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap(originalImage, PixelFormats.Gray16, null, 0);
            grayBitmap.DestinationFormat = PixelFormats.Bgra32;

            int stride = (originalImage.PixelWidth * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[originalImage.PixelHeight * stride];

            grayBitmap.CopyPixels(pixelData, stride, 0);
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                byte gray = pixelData[i];
                byte alpha = pixelData[i + 3];
                byte newAlpha = alpha;
                byte newValue = gray;

                if (alpha == 0)
                {
                    newAlpha = 0;
                }
                else if (alpha != 255)
                {
                    // Convert gray value to alpha value, assuming that black = 0 and white = 255
                    // This formula maps [0,255] -> [0, alpha], so that gray=0 (black) maps to 0 (transparent)
                    // and gray=255 (white) maps to alpha (fully opaque)
                    newAlpha = (byte)((gray * alpha + 128) / 255);
                }

                pixelData[i] = newValue;
                pixelData[i + 3] = newAlpha;
            }

            BitmapSource bgraBitmap = BitmapSource.Create(originalImage.PixelWidth, originalImage.PixelHeight, originalImage.DpiX, originalImage.DpiY, PixelFormats.Bgra32, null, pixelData, stride);

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(bgraBitmap, new Rect(0, 0, originalImage.PixelWidth, originalImage.PixelHeight));
            }

            RenderTargetBitmap renderedBitmap = new RenderTargetBitmap(originalImage.PixelWidth, originalImage.PixelHeight, originalImage.DpiX, originalImage.DpiY, PixelFormats.Default);
            renderedBitmap.Render(drawingVisual);

            BitmapImage grayscaledImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderedBitmap));
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                grayscaledImage.BeginInit();
                grayscaledImage.StreamSource = stream;
                grayscaledImage.CacheOption = BitmapCacheOption.OnLoad;
                grayscaledImage.EndInit();
            }

            return grayscaledImage;
        }




        private MemoryStream ConvertToPngStream(BitmapSource bitmap)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }


        /*
        public static BitmapImage ToBitmapImage(this d.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, d.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
        */

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private BitmapImage Bitmap2BitmapImage(d.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapImage retval;

            try
            {
                retval = (BitmapImage)Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return retval;
        }

        private d.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                d.Bitmap bitmap = new d.Bitmap(outStream);

                return new d.Bitmap(bitmap);
            }
        }

        private d.Bitmap gr(d.Bitmap original)
        {
            d.Bitmap newBitmap = new d.Bitmap(original.Width, original.Height);
            d.Graphics g = d.Graphics.FromImage(newBitmap);
            d.Imaging.ColorMatrix colorMatrix = new d.Imaging.ColorMatrix(
            new float[][]
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {     0,      0,      0, 1, 0},
                new float[] {     0,      0,      0, 0, 1}
            });

            d.Imaging.ImageAttributes attributes = new d.Imaging.ImageAttributes();

            attributes.SetColorMatrix(colorMatrix, d.Imaging.ColorMatrixFlag.Default, d.Imaging.ColorAdjustType.Bitmap);

            g.DrawImage(original, new d.Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, d.GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        public Image ConvertToGrayscale(d.Image image)
        {
            d.Image grayscaleImage = new d.Bitmap(image.Width, image.Height, image.PixelFormat);

            // Create the ImageAttributes object and apply the ColorMatrix
            d.Imaging.ImageAttributes attributes = new d.Imaging.ImageAttributes();
            d.Imaging.ColorMatrix grayscaleMatrix = new d.Imaging.ColorMatrix(new float[][]{
        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
        new float[] {     0,      0,      0, 1, 0},
        new float[] {     0,      0,      0, 0, 1}
        });
            attributes.SetColorMatrix(grayscaleMatrix);

            // Use a new Graphics object from the new image.
            using (d.Graphics g = d.Graphics.FromImage(grayscaleImage))
            {
                // Draw the original image using the ImageAttributes created above.
                g.DrawImage(image,
                            new d.Rectangle(0, 0, grayscaleImage.Width, grayscaleImage.Height),
                            0, 0, grayscaleImage.Width, grayscaleImage.Height,
                            d.GraphicsUnit.Pixel,
                            attributes);
            }

            Image convertedImage;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    grayscaleImage.Save(ms, d.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    convertedImage = new Image { Source = decoder.Frames[0] };
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return convertedImage;
        }
    }
}
