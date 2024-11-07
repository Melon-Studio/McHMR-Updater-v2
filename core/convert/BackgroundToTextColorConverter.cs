using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;

namespace McHMR_Updater_v2.core.convert;
public class BackgroundToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var background = value as Brush;
        if (background is ImageBrush imageBrush)
        {
            var bitmapImage = new BitmapImage(new Uri(imageBrush.ImageSource.ToString()));
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);

                memoryStream.Position = 0;
                using (var bitmap = new Bitmap(memoryStream))
                {
                    int totalBrightness = 0;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            var color = bitmap.GetPixel(x, y);
                            int brightness = (int)(0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
                            totalBrightness += brightness;
                        }
                    }
                    int averageBrightness = totalBrightness / (bitmap.Width * bitmap.Height);
                    return averageBrightness < 128 ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
                }
            }
        }
        return System.Windows.Media.Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
