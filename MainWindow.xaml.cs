using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

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
            Uri imageURI = new Uri($"pack://application:,,,/GrayTransparencyTest;component/Media/{imageKey}.png", UriKind.Absolute);
            BitmapImage bitmapImage = new BitmapImage(imageURI);
            ImageBrush opacityMask = new ImageBrush()
            {
                ImageSource = bitmapImage
            };
            ImageDefault = new Image()
            {
                Source = bitmapImage,
            };
            FormatConvertedBitmap bitmapGreyscale = new FormatConvertedBitmap(); bitmapGreyscale.BeginInit(); bitmapGreyscale.Source = new BitmapImage(imageURI); bitmapGreyscale.DestinationFormat = PixelFormats.Gray16; bitmapGreyscale.EndInit();
            ImageGray = new Image()
            {
                Source = bitmapGreyscale,
                OpacityMask = opacityMask,
            };
        }
    }
}
