using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MarsUnitTest.PicTest
{
    /// <summary>
    /// 图片比较测试类，提供图片对比、相似度计算等功能
    /// </summary>
    public class PicCompareTest
    {
        /// <summary>
        /// 比较两张图片是否完全相同（像素级比较）
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <returns>如果图片完全相同返回true，否则返回false</returns>
        public bool CompareImagesExact(string imagePath1, string imagePath2)
        {
            if (string.IsNullOrEmpty(imagePath1) || string.IsNullOrEmpty(imagePath2))
            {
                throw new ArgumentException("图片路径不能为空");
            }

            if (!File.Exists(imagePath1) || !File.Exists(imagePath2))
            {
                throw new FileNotFoundException("图片文件不存在");
            }

            using (var img1 = new Bitmap(imagePath1))
            using (var img2 = new Bitmap(imagePath2))
            {
                // 检查尺寸是否相同
                if (img1.Width != img2.Width || img1.Height != img2.Height)
                {
                    return false;
                }

                // 像素级比较
                for (int x = 0; x < img1.Width; x++)
                {
                    for (int y = 0; y < img1.Height; y++)
                    {
                        if (img1.GetPixel(x, y) != img2.GetPixel(x, y))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 计算两张图片的相似度（基于像素差异）
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <returns>相似度百分比（0-100）</returns>
        public double CalculateSimilarity(string imagePath1, string imagePath2)
        {
            if (string.IsNullOrEmpty(imagePath1) || string.IsNullOrEmpty(imagePath2))
            {
                throw new ArgumentException("图片路径不能为空");
            }

            if (!File.Exists(imagePath1) || !File.Exists(imagePath2))
            {
                throw new FileNotFoundException("图片文件不存在");
            }

            using (var img1 = new Bitmap(imagePath1))
            using (var img2 = new Bitmap(imagePath2))
            {
                // 如果尺寸不同，返回0相似度
                if (img1.Width != img2.Width || img1.Height != img2.Height)
                {
                    return 0.0;
                }

                int totalPixels = img1.Width * img1.Height;
                int differentPixels = 0;

                // 计算不同像素的数量
                for (int x = 0; x < img1.Width; x++)
                {
                    for (int y = 0; y < img1.Height; y++)
                    {
                        if (img1.GetPixel(x, y) != img2.GetPixel(x, y))
                        {
                            differentPixels++;
                        }
                    }
                }

                // 计算相似度百分比
                double similarity = ((double)(totalPixels - differentPixels) / totalPixels) * 100;
                return Math.Round(similarity, 2);
            }
        }

        /// <summary>
        /// 比较两张图片是否相似（基于阈值）
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <param name="threshold">相似度阈值（0-100）</param>
        /// <returns>如果相似度大于等于阈值返回true，否则返回false</returns>
        public bool CompareImagesSimilar(string imagePath1, string imagePath2, double threshold = 95.0)
        {
            double similarity = CalculateSimilarity(imagePath1, imagePath2);
            return similarity >= threshold;
        }

        /// <summary>
        /// 保存图片比较结果到文件
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <returns>比较结果信息</returns>
        public string SaveComparisonResult(string imagePath1, string imagePath2, string outputPath)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine($"图片比较结果 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine($"图片1: {imagePath1}");
            result.AppendLine($"图片2: {imagePath2}");
            result.AppendLine();

            try
            {
                bool isExact = CompareImagesExact(imagePath1, imagePath2);
                double similarity = CalculateSimilarity(imagePath1, imagePath2);

                result.AppendLine($"完全匹配: {(isExact ? "是" : "否")}");
                result.AppendLine($"相似度: {similarity}%");

                // 获取图片信息
                using (var img1 = new Bitmap(imagePath1))
                using (var img2 = new Bitmap(imagePath2))
                {
                    result.AppendLine();
                    result.AppendLine("图片1信息:");
                    result.AppendLine($"  尺寸: {img1.Width}x{img1.Height}");
                    result.AppendLine($"  格式: {img1.PixelFormat}");
                    
                    result.AppendLine();
                    result.AppendLine("图片2信息:");
                    result.AppendLine($"  尺寸: {img2.Width}x{img2.Height}");
                    result.AppendLine($"  格式: {img2.PixelFormat}");
                }

                // 保存结果到文件
                File.WriteAllText(outputPath, result.ToString(), System.Text.Encoding.UTF8);
                result.AppendLine();
                result.AppendLine($"结果已保存到: {outputPath}");
            }
            catch (Exception ex)
            {
                result.AppendLine($"比较过程中发生错误: {ex.Message}");
            }

            return result.ToString();
        }

        /// <summary>
        /// 创建图片差异图（高亮显示不同的像素）
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <param name="outputPath">差异图输出路径</param>
        /// <returns>是否成功创建差异图</returns>
        public bool CreateDifferenceImage(string imagePath1, string imagePath2, string outputPath)
        {
            try
            {
                using (var img1 = new Bitmap(imagePath1))
                using (var img2 = new Bitmap(imagePath2))
                {
                    // 检查尺寸是否相同
                    if (img1.Width != img2.Width || img1.Height != img2.Height)
                    {
                        return false;
                    }

                    using (var diffImg = new Bitmap(img1.Width, img1.Height))
                    {
                        for (int x = 0; x < img1.Width; x++)
                        {
                            for (int y = 0; y < img1.Height; y++)
                            {
                                Color pixel1 = img1.GetPixel(x, y);
                                Color pixel2 = img2.GetPixel(x, y);

                                if (pixel1 == pixel2)
                                {
                                    // 相同像素显示为灰色
                                    diffImg.SetPixel(x, y, Color.Gray);
                                }
                                else
                                {
                                    // 不同像素显示为红色
                                    diffImg.SetPixel(x, y, Color.Red);
                                }
                            }
                        }

                        diffImg.Save(outputPath, ImageFormat.Png);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取图片的基本信息
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>图片信息字符串</returns>
        public string GetImageInfo(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return "图片文件不存在或路径为空";
            }

            try
            {
                using (var img = new Bitmap(imagePath))
                {
                    var info = new System.Text.StringBuilder();
                    info.AppendLine($"图片路径: {imagePath}");
                    info.AppendLine($"文件名: {Path.GetFileName(imagePath)}");
                    info.AppendLine($"文件大小: {new FileInfo(imagePath).Length} 字节");
                    info.AppendLine($"尺寸: {img.Width}x{img.Height}");
                    info.AppendLine($"像素格式: {img.PixelFormat}");
                    info.AppendLine($"水平分辨率: {img.HorizontalResolution} DPI");
                    info.AppendLine($"垂直分辨率: {img.VerticalResolution} DPI");
                    
                    return info.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"获取图片信息时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 比较两个图片并将不同的地方用矩形标识出来，在窗体中展示
        /// </summary>
        /// <param name="imagePath1">第一张图片路径</param>
        /// <param name="imagePath2">第二张图片路径</param>
        /// <param name="tolerance">像素差异容忍度（0-255）</param>
        /// <param name="minRectangleSize">最小矩形尺寸</param>
        public void CompareAndHighlight(string imagePath1, string imagePath2, int tolerance = 0, int minRectangleSize = 5)
        {
            if (string.IsNullOrEmpty(imagePath1) || string.IsNullOrEmpty(imagePath2))
            {
                throw new ArgumentException("图片路径不能为空");
            }

            if (!File.Exists(imagePath1) || !File.Exists(imagePath2))
            {
                throw new FileNotFoundException("图片文件不存在");
            }

            try
            {
                using (var img1 = new Bitmap(imagePath1))
                using (var img2 = new Bitmap(imagePath2))
                {
                    // 检查尺寸是否相同
                    if (img1.Width != img2.Width || img1.Height != img2.Height)
                    {
                        MessageBox.Show($"图片尺寸不匹配！\n图片1: {img1.Width}x{img1.Height}\n图片2: {img2.Width}x{img2.Height}", 
                            "尺寸不匹配", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 查找不同的区域
                    var differentRegions = FindDifferentRegions(img1, img2, tolerance, minRectangleSize);
                    
                    // 创建并显示比较窗体
                    var comparisonForm = new ImageComparisonForm(img1, img2, differentRegions, imagePath1, imagePath2);
                    comparisonForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"比较图片时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 查找图片中不同的区域
        /// </summary>
        /// <param name="img1">第一张图片</param>
        /// <param name="img2">第二张图片</param>
        /// <param name="tolerance">像素差异容忍度</param>
        /// <param name="minRectangleSize">最小矩形尺寸</param>
        /// <returns>不同区域的矩形列表</returns>
        private List<Rectangle> FindDifferentRegions(Bitmap img1, Bitmap img2, int tolerance, int minRectangleSize)
        {
            var differentPixels = new bool[img1.Width, img1.Height];
            var regions = new List<Rectangle>();

            // 标记不同的像素
            for (int x = 0; x < img1.Width; x++)
            {
                for (int y = 0; y < img1.Height; y++)
                {
                    Color pixel1 = img1.GetPixel(x, y);
                    Color pixel2 = img2.GetPixel(x, y);
                    
                    if (IsPixelDifferent(pixel1, pixel2, tolerance))
                    {
                        differentPixels[x, y] = true;
                    }
                }
            }

            // 查找连续的矩形区域
            var visited = new bool[img1.Width, img1.Height];
            
            for (int x = 0; x < img1.Width; x++)
            {
                for (int y = 0; y < img1.Height; y++)
                {
                    if (differentPixels[x, y] && !visited[x, y])
                    {
                        var region = FindLargestRectangle(differentPixels, visited, x, y, img1.Width, img1.Height);
                        if (region.Width >= minRectangleSize && region.Height >= minRectangleSize)
                        {
                            regions.Add(region);
                        }
                    }
                }
            }

            return regions;
        }

        /// <summary>
        /// 检查两个像素是否不同
        /// </summary>
        /// <param name="pixel1">像素1</param>
        /// <param name="pixel2">像素2</param>
        /// <param name="tolerance">容忍度</param>
        /// <returns>是否不同</returns>
        private bool IsPixelDifferent(Color pixel1, Color pixel2, int tolerance)
        {
            return Math.Abs(pixel1.R - pixel2.R) > tolerance ||
                   Math.Abs(pixel1.G - pixel2.G) > tolerance ||
                   Math.Abs(pixel1.B - pixel2.B) > tolerance ||
                   Math.Abs(pixel1.A - pixel2.A) > tolerance;
        }

        /// <summary>
        /// 查找最大的矩形区域
        /// </summary>
        /// <param name="differentPixels">不同像素标记</param>
        /// <param name="visited">访问标记</param>
        /// <param name="startX">起始X坐标</param>
        /// <param name="startY">起始Y坐标</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>矩形区域</returns>
        private Rectangle FindLargestRectangle(bool[,] differentPixels, bool[,] visited, int startX, int startY, int width, int height)
        {
            int maxWidth = 0;
            int maxHeight = 0;
            int currentY = startY;

            // 找到最大宽度
            while (currentY < height && differentPixels[startX, currentY] && !visited[startX, currentY])
            {
                int currentWidth = 0;
                int currentX = startX;
                
                while (currentX < width && differentPixels[currentX, currentY] && !visited[currentX, currentY])
                {
                    currentWidth++;
                    currentX++;
                }
                
                maxWidth = Math.Max(maxWidth, currentWidth);
                maxHeight++;
                currentY++;
            }

            // 标记为已访问
            for (int y = startY; y < startY + maxHeight; y++)
            {
                for (int x = startX; x < startX + maxWidth; x++)
                {
                    if (x < width && y < height)
                    {
                        visited[x, y] = true;
                    }
                }
            }

            return new Rectangle(startX, startY, maxWidth, maxHeight);
        }
    }

    /// <summary>
    /// 图片比较展示窗体
    /// </summary>
    public class ImageComparisonForm : Form
    {
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private Label label1;
        private Label label2;
        private Label label3;
        private ListBox regionsListBox;
        private Button closeButton;
        private List<Rectangle> differentRegions;
        private Bitmap originalImage1;
        private Bitmap originalImage2;

        public ImageComparisonForm(Bitmap img1, Bitmap img2, List<Rectangle> regions, string path1, string path2)
        {
            this.originalImage1 = new Bitmap(img1);
            this.originalImage2 = new Bitmap(img2);
            this.differentRegions = regions;
            
            InitializeComponent();
            LoadImages();
            UpdateRegionList();
            
            this.Text = "图片比较结果";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            this.SuspendLayout();

            // 创建标签
            label1 = new Label
            {
                Text = "原始图片1",
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            label2 = new Label
            {
                Text = "原始图片2",
                Location = new Point(220, 10),
                Size = new Size(200, 20),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            label3 = new Label
            {
                Text = "差异高亮图",
                Location = new Point(430, 10),
                Size = new Size(200, 20),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            // 创建图片框
            pictureBox1 = new PictureBox
            {
                Location = new Point(10, 35),
                Size = new Size(200, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            pictureBox2 = new PictureBox
            {
                Location = new Point(220, 35),
                Size = new Size(200, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            pictureBox3 = new PictureBox
            {
                Location = new Point(430, 35),
                Size = new Size(200, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 创建区域列表
            regionsListBox = new ListBox
            {
                Location = new Point(10, 250),
                Size = new Size(620, 200),
                Font = new Font("Consolas", 9F)
            };

            // 创建关闭按钮
            closeButton = new Button
            {
                Text = "关闭",
                Location = new Point(560, 460),
                Size = new Size(70, 30),
                DialogResult = DialogResult.OK
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] 
            { 
                label1, label2, label3, 
                pictureBox1, pictureBox2, pictureBox3, 
                regionsListBox, closeButton 
            });

            this.ResumeLayout(false);
        }

        private void LoadImages()
        {
            // 加载原始图片
            pictureBox1.Image = new Bitmap(originalImage1);
            pictureBox2.Image = new Bitmap(originalImage2);

            // 创建差异高亮图
            var highlightImage = CreateHighlightImage();
            pictureBox3.Image = highlightImage;
        }

        private Bitmap CreateHighlightImage()
        {
            var result = new Bitmap(originalImage1);
            using (var graphics = Graphics.FromImage(result))
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    foreach (var region in differentRegions)
                    {
                        graphics.DrawRectangle(pen, region);
                    }
                }
            }
            return result;
        }

        private void UpdateRegionList()
        {
            regionsListBox.Items.Clear();
            regionsListBox.Items.Add($"发现 {differentRegions.Count} 个不同区域:");
            regionsListBox.Items.Add("");

            for (int i = 0; i < differentRegions.Count; i++)
            {
                var region = differentRegions[i];
                regionsListBox.Items.Add($"区域 {i + 1}: X={region.X}, Y={region.Y}, W={region.Width}, H={region.Height}");
            }

            if (differentRegions.Count == 0)
            {
                regionsListBox.Items.Add("未发现不同区域");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                originalImage1?.Dispose();
                originalImage2?.Dispose();
                pictureBox1?.Image?.Dispose();
                pictureBox2?.Image?.Dispose();
                pictureBox3?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
