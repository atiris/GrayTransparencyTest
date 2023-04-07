# GrayTransparencyTest

**This repository was created to support answering questions on stackoverflow.**

Can you advise me how to **ensure transparency after conversion to grayscale** in the case of a wpf c# application and resource image in png format? I've read multiple resources and tried multiple approaches, but I've never been able to get transparency programmatically.

Result looks like this:  
<img src="https://i.stack.imgur.com/k64wU.png" width="300" alt="Result" />    
_I need the 4th image to retain the transparency of the original image after converting to grayscale._

Using XAML everything works:

```xaml
<Image>
  <Image.Source>
    <FormatConvertedBitmap DestinationFormat="Gray16">
      <FormatConvertedBitmap.Source>
        <BitmapImage UriSource="pack://application:,,,/GrayTransparencyTest;component/Media/priority.png" />
      </FormatConvertedBitmap.Source>
    </FormatConvertedBitmap>
  </Image.Source>
  <Image.OpacityMask>
    <ImageBrush>
      <ImageBrush.ImageSource>
        <BitmapImage UriSource="pack://application:,,,/GrayTransparencyTest;component/Media/priority.png" />
      </ImageBrush.ImageSource>
    </ImageBrush>
  </Image.OpacityMask>
</Image>
```

Using C# method and binding, no chance:

```cs
string imageKey = "priority";
Uri imageURI = new Uri($"pack://application:,,,/GrayTransparencyTest;component/Media/{imageKey}.png", UriKind.Absolute);
BitmapImage bitmapImage = new BitmapImage(imageURI);
ImageBrush opacityMask = new ImageBrush()
{
  ImageSource = bitmapImage
};
FormatConvertedBitmap bitmapGreyscale = new FormatConvertedBitmap();
bitmapGreyscale.BeginInit();
bitmapGreyscale.Source = new BitmapImage(imageURI);
bitmapGreyscale.DestinationFormat = PixelFormats.Gray16;
bitmapGreyscale.EndInit();
ImageGray = new Image()
{
  Source = bitmapGreyscale,
  OpacityMask = opacityMask,
};
```

I tried several approaches found on stackoverflow, but all were dead ends. I even converted BitmapImage to System.Drawing Bitmap, used ColorMatrix and converted back but nothing helped. I'm starting to think that this can't be done. In the original program, I added their grayscale versions as resources as a bypass of the real solution, but it is possible that this does not have a solution (without using disk storage and external libraries).
