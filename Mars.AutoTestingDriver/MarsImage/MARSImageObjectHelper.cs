using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mars.message.DataLayer;
using Newtonsoft.Json;
using Route2NSEx.src.Marquis.systemUtil;
using OpenCvSharp;
using System.Drawing;
using OpenCvSharp.Extensions;
using System.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.MarsImage
{
    /// <summary>
    /// Helper class for handling image object operations via REST API
    /// </summary>
    public class MARSImageObjectHelper : IDisposable
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MARSImageObjectHelper));
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        private static string? targetPath = null;
        public static string TargetPath
        {
            get
            {
                if (targetPath == null)
                {
                    string strTagetPath = typeof(MARSImageObjectHelper).Assembly.Location;
                    strTagetPath = System.IO.Path.GetDirectoryName(strTagetPath);
                    strTagetPath = System.IO.Path.Combine(strTagetPath, "ImageObjectFiles");
                    if (!System.IO.Directory.Exists(strTagetPath))
                    {
                        System.IO.Directory.CreateDirectory(strTagetPath);
                    }
                    targetPath = strTagetPath;
                    return targetPath;
                } else
                    return targetPath;
            }
        }

        public MARSImageObjectHelper(string baseUrl = "http://localhost:5000")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the object image path from the REST API
        /// </summary>
        /// <param name="objectId">The object ID</param>
        /// <param name="objectName">The object name</param>
        /// <param name="applicationId">The application ID</param>
        /// <returns>JsonResult containing the image path information</returns>
        public bool GetObjectImagePath(string strDbIdx, long objectId, string objectName, long applicationId,
            ref string strFileWithPath,
            ref string strError)
        {
            Logger.logBegin("GetObjectImagePath", $"objectId: {objectId}, objectName: {objectName}, applicationId: {applicationId}");

            try
            {
                MarsRESTfulApiClient client = new MarsRESTfulApiClient(strDbIdx);
                //bool isOk = client.GetImageObjectById(objectId, objectName, applicationId, TargetPath, ref strError, ref strFileWithPath);
                bool isOk = false;
                if (!isOk)
                {
                    Logger.Error("GetObjectImagePath", strError = $"Failed to get image object: {strError}");
                    return false;
                }
                else
                {
                    Logger.logEnd("GetObjectImagePath", $"Successfully retrieved image path: {strFileWithPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetObjectImagePath", $"Exception occurred: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public static System.Drawing.Bitmap CaptureScreen()
        {
            System.Drawing.Rectangle rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(rect.Width, rect.Height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
            }
            return bmp;
        }
        /// <summary>
        /// 在屏幕图像中查找目标图像，返回位置(使用opensv)
        /// </summary>
        /// <param name="bmpScreen"></param>
        /// <param name="bmpTarget"></param>
        /// <param name="threshold">临界点</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>


        internal static System.Drawing.Rectangle? FindImage(Bitmap bmpScreen, string imageFileName, double threshold,
            ref string strError)
        {
            // 转换 Bitmap 为 Mat
            // 3. 加载模板图像
            var screenMat = BitmapConverter.ToMat(bmpScreen);
            var templateMat = Cv2.ImDecode(System.IO.File.ReadAllBytes(imageFileName), ImreadModes.Color);
            if (templateMat.Empty() || screenMat.Empty())
            {
                strError = $"can't load image: {imageFileName}";
                Logger.Error("FindAndClickOnScreen", strError);
                //Console.WriteLine($"无法加载图像: {templatePath}");
                return null;
            }

            if (screenMat.Channels() != templateMat.Channels())
            {
                Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
                Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2BGR);
            }

            // 4. 模板匹配
            using var result = new Mat();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            Console.WriteLine($"pre：{maxVal:F4}");
            Logger.Info("FindImage", $"precision：{maxVal:F4}");
            if (maxVal >= threshold)
            {
                return new System.Drawing.Rectangle()
                {
                    X = maxLoc.X,
                    Y = maxLoc.Y,
                    Width = templateMat.Width,
                    Height = templateMat.Height
                };
                #region 参考用代码
                //// 加上屏幕偏移，得到全局坐标
                //int clickX = screen.Bounds.Left + maxLoc.X + templateMat.Width / 2;
                //int clickY = screen.Bounds.Top + maxLoc.Y + templateMat.Height / 2;

                //rect = new Rectangle(maxLoc.X, maxLoc.Y, templateMat.Width, templateMat.Height);

                //// 5. 点击目标位置
                //SetCursorPos(clickX, clickY);
                //Thread.Sleep(100);
                //if (string.IsNullOrEmpty(strParameter))
                //{
                //    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 0, 0);
                //}
                //else
                //{
                //    logger.Info("FindAndClickOnScreen", $"{strParameter}");
                //    if ((cnst_mouse_action_double_click.Equals(strParameter, StringComparison.OrdinalIgnoreCase))
                //        || (cnst_mouse_action_dbl_click.Equals(strParameter, StringComparison.OrdinalIgnoreCase)))
                //    {
                //        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 0, 0);
                //        Thread.Sleep(100);
                //        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 0, 0);
                //    }
                //    else if (cnst_mouse_action_right_click.Equals(strParameter, StringComparison.OrdinalIgnoreCase))
                //    {
                //        mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, (uint)clickX, (uint)clickY, 0, 0);
                //    }
                //    else
                //    {
                //        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 0, 0);
                //    }
                //}
                //Thread.Sleep(1000); // 等待1秒，确保点击生效
                ////Console.WriteLine($"点击位置：({clickX}, {clickY})");
                //return true;
                #endregion
            }
            return null;
        }
    }
}

