using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using XGMusic.DataConverter;

namespace XGMusic.FormatConverter
{
    public sealed class IsCheckedToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return Windows.UI.Xaml.Visibility.Visible;
            else
                return Windows.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class IsCheckedToCollapsed : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return Windows.UI.Xaml.Visibility.Collapsed;
            else
                return Windows.UI.Xaml.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class ThumbnailPathToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var image = new BitmapImage();
            string thumbnail = (String)value;
            if (string.IsNullOrEmpty(thumbnail))
            {
                image.UriSource = new Uri("ms-appx:///Assets/Images/DefaultThumbnailImage.jpg");
            }
            else
            {
                var folder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists).AsTask<StorageFolder>().Result;
                var file = folder.CreateFileAsync(thumbnail, CreationCollisionOption.OpenIfExists).AsTask<StorageFile>().Result;
                using (var stream = file.OpenAsync(FileAccessMode.Read).AsTask<IRandomAccessStream>().Result)
                {
                    image.SetSource(stream);
                }
            }
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class MusicInforToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MusicInfor music = (value as MusicInfor);
            if (music == null)
                return null;
            string thumbnail = music.ThumbnailImage;
            var image = new BitmapImage();
            if (string.IsNullOrEmpty(thumbnail))
            {
                image.UriSource = new Uri("ms-appx:///Assets/Images/DefaultThumbnailImage.jpg");
            }
            else
            {
                var folder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists).AsTask<StorageFolder>().Result;
                var file = folder.CreateFileAsync(thumbnail, CreationCollisionOption.OpenIfExists).AsTask<StorageFile>().Result;
                using (var stream = file.OpenAsync(FileAccessMode.Read).AsTask<IRandomAccessStream>().Result)
                {
                    image.SetSource(stream);
                }
            }
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
