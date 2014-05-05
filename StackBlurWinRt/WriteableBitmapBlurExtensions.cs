using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace StackBlur
{
    public static class WriteableBitmapBlurExtensions
    {
        /// <summary>
        /// Blurs original image with specified radius
        /// </summary>
        /// <param name="img">Image to be blurred</param>
        /// <param name="radius">Radius of blur</param>
        public static void SuperfastBlur(this WriteableBitmap img, int radius)
        {
            if (radius < 1)
            {
                return;
            }
            var width = img.PixelWidth;
            var height = img.PixelHeight;
            var maximumXIndex = width - 1;
            var maximumYIndex = height - 1;
            var totalPixels = width * height;
            var convolutionKernelSize = radius + radius + 1;
            var r = new int[totalPixels];
            var g = new int[totalPixels];
            var b = new int[totalPixels];
            var precalculatedRightIndex = new int[Math.Max(width, height)];
            var precalculatedLeftIndex = new int[Math.Max(width, height)];
            var pixels = img.PixelBuffer.ToArray();
            var precalculatedQuotients = new int[256 * convolutionKernelSize];

            var index = 0;
            for (var i = 0; i < 256; i++)
            {
                for (var j = 0; j < convolutionKernelSize; j++)
                {
                    precalculatedQuotients[index] = i;
                    index++;
                }
            }

            index = 0;
            var currentRowBase = 0;

            for (var y = 0; y < height; y++)
            {
                var bsum = 0;
                var gsum = 0;
                var rsum = 0;
                for (var i = -radius; i <= radius; i++)
                {
                    var pixel = pixels[index + Math.Min(maximumXIndex, Math.Max(i, 0))];
                    rsum += (pixel & 0xff0000) >> 16;
                    gsum += (pixel & 0x00ff00) >> 8;
                    bsum += pixel & 0x0000ff;
                }
                for (var x = 0; x < width; x++)
                {

                    r[index] = precalculatedQuotients[rsum];
                    g[index] = precalculatedQuotients[gsum];
                    b[index] = precalculatedQuotients[bsum];

                    if (y == 0)
                    {
                        precalculatedRightIndex[x] = Math.Min(x + radius + 1, maximumXIndex);
                        precalculatedLeftIndex[x] = Math.Max(x - radius, 0);
                    }
                    var pixelToBeAdded = pixels[currentRowBase + precalculatedRightIndex[x]];
                    var pixelToBeRemoved = pixels[currentRowBase + precalculatedLeftIndex[x]];

                    rsum += ((pixelToBeAdded & 0xff0000) - (pixelToBeRemoved & 0xff0000)) >> 16;
                    gsum += ((pixelToBeAdded & 0x00ff00) - (pixelToBeRemoved & 0x00ff00)) >> 8;
                    bsum += (pixelToBeAdded & 0x0000ff) - (pixelToBeRemoved & 0x0000ff);
                    index++;
                }
                currentRowBase += width;
            }

            for (var x = 0; x < width; x++)
            {
                var bsum = 0;
                var gsum = 0;
                var rsum = 0;
                currentRowBase = -radius * width;
                for (var i = -radius; i <= radius; i++)
                {
                    index = Math.Max(0, currentRowBase) + x;
                    rsum += r[index];
                    gsum += g[index];
                    bsum += b[index];
                    currentRowBase += width;
                }
                index = x;
                for (var y = 0; y < height; y++)
                {
                    pixels[index] = (byte)(0xff000000 | (precalculatedQuotients[rsum] << 16) | (precalculatedQuotients[gsum] << 8) | precalculatedQuotients[bsum]);
                    if (x == 0)
                    {
                        precalculatedRightIndex[y] = Math.Min(y + radius + 1, maximumYIndex) * width;
                        precalculatedLeftIndex[y] = Math.Max(y - radius, 0) * width;
                    }
                    var pixelToBeAdded = x + precalculatedRightIndex[y];
                    var pixelToBeRemoved = x + precalculatedLeftIndex[y];

                    rsum += r[pixelToBeAdded] - r[pixelToBeRemoved];
                    gsum += g[pixelToBeAdded] - g[pixelToBeRemoved];
                    bsum += b[pixelToBeAdded] - b[pixelToBeRemoved];

                    index += width;
                }
            }
            var targetStream = img.PixelBuffer.AsStream();
            targetStream.Seek(0, SeekOrigin.Begin);
            targetStream.Write(pixels, 0, pixels.Length);
        }
    }
}

