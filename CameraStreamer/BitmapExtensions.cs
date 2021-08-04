using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraStreamer
{
    /// <summary>
    /// Extends the Bitmap class with new Methods
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        /// Converts a Bitmap to a BitmapSource
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns>Newly generated BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            //Create BitmapData from the given Bitmap 
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            //Create BitmapSource from the Bimap and newly created BitmapData
            BitmapSource bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                PixelFormats.Bgr24,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            //Unlock the bitmap from the memory
            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }
    }
}
