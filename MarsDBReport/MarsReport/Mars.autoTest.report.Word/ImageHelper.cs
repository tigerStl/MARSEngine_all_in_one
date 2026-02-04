using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    class ImageHelper
    {
        public static byte bkgRed { get; private set; }
        public static byte bkgGeen { get; private set; }
        public static byte bkgBlue { get; private set; }

        internal static void SaveImage(byte[] pictureBytes, string realFileName)
        {
            // Convert byte array to image
            Image img = ByteArrayToImage(pictureBytes);

            // Convert Image to bitmap
            Bitmap btm = ImageToBitmap(img);
            
            // Continue to work on this late.  Performance is a problem
            /*
            // Derive background color
            Color bkgColor = GetBackgroundColor(btm);

            // Set background color
            SetBackgroundColor(bkgColor);


            // remove white spaces from bitmap
            Bitmap btmWithoutWhiteSpaces = RemoveWhiteSpaces(btm);

            // save the bitmap
            //SaveBitmap(btmWithoutWhiteSpaces, realFileName);
            */

            SaveBitmap(btm, realFileName);
        }

        private static void SetBackgroundColor(Color color)
        {
            var argbarray = BitConverter.GetBytes(color.ToArgb())
                .Reverse()
                .ToArray();

            bkgRed = argbarray[1];
            bkgGeen = argbarray[2];
            bkgBlue = argbarray[3];
        }

        private static void SaveBitmap(Bitmap btmWithoutWhiteSpaces, string fileName)
        {
            SaveImage(btmWithoutWhiteSpaces, fileName);
        }

        private static Bitmap RemoveWhiteSpaces(Bitmap btm)
        {
            Bitmap btmOut = ProcessImage(btm);

            return btmOut;
        }

        private static Bitmap ProcessImage(Bitmap btm)
        {
            int margin = 2;
            Bitmap btmOut = TrimImage(btm, margin);
            return btmOut;
        }

        private static Bitmap TrimImage(Bitmap image, int margin)
        {
            // Make a Bitmap32.
            Bitmap32 bm32 = new Bitmap32(image);
            bm32.LockBitmap();

            // Find the pixel bounds.
            Rectangle src_rect = ImageBounds(bm32);
            bm32.UnlockBitmap();

            // Copy the non-white area.
            int wid = src_rect.Width + 2 * margin;
            int hgt = src_rect.Height + 2 * margin;
            Bitmap bm = new Bitmap(wid, hgt);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.Clear(Color.White);
                Rectangle dest_rect = new Rectangle(
                    margin, margin, src_rect.Width, src_rect.Height);
                gr.DrawImage(image, dest_rect, src_rect, GraphicsUnit.Pixel);
            }

            return bm;
        }

        private static Rectangle ImageBounds(Bitmap32 bm32)
        {
            // ymin.
            int ymin = bm32.Height - 1;
            for (int y = 0; y < bm32.Height; y++)
            {
                if (!RowIsWhite(bm32, y))
                {
                    ymin = y;
                    break;
                }
            }

            // ymax.
            int ymax = 0;
            for (int y = bm32.Height - 1; y >= ymin; y--)
            {
                if (!RowIsWhite(bm32, y))
                {
                    ymax = y;
                    break;
                }
            }

            // xmin.
            int xmin = bm32.Width - 1;
            for (int x = 0; x < bm32.Width; x++)
            {
                if (!ColumnIsWhite(bm32, x))
                {
                    xmin = x;
                    break;
                }
            }

            // xmax.
            int xmax = 0;
            for (int x = bm32.Width - 1; x >= xmin; x--)
            {
                if (!ColumnIsWhite(bm32, x))
                {
                    xmax = x;
                    break;
                }
            }

            // Build the rectangle.
            return new Rectangle(xmin, ymin,
                xmax - xmin + 1, ymax - ymin + 1);
        }

        // Return true if this row is all white.
        private static bool RowIsWhite(Bitmap32 bm32, int y)
        {
            byte r, g, b, a;
            for (int x = 0; x < bm32.Width; x++)
            {
                bm32.GetPixel(x, y, out r, out g, out b, out a);
                // AF
                // if ((r != 255) || (g != 255) || (b != 255)) 
                int threshHoldColor = 215;
                if ((r < threshHoldColor) || (g < threshHoldColor) || (b < threshHoldColor) || 
                    (r == bkgRed && g == bkgGeen && b == bkgBlue))
                    return false;
            }
            return true;
        }

        // Return true if this column is all white.
        private static bool ColumnIsWhite(Bitmap32 bm32, int x)
        {
            byte r, g, b, a;
            for (int y = 0; y < bm32.Height; y++)
            {
                bm32.GetPixel(x, y, out r, out g, out b, out a);
                // AF
                //if ((r != 255) || (g != 255) || (b != 255)) 
                int threshHoldColor = 215;
                if ((r < threshHoldColor) || (g < threshHoldColor) || (b < threshHoldColor) ||
                     (r == bkgRed && g == bkgGeen && b == bkgBlue))
                    return false;
            }
            return true;
        }


        private static Bitmap ImageToBitmap(Image img)
        {
            return new Bitmap(img);
        }

        private static Image ByteArrayToImage(byte[] pictureBytes)
        {
            using (var ms = new MemoryStream(pictureBytes))
            {
                return Image.FromStream(ms);
            }
        }

        public static void SaveImage(Image image, string filename)
        {
            Console.WriteLine("SaveImage file:" + filename);

            try
            {


                string extension = Path.GetExtension(filename);
                switch (extension.ToLower())
                {
                    case ".bmp":
                        image.Save(filename, ImageFormat.Bmp);
                        break;
                    case ".exif":
                        image.Save(filename, ImageFormat.Exif);
                        break;
                    case ".gif":
                        image.Save(filename, ImageFormat.Gif);
                        break;
                    case ".jpg":
                    case ".jpeg":
                        image.Save(filename, ImageFormat.Jpeg);
                        break;
                    case ".png":
                        image.Save(filename, ImageFormat.Png);
                        break;
                    case ".tif":
                    case ".tiff":
                        image.Save(filename, ImageFormat.Tiff);
                        break;
                    default:
                        throw new NotSupportedException(
                            "Unknown file extension " + extension);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static Color GetBackgroundColor(Bitmap bmp)
        {
            Color color = new Color();
            Color bkgColor = new Color();

            int colorMaxCount = 0;


            Dictionary<Color, int> colorCountDict = new Dictionary<Color, int>();
            for (int x = 0; x < bmp.Size.Width; x++)
            {
                for (int y = 0; y < bmp.Size.Height; y++)
                {
                    try
                    {
                        color = bmp.GetPixel(x, y);
                        if (colorCountDict.ContainsKey(color))
                        {
                            colorCountDict[color]++;
                            if (colorCountDict[color] > colorMaxCount)
                            {
                                colorMaxCount++;
                                bkgColor = color;
                            }
                        }
                           
                        else
                            colorCountDict.Add(color, 1);
                                           }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            return bkgColor;
        }
    }
}
