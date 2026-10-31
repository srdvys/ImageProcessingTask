using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class ImageProcessingService
    {
        public void ApplyHeavyFilter(string inputPath, string outputPath, Action<int> reportProgress, string filterType = "grayscale")
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

                    byte newR = r, newG = g, newB = b;

                    if (filterType == "negative")
                    {
                        newR = (byte)(255 - r);
                        newG = (byte)(255 - g);
                        newB = (byte)(255 - b);
                    }
                    else if (filterType == "sepia")
                    {
                        newR = (byte)Math.Min(255, (r * 0.393) + (g * 0.769) + (b * 0.189));
                        newG = (byte)Math.Min(255, (r * 0.349) + (g * 0.686) + (b * 0.168));
                        newB = (byte)Math.Min(255, (r * 0.272) + (g * 0.534) + (b * 0.131));
                    }
                    else if (filterType == "red")
                    {
                        newR = (byte)Math.Min(255, r * 1.5);
                        newG = (byte)(g * 0.5);
                        newB = (byte)(b * 0.5);
                    }
                    else if (filterType == "blue")
                    {
                        newR = (byte)(r * 0.5);
                        newG = (byte)(g * 0.5);
                        newB = (byte)Math.Min(255, b * 1.5);
                    }
                    else 
                    {
                        byte gray = (byte)((r * 0.299) + (g * 0.587) + (b * 0.114));
                        newR = gray;
                        newG = gray;
                        newB = gray;
                    }

                    double heavyMath = Math.Sqrt(Math.Pow(newR, 2) + Math.Pow(a, 2));

                    pixelBytes[pixelOffset] = newR;
                    pixelBytes[pixelOffset + 1] = newG;
                    pixelBytes[pixelOffset + 2] = newB;
                }

                lock (lockObj)
                {
                    processedPixels += width;
                    int progress = (int)((double)processedPixels / totalPixels * 100);
                    reportProgress?.Invoke(progress);

                }
            });

            using var resultImage = Image.LoadPixelData<Rgba32>(pixelBytes, width, height);
            resultImage.Save(outputPath);
        }
    }
}