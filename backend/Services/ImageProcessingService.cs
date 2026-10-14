using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class ImageProcessingService
    {
        public void ApplyHeavyFilter(string inputPath, string outputPath, Action<int> onProgress)
        {
            using var image = Image.Load<Rgba32>(inputPath);
            int width = image.Width;
            int height = image.Height;
            int totalPixels = width * height;
            int processedPixels = 0;

            byte[] pixelBytes = new byte[width * height * 4];
            image.CopyPixelDataTo(pixelBytes);

            object lockObj = new object();

            Parallel.For(0, height, y =>
            {
                int rowStartOffset = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowStartOffset + (x * 4);

                    byte r = pixelBytes[pixelOffset];
                    byte g = pixelBytes[pixelOffset + 1];
                    byte b = pixelBytes[pixelOffset + 2];
                    byte a = pixelBytes[pixelOffset + 3];

                    byte gray = (byte)((r * 0.299) + (g * 0.587) + (b * 0.114));

                    double heavyMath = Math.Sqrt(Math.Pow(gray, 2) + Math.Pow(a, 2));

                    pixelBytes[pixelOffset] = gray; //R
                    pixelBytes[pixelOffset + 1] = gray; //G
                    pixelBytes[pixelOffset + 2] = (byte)(gray > 128 ? gray - 10 : gray + 10); //B
                }

                lock (lockObj)
                {
                    processedPixels += width;
                    int progress = (int)((double)processedPixels / totalPixels * 100);
                    onProgress?.Invoke(progress);
                }
            });

            using var resultImage = Image.LoadPixelData<Rgba32>(pixelBytes, width, height);
            resultImage.Save(outputPath);
        }
    }
}