using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace XGMusic.Utility
{
    class PictureHelper
    {
        static public async Task SavePicture(IRandomAccessStream inputStream, string outputFileName)
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists);
            StorageFile outputFile = null;
            try
            {
                outputFile = await folder.CreateFileAsync(outputFileName, CreationCollisionOption.FailIfExists);
            }
            catch (Exception ex)
            {
                return;
            }

            try
            {
                using (var outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    outputStream.Size = 0;

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inputStream);
                    BitmapPixelFormat format = decoder.BitmapPixelFormat;
                    BitmapAlphaMode alpha = decoder.BitmapAlphaMode;

                    //PixelDataProvider pixelProvider = await decoder.GetPixelDataAsync(
                    //    format,
                    //    alpha,
                    //    transform,
                    //    ExifOrientationMode.RespectExifOrientation,
                    //    ColorManagementMode.ColorManageToSRgb
                    //    );
                    byte[] pixels = (await decoder.GetPixelDataAsync()).DetachPixelData();

                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);
                    encoder.SetPixelData(
                        format,
                        alpha,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels
                        );

                    await encoder.FlushAsync();
                    outputStream.Dispose();
                }
                inputStream.Dispose();
            }
            catch (Exception err)
            {
            }
        }

        static public async void SaveAsPicture(StorageFile inputFile)
        {

            try
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.FileTypeChoices.Add("JPEG image", new string[] { ".jpg" });
                picker.FileTypeChoices.Add("PNG image", new string[] { ".png" });
                picker.FileTypeChoices.Add("BMP image", new string[] { ".bmp" });
                picker.DefaultFileExtension = ".png";
                picker.SuggestedFileName = "Output file";
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                StorageFile outputFile = await picker.PickSaveFileAsync();
                if (outputFile == null)
                {
                    return;
                }

                Guid encoderId;
                switch (outputFile.FileType)
                {
                    case ".png":
                        encoderId = BitmapEncoder.PngEncoderId;
                        break;
                    case ".bmp":
                        encoderId = BitmapEncoder.BmpEncoderId;
                        break;
                    case ".jpg":
                    default:
                        encoderId = BitmapEncoder.JpegEncoderId;
                        break;
                }

                using (IRandomAccessStream inputStream = await inputFile.OpenAsync(FileAccessMode.Read),
                    outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    outputStream.Size = 0;
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inputStream);

                    PixelDataProvider pixelProvider = await decoder.GetPixelDataAsync();

                    byte[] pixels = pixelProvider.DetachPixelData();

                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, outputStream);
                    encoder.SetPixelData(
                        decoder.BitmapPixelFormat,
                        decoder.BitmapAlphaMode,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels
                        );
                    await encoder.FlushAsync();
                    inputStream.Dispose();
                    outputStream.Dispose();
                }
            }
            catch (Exception err)
            {
            }
        }

        static public async Task CopyPicture(string outputFolderName, string outputFileName)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                StorageFolder folder = null;
                if (string.IsNullOrEmpty(outputFolderName))
                {
                    folder = ApplicationData.Current.LocalFolder;
                }
                else
                {
                    folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(outputFolderName, CreationCollisionOption.OpenIfExists);
                }
                StorageFile outputFile = await folder.CreateFileAsync(outputFileName, CreationCollisionOption.OpenIfExists);

                using (IRandomAccessStream inputStream = await file.OpenAsync(FileAccessMode.Read),
                    outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    outputStream.Size = 0;
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inputStream);

                    PixelDataProvider pixelProvider = await decoder.GetPixelDataAsync();

                    byte[] pixels = pixelProvider.DetachPixelData();

                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputStream);
                    encoder.SetPixelData(
                        decoder.BitmapPixelFormat,
                        decoder.BitmapAlphaMode,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels
                        );
                    await encoder.FlushAsync();
                    inputStream.Dispose();
                    outputStream.Dispose();
                }
            }
        }
    }
}
