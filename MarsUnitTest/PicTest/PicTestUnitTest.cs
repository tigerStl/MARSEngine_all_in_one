using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarsUnitTest.PicTest
{
    /// <summary>
    /// PicTest的单元测试类
    /// </summary>
    [TestClass]
    public class PicTestUnitTest
    {
        private PicCompareTest picCompareTest;
        private string testImagePath1;
        private string testImagePath2;
        private string testOutputDir;

        [TestInitialize]
        public void TestInitialize()
        {
            picCompareTest = new PicCompareTest();
            testOutputDir = Path.Combine(Path.GetTempPath(), "PicTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(testOutputDir);

            // 创建测试图片
            CreateTestImages();
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

        /// <summary>
        /// 创建测试用的图片文件
        /// </summary>
        private void CreateTestImages()
        {
            // 创建第一张测试图片（红色矩形）
            testImagePath1 = Path.Combine(testOutputDir, "test1.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 20, 20, 60, 60);
                bitmap.Save(testImagePath1, ImageFormat.Png);
            }

            // 创建第二张测试图片（与第一张相同）
            testImagePath2 = Path.Combine(testOutputDir, "test2.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 20, 20, 60, 60);
                bitmap.Save(testImagePath2, ImageFormat.Png);
            }
        }

        [TestMethod]
        public void TestCompareImagesExact_SameImages()
        {
            // 测试相同的图片
            bool result = picCompareTest.CompareImagesExact(testImagePath1, testImagePath2);
            Assert.IsTrue(result, "相同的图片应该返回true");
        }

        [TestMethod]
        public void TestCompareImagesExact_DifferentImages()
        {
            // 创建不同的图片
            string differentImagePath = Path.Combine(testOutputDir, "different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Green);
                graphics.FillRectangle(Brushes.Yellow, 20, 20, 60, 60);
                bitmap.Save(differentImagePath, ImageFormat.Png);
            }

            bool result = picCompareTest.CompareImagesExact(testImagePath1, differentImagePath);
            Assert.IsFalse(result, "不同的图片应该返回false");
        }

        [TestMethod]
        public void TestCompareImagesExact_InvalidPaths()
        {
            // 测试空路径
            Assert.ThrowsException<ArgumentException>(() => 
                picCompareTest.CompareImagesExact("", testImagePath2));

            Assert.ThrowsException<ArgumentException>(() => 
                picCompareTest.CompareImagesExact(testImagePath1, null));

            // 测试不存在的文件
            Assert.ThrowsException<FileNotFoundException>(() => 
                picCompareTest.CompareImagesExact("nonexistent1.png", "nonexistent2.png"));
        }

        [TestMethod]
        public void TestCalculateSimilarity_SameImages()
        {
            double similarity = picCompareTest.CalculateSimilarity(testImagePath1, testImagePath2);
            Assert.AreEqual(100.0, similarity, 0.01, "相同图片的相似度应该是100%");
        }

        [TestMethod]
        public void TestCalculateSimilarity_DifferentImages()
        {
            // 创建部分不同的图片
            string partialDifferentPath = Path.Combine(testOutputDir, "partial_different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Green, 20, 20, 60, 60); // 改变颜色
                bitmap.Save(partialDifferentPath, ImageFormat.Png);
            }

            double similarity = picCompareTest.CalculateSimilarity(testImagePath1, partialDifferentPath);
            Assert.IsTrue(similarity > 0 && similarity < 100, "部分不同的图片相似度应该在0-100之间");
        }

        [TestMethod]
        public void TestCalculateSimilarity_DifferentSizes()
        {
            // 创建不同尺寸的图片
            string differentSizePath = Path.Combine(testOutputDir, "different_size.png");
            using (var bitmap = new Bitmap(50, 50))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 10, 10, 30, 30);
                bitmap.Save(differentSizePath, ImageFormat.Png);
            }

            double similarity = picCompareTest.CalculateSimilarity(testImagePath1, differentSizePath);
            Assert.AreEqual(0.0, similarity, 0.01, "不同尺寸的图片相似度应该是0%");
        }

        [TestMethod]
        public void TestCompareImagesSimilar_WithThreshold()
        {
            // 测试相似度比较
            bool result = picCompareTest.CompareImagesSimilar(testImagePath1, testImagePath2, 95.0);
            Assert.IsTrue(result, "相同图片应该满足95%的相似度阈值");

            // 创建完全不同的图片
            string completelyDifferentPath = Path.Combine(testOutputDir, "completely_different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                bitmap.Save(completelyDifferentPath, ImageFormat.Png);
            }

            bool result2 = picCompareTest.CompareImagesSimilar(testImagePath1, completelyDifferentPath, 95.0);
            Assert.IsFalse(result2, "完全不同的图片不应该满足95%的相似度阈值");
        }

        [TestMethod]
        public void TestSaveComparisonResult()
        {
            string outputPath = Path.Combine(testOutputDir, "comparison_result.txt");
            string result = picCompareTest.SaveComparisonResult(testImagePath1, testImagePath2, outputPath);

            Assert.IsNotNull(result, "比较结果不应该为空");
            Assert.IsTrue(File.Exists(outputPath), "结果文件应该被创建");
            Assert.IsTrue(result.Contains("完全匹配: 是"), "结果应该包含完全匹配信息");
            Assert.IsTrue(result.Contains("相似度: 100%"), "结果应该包含相似度信息");
        }

        [TestMethod]
        public void TestCreateDifferenceImage()
        {
            // 创建略有不同的图片
            string slightlyDifferentPath = Path.Combine(testOutputDir, "slightly_different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 20, 20, 60, 60);
                graphics.FillRectangle(Brushes.Yellow, 40, 40, 20, 20); // 添加一个黄色矩形
                bitmap.Save(slightlyDifferentPath, ImageFormat.Png);
            }

            string diffImagePath = Path.Combine(testOutputDir, "difference.png");
            bool result = picCompareTest.CreateDifferenceImage(testImagePath1, slightlyDifferentPath, diffImagePath);

            Assert.IsTrue(result, "应该成功创建差异图");
            Assert.IsTrue(File.Exists(diffImagePath), "差异图文件应该被创建");
        }

        [TestMethod]
        public void TestGetImageInfo()
        {
            string info = picCompareTest.GetImageInfo(testImagePath1);
            
            Assert.IsNotNull(info, "图片信息不应该为空");
            Assert.IsTrue(info.Contains("100x100"), "应该包含图片尺寸信息");
            Assert.IsTrue(info.Contains("PNG"), "应该包含图片格式信息");
            Assert.IsTrue(info.Contains("字节"), "应该包含文件大小信息");
        }

        [TestMethod]
        public void TestGetImageInfo_InvalidPath()
        {
            string info = picCompareTest.GetImageInfo("nonexistent.png");
            Assert.AreEqual("图片文件不存在或路径为空", info, "不存在的文件应该返回错误信息");
        }

        [TestMethod]
        public void TestPerformance_LargeImages()
        {
            // 创建较大的测试图片
            string largeImagePath1 = Path.Combine(testOutputDir, "large1.png");
            string largeImagePath2 = Path.Combine(testOutputDir, "large2.png");

            using (var bitmap1 = new Bitmap(500, 500))
            using (var bitmap2 = new Bitmap(500, 500))
            using (var graphics1 = Graphics.FromImage(bitmap1))
            using (var graphics2 = Graphics.FromImage(bitmap2))
            {
                graphics1.Clear(Color.Red);
                graphics2.Clear(Color.Red);
                
                // 添加一些随机图案
                Random rand = new Random(12345);
                for (int i = 0; i < 100; i++)
                {
                    int x = rand.Next(0, 450);
                    int y = rand.Next(0, 450);
                    graphics1.FillRectangle(Brushes.Blue, x, y, 10, 10);
                    graphics2.FillRectangle(Brushes.Blue, x, y, 10, 10);
                }
                
                bitmap1.Save(largeImagePath1, ImageFormat.Png);
                bitmap2.Save(largeImagePath2, ImageFormat.Png);
            }

            // 测试大图片的比较性能
            var startTime = DateTime.Now;
            bool result = picCompareTest.CompareImagesExact(largeImagePath1, largeImagePath2);
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Assert.IsTrue(result, "相同的大图片应该返回true");
            Assert.IsTrue(duration.TotalSeconds < 10, "大图片比较应该在10秒内完成");
        }

        [TestMethod]
        public void TestEdgeCases()
        {
            // 测试1x1像素的图片
            string tinyImagePath1 = Path.Combine(testOutputDir, "tiny1.png");
            string tinyImagePath2 = Path.Combine(testOutputDir, "tiny2.png");

            using (var bitmap1 = new Bitmap(1, 1))
            using (var bitmap2 = new Bitmap(1, 1))
            {
                bitmap1.SetPixel(0, 0, Color.Red);
                bitmap2.SetPixel(0, 0, Color.Red);
                bitmap1.Save(tinyImagePath1, ImageFormat.Png);
                bitmap2.Save(tinyImagePath2, ImageFormat.Png);
            }

            bool result = picCompareTest.CompareImagesExact(tinyImagePath1, tinyImagePath2);
            Assert.IsTrue(result, "1x1像素的相同图片应该返回true");

            // 测试不同颜色的1x1图片
            using (var bitmap3 = new Bitmap(1, 1))
            {
                bitmap3.SetPixel(0, 0, Color.Blue);
                string tinyImagePath3 = Path.Combine(testOutputDir, "tiny3.png");
                bitmap3.Save(tinyImagePath3, ImageFormat.Png);

                bool result2 = picCompareTest.CompareImagesExact(tinyImagePath1, tinyImagePath3);
                Assert.IsFalse(result2, "不同颜色的1x1像素图片应该返回false");
            }
        }

        [TestMethod]
        public void TestCompareAndHighlight_SameImages()
        {
            // 测试相同图片的高亮比较
            try
            {
                picCompareTest.CompareAndHighlight(testImagePath1, testImagePath2);
                // 如果方法执行成功且没有异常，则认为测试通过
                Assert.IsTrue(true, "相同图片的高亮比较应该正常执行");
            }
            catch (Exception ex)
            {
                Assert.Fail($"相同图片的高亮比较不应该抛出异常: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestCompareAndHighlight_DifferentImages()
        {
            // 创建有明显差异的图片
            string differentImagePath = Path.Combine(testOutputDir, "highlight_different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 20, 20, 60, 60);
                graphics.FillRectangle(Brushes.Yellow, 40, 40, 20, 20); // 添加一个黄色矩形
                bitmap.Save(differentImagePath, ImageFormat.Png);
            }

            try
            {
                picCompareTest.CompareAndHighlight(testImagePath1, differentImagePath);
                // 如果方法执行成功且没有异常，则认为测试通过
                Assert.IsTrue(true, "不同图片的高亮比较应该正常执行");
            }
            catch (Exception ex)
            {
                Assert.Fail($"不同图片的高亮比较不应该抛出异常: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestCompareAndHighlight_WithTolerance()
        {
            // 创建略有差异的图片
            string slightlyDifferentPath = Path.Combine(testOutputDir, "highlight_slightly_different.png");
            using (var bitmap = new Bitmap(100, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                graphics.FillRectangle(Brushes.Blue, 20, 20, 60, 60);
                // 添加一些细微的颜色差异
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 1, 0)), 30, 30, 10, 10);
                bitmap.Save(slightlyDifferentPath, ImageFormat.Png);
            }

            try
            {
                // 使用容忍度进行高亮比较
                picCompareTest.CompareAndHighlight(testImagePath1, slightlyDifferentPath, tolerance: 5);
                Assert.IsTrue(true, "使用容忍度的高亮比较应该正常执行");
            }
            catch (Exception ex)
            {
                Assert.Fail($"使用容忍度的高亮比较不应该抛出异常: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestCompareAndHighlight_InvalidInput()
        {
            // 测试空路径
            Assert.ThrowsException<ArgumentException>(() => 
                picCompareTest.CompareAndHighlight("", testImagePath2));

            Assert.ThrowsException<ArgumentException>(() => 
                picCompareTest.CompareAndHighlight(testImagePath1, null));

            // 测试不存在的文件
            Assert.ThrowsException<FileNotFoundException>(() => 
                picCompareTest.CompareAndHighlight("nonexistent1.png", "nonexistent2.png"));
        }
    }
}
