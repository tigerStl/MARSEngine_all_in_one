using System;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarsUnitTest.PicTest
{
    /// <summary>
    /// PicTest类的单元测试
    /// </summary>
    [TestClass]
    public class PicTestClassUnitTest
    {
        private PicTest picTest;
        private string testOutputDir;

        [TestInitialize]
        public void TestInitialize()
        {
            picTest = new PicTest();
            testOutputDir = Path.Combine(Path.GetTempPath(), "PicTestClass_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(testOutputDir);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // 清理测试文件
            try
            {
                if (Directory.Exists(testOutputDir))
                {
                    Directory.Delete(testOutputDir, true);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }

        [TestMethod]
        public void TestCreateTestImage()
        {
            string outputPath = Path.Combine(testOutputDir, "test_image.png");
            bool result = picTest.CreateTestImage(100, 100, Color.Red, outputPath);

            Assert.IsTrue(result, "应该成功创建测试图片");
            Assert.IsTrue(File.Exists(outputPath), "图片文件应该被创建");

            // 验证图片内容
            using (var bitmap = new Bitmap(outputPath))
            {
                Assert.AreEqual(100, bitmap.Width, "图片宽度应该正确");
                Assert.AreEqual(100, bitmap.Height, "图片高度应该正确");
                Assert.AreEqual(Color.Red, bitmap.GetPixel(50, 50), "图片颜色应该正确");
            }
        }

        [TestMethod]
        public void TestCreateTestImage_InvalidParameters()
        {
            string outputPath = Path.Combine(testOutputDir, "invalid.png");
            
            // 测试无效尺寸
            bool result1 = picTest.CreateTestImage(0, 100, Color.Red, outputPath);
            Assert.IsFalse(result1, "零宽度应该返回false");

            bool result2 = picTest.CreateTestImage(100, 0, Color.Red, outputPath);
            Assert.IsFalse(result2, "零高度应该返回false");

            // 测试无效路径
            bool result3 = picTest.CreateTestImage(100, 100, Color.Red, "");
            Assert.IsFalse(result3, "空路径应该返回false");
        }

        [TestMethod]
        public void TestCreatePatternImage()
        {
            string outputPath = Path.Combine(testOutputDir, "pattern_image.png");
            bool result = picTest.CreatePatternImage(200, 200, Color.White, Color.Blue, outputPath);

            Assert.IsTrue(result, "应该成功创建图案图片");
            Assert.IsTrue(File.Exists(outputPath), "图案图片文件应该被创建");

            // 验证图片尺寸
            using (var bitmap = new Bitmap(outputPath))
            {
                Assert.AreEqual(200, bitmap.Width, "图案图片宽度应该正确");
                Assert.AreEqual(200, bitmap.Height, "图案图片高度应该正确");
            }
        }

        [TestMethod]
        public void TestResizeImage()
        {
            // 先创建一个原始图片
            string originalPath = Path.Combine(testOutputDir, "original.png");
            picTest.CreateTestImage(100, 100, Color.Red, originalPath);

            // 调整图片尺寸
            string resizedPath = Path.Combine(testOutputDir, "resized.png");
            bool result = picTest.ResizeImage(originalPath, resizedPath, 50, 50);

            Assert.IsTrue(result, "应该成功调整图片尺寸");
            Assert.IsTrue(File.Exists(resizedPath), "调整后的图片文件应该被创建");

            // 验证调整后的尺寸
            using (var bitmap = new Bitmap(resizedPath))
            {
                Assert.AreEqual(50, bitmap.Width, "调整后的宽度应该正确");
                Assert.AreEqual(50, bitmap.Height, "调整后的高度应该正确");
            }
        }

        [TestMethod]
        public void TestResizeImage_InvalidInput()
        {
            string resizedPath = Path.Combine(testOutputDir, "resized.png");
            
            // 测试不存在的文件
            bool result = picTest.ResizeImage("nonexistent.png", resizedPath, 50, 50);
            Assert.IsFalse(result, "不存在的文件应该返回false");

            // 测试空路径
            bool result2 = picTest.ResizeImage("", resizedPath, 50, 50);
            Assert.IsFalse(result2, "空路径应该返回false");
        }

        [TestMethod]
        public void TestGetPixelData()
        {
            // 创建测试图片
            string imagePath = Path.Combine(testOutputDir, "pixel_test.png");
            picTest.CreateTestImage(10, 10, Color.Red, imagePath);

            // 获取像素数据
            Color[,] pixels = picTest.GetPixelData(imagePath);

            Assert.IsNotNull(pixels, "像素数据不应该为空");
            Assert.AreEqual(10, pixels.GetLength(0), "像素数组宽度应该正确");
            Assert.AreEqual(10, pixels.GetLength(1), "像素数组高度应该正确");

            // 验证像素颜色
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Assert.AreEqual(Color.Red, pixels[x, y], $"像素({x},{y})应该是红色");
                }
            }
        }

        [TestMethod]
        public void TestGetPixelData_InvalidInput()
        {
            // 测试不存在的文件
            Color[,] pixels1 = picTest.GetPixelData("nonexistent.png");
            Assert.IsNull(pixels1, "不存在的文件应该返回null");

            // 测试空路径
            Color[,] pixels2 = picTest.GetPixelData("");
            Assert.IsNull(pixels2, "空路径应该返回null");
        }

        [TestMethod]
        public void TestGetAverageColor()
        {
            // 创建纯色图片
            string redImagePath = Path.Combine(testOutputDir, "red.png");
            picTest.CreateTestImage(100, 100, Color.Red, redImagePath);

            Color avgColor = picTest.GetAverageColor(redImagePath);
            Assert.AreEqual(Color.Red, avgColor, "纯红色图片的平均颜色应该是红色");

            // 创建混合颜色图片
            string mixedImagePath = Path.Combine(testOutputDir, "mixed.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 0, 0, 50, 50);
                bitmap.Save(mixedImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Color mixedAvgColor = picTest.GetAverageColor(mixedImagePath);
            Assert.IsNotNull(mixedAvgColor, "混合颜色图片的平均颜色不应该为空");
        }

        [TestMethod]
        public void TestGetAverageColor_InvalidInput()
        {
            // 测试不存在的文件
            Color avgColor1 = picTest.GetAverageColor("nonexistent.png");
            Assert.AreEqual(Color.Black, avgColor1, "不存在的文件应该返回黑色");

            // 测试空路径
            Color avgColor2 = picTest.GetAverageColor("");
            Assert.AreEqual(Color.Black, avgColor2, "空路径应该返回黑色");
        }

        [TestMethod]
        public void TestIsValidImageFile()
        {
            // 创建有效图片
            string validImagePath = Path.Combine(testOutputDir, "valid.png");
            picTest.CreateTestImage(50, 50, Color.Green, validImagePath);

            bool isValid = picTest.IsValidImageFile(validImagePath);
            Assert.IsTrue(isValid, "有效图片文件应该返回true");

            // 测试不存在的文件
            bool isInvalid1 = picTest.IsValidImageFile("nonexistent.png");
            Assert.IsFalse(isInvalid1, "不存在的文件应该返回false");

            // 测试空路径
            bool isInvalid2 = picTest.IsValidImageFile("");
            Assert.IsFalse(isInvalid2, "空路径应该返回false");

            // 测试非图片文件
            string textFilePath = Path.Combine(testOutputDir, "test.txt");
            File.WriteAllText(textFilePath, "This is not an image");
            bool isInvalid3 = picTest.IsValidImageFile(textFilePath);
            Assert.IsFalse(isInvalid3, "文本文件应该返回false");
        }

        [TestMethod]
        public void TestGetSupportedFormats()
        {
            string[] formats = picTest.GetSupportedFormats();

            Assert.IsNotNull(formats, "支持的格式列表不应该为空");
            Assert.IsTrue(formats.Length > 0, "应该至少支持一种格式");
            Assert.IsTrue(Array.Exists(formats, f => f == "PNG"), "应该支持PNG格式");
            Assert.IsTrue(Array.Exists(formats, f => f == "JPEG"), "应该支持JPEG格式");
        }

        [TestMethod]
        public void TestCreateThumbnail()
        {
            // 创建原始图片
            string originalPath = Path.Combine(testOutputDir, "original_thumb.png");
            picTest.CreateTestImage(200, 200, Color.Blue, originalPath);

            // 创建缩略图
            string thumbnailPath = Path.Combine(testOutputDir, "thumbnail.png");
            bool result = picTest.CreateThumbnail(originalPath, thumbnailPath, 50, 50);

            Assert.IsTrue(result, "应该成功创建缩略图");
            Assert.IsTrue(File.Exists(thumbnailPath), "缩略图文件应该被创建");

            // 验证缩略图尺寸（应该保持宽高比）
            using (var bitmap = new Bitmap(thumbnailPath))
            {
                Assert.AreEqual(50, bitmap.Width, "缩略图宽度应该正确");
                Assert.AreEqual(50, bitmap.Height, "缩略图高度应该正确");
            }
        }

        [TestMethod]
        public void TestCreateThumbnail_InvalidInput()
        {
            string thumbnailPath = Path.Combine(testOutputDir, "thumbnail.png");

            // 测试不存在的文件
            bool result1 = picTest.CreateThumbnail("nonexistent.png", thumbnailPath, 50, 50);
            Assert.IsFalse(result1, "不存在的文件应该返回false");

            // 测试空路径
            bool result2 = picTest.CreateThumbnail("", thumbnailPath, 50, 50);
            Assert.IsFalse(result2, "空路径应该返回false");
        }

        [TestMethod]
        public void TestCreateThumbnail_DifferentAspectRatios()
        {
            // 创建横向图片
            string horizontalPath = Path.Combine(testOutputDir, "horizontal.png");
            picTest.CreateTestImage(200, 100, Color.Yellow, horizontalPath);

            string thumbnailPath = Path.Combine(testOutputDir, "horizontal_thumb.png");
            bool result = picTest.CreateThumbnail(horizontalPath, thumbnailPath, 50, 50);

            Assert.IsTrue(result, "应该成功创建横向图片的缩略图");

            using (var bitmap = new Bitmap(thumbnailPath))
            {
                // 横向图片的缩略图应该保持宽高比
                Assert.IsTrue(bitmap.Width <= 50, "缩略图宽度不应该超过限制");
                Assert.IsTrue(bitmap.Height <= 50, "缩略图高度不应该超过限制");
                Assert.IsTrue(bitmap.Width > bitmap.Height, "横向图片的缩略图应该保持横向");
            }
        }

        [TestMethod]
        public void TestEdgeCases()
        {
            // 测试1x1像素图片
            string tinyImagePath = Path.Combine(testOutputDir, "tiny.png");
            bool result1 = picTest.CreateTestImage(1, 1, Color.Red, tinyImagePath);
            Assert.IsTrue(result1, "应该能创建1x1像素图片");

            // 测试大图片
            string largeImagePath = Path.Combine(testOutputDir, "large.png");
            bool result2 = picTest.CreateTestImage(1000, 1000, Color.Green, largeImagePath);
            Assert.IsTrue(result2, "应该能创建大图片");

            // 测试各种颜色
            Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.White, Color.Black, Color.Transparent };
            for (int i = 0; i < colors.Length; i++)
            {
                string colorImagePath = Path.Combine(testOutputDir, $"color_{i}.png");
                bool result = picTest.CreateTestImage(10, 10, colors[i], colorImagePath);
                Assert.IsTrue(result, $"应该能创建{colors[i]}颜色的图片");
            }
        }
    }
}
