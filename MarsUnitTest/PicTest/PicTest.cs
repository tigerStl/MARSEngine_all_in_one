using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MarsUnitTest.PicTest
{
    /// <summary>
    /// 图片测试基础类，提供基本的图片操作功能
    /// </summary>
    public class PicTest
    {
        /// <summary>
        /// 创建指定尺寸和颜色的测试图片
        /// </summary>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="backgroundColor">背景颜色</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>是否创建成功</returns>
        public bool CreateTestImage(int width, int height, Color backgroundColor, string outputPath)
        {
            try
            {
                using (var bitmap = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(backgroundColor);
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建带图案的测试图片
        /// </summary>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="backgroundColor">背景颜色</param>
        /// <param name="patternColor">图案颜色</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>是否创建成功</returns>
        public bool CreatePatternImage(int width, int height, Color backgroundColor, Color patternColor, string outputPath)
        {
            try
            {
                using (var bitmap = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(backgroundColor);
                    
                    // 绘制网格图案
                    using (var pen = new Pen(patternColor, 1))
                    {
                        for (int x = 0; x < width; x += 20)
                        {
                            graphics.DrawLine(pen, x, 0, x, height);
                        }
                        for (int y = 0; y < height; y += 20)
                        {
                            graphics.DrawLine(pen, 0, y, width, y);
                        }
                    }
                    
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 调整图片尺寸
        /// </summary>
        /// <param name="inputPath">输入图片路径</param>
        /// <param name="outputPath">输出图片路径</param>
        /// <param name="newWidth">新宽度</param>
        /// <param name="newHeight">新高度</param>
        /// <returns>是否调整成功</returns>
        public bool ResizeImage(string inputPath, string outputPath, int newWidth, int newHeight)
        {
            try
            {
                using (var originalImage = new Bitmap(inputPath))
                using (var resizedImage = new Bitmap(originalImage, newWidth, newHeight))
                {
                    resizedImage.Save(outputPath, ImageFormat.Png);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取图片的像素数据
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>像素数据数组</returns>
        public Color[,] GetPixelData(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                using (var bitmap = new Bitmap(imagePath))
                {
                    var pixels = new Color[bitmap.Width, bitmap.Height];
                    
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            pixels[x, y] = bitmap.GetPixel(x, y);
                        }
                    }
                    
                    return pixels;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 计算图片的平均颜色
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>平均颜色</returns>
        public Color GetAverageColor(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return Color.Black;
            }

            try
            {
                using (var bitmap = new Bitmap(imagePath))
                {
                    long totalR = 0, totalG = 0, totalB = 0;
                    int pixelCount = bitmap.Width * bitmap.Height;

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            Color pixel = bitmap.GetPixel(x, y);
                            totalR += pixel.R;
                            totalG += pixel.G;
                            totalB += pixel.B;
                        }
                    }

                    int avgR = (int)(totalR / pixelCount);
                    int avgG = (int)(totalG / pixelCount);
                    int avgB = (int)(totalB / pixelCount);

                    return Color.FromArgb(avgR, avgG, avgB);
                }
            }
            catch
            {
                return Color.Black;
            }
        }

        /// <summary>
        /// 验证图片文件格式
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>是否为有效的图片文件</returns>
        public bool IsValidImageFile(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            try
            {
                using (var bitmap = new Bitmap(imagePath))
                {
                    // 尝试访问图片属性来验证格式
                    var width = bitmap.Width;
                    var height = bitmap.Height;
                    return width > 0 && height > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取支持的图片格式列表
        /// </summary>
        /// <returns>支持的格式数组</returns>
        public string[] GetSupportedFormats()
        {
            return new string[]
            {
                "PNG", "JPEG", "BMP", "GIF", "TIFF", "WMF", "EMF"
            };
        }

        /// <summary>
        /// 创建图片缩略图
        /// </summary>
        /// <param name="inputPath">输入图片路径</param>
        /// <param name="outputPath">输出缩略图路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <returns>是否创建成功</returns>
        public bool CreateThumbnail(string inputPath, string outputPath, int maxWidth, int maxHeight)
        {
            try
            {
                using (var originalImage = new Bitmap(inputPath))
                {
                    // 计算缩略图尺寸，保持宽高比
                    double ratioX = (double)maxWidth / originalImage.Width;
                    double ratioY = (double)maxHeight / originalImage.Height;
                    double ratio = Math.Min(ratioX, ratioY);

                    int newWidth = (int)(originalImage.Width * ratio);
                    int newHeight = (int)(originalImage.Height * ratio);

                    using (var thumbnail = new Bitmap(originalImage, newWidth, newHeight))
                    {
                        thumbnail.Save(outputPath, ImageFormat.Png);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
