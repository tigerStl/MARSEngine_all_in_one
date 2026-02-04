using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class SnapshotHelper
    {
        internal static bool SnapshotMARSUIObj(AutomationElement targetElement, 
            string pegWindName, string objName, 
            Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.logBegin("SnapshotMARSUIObj", $"{iMark}|snapshot({pegWindName}.{objName},{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}, {strParaMeter}, {strData})");
            if (dealResult==null)
                dealResult = new MARSDealResult();
            try
            {
                if (targetElement == null)
                {
                    strError = "Target element is null";
                    MarsLoggerSimple.Error("SnapshotMARSUIObj", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 获取元素的边界框
                Rect boundingRect = targetElement.Current.BoundingRectangle;
                MarsLoggerSimple.Info("SnapshotMARSUIObj", $"{iMark}|Element bounding rect: X={boundingRect.X}, Y={boundingRect.Y}, W={boundingRect.Width}, H={boundingRect.Height}");

                // 检查边界框是否有效
                if (boundingRect.Width <= 0 || boundingRect.Height <= 0)
                {
                    strError = "Invalid element bounding rectangle";
                    MarsLoggerSimple.Error("SnapshotMARSUIObj", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 创建截图目录
                string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string assemblyDir = Path.GetDirectoryName(assemblyPath);
                string snapshotDir = Path.Combine(assemblyDir, "snapshotimage");
                
                if (!Directory.Exists(snapshotDir))
                {
                    Directory.CreateDirectory(snapshotDir);
                    MarsLoggerSimple.Info("SnapshotMARSUIObj", $"{iMark}|Created snapshot directory: {snapshotDir}");
                }

                // 生成文件名：strData_yyyyMMddhh24mmss.jpg
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName = $"{strData}_{timestamp}.jpg";
                string filePath = Path.Combine(snapshotDir, fileName);

                // 确保坐标在屏幕范围内
                int x = Math.Max(0, (int)boundingRect.X);
                int y = Math.Max(0, (int)boundingRect.Y);
                int width = Math.Max(1, (int)boundingRect.Width);
                int height = Math.Max(1, (int)boundingRect.Height);

#if gdienable
                FlashControlHelper.FlashControlByXORDrawing(targetElement);
                //if( c.CanFocus || c.CanSelect)
#endif

                MarsLoggerSimple.Info("SnapshotMARSUIObj", $"{iMark}|Capturing screen area: X={x}, Y={y}, W={width}, H={height}");

                // 捕获屏幕图像
                using (Bitmap screenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                    }

                    // 保存为JPG文件
                    screenshot.Save(filePath, ImageFormat.Jpeg);
                    MarsLoggerSimple.Info("SnapshotMARSUIObj", $"{iMark}|Screenshot saved to: {filePath}");
                }

                // 设置返回结果
                dealResult.ReturnedData = filePath;
                dealResult.snapshotFilePath = filePath;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ErrorMessage = "";
                dealResult.AckTime = DateTime.Now;

                MarsLoggerSimple.Info("SnapshotMARSUIObj", $"{iMark}|Snapshot completed successfully: {fileName}");
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                MarsLoggerSimple.Error("SnapshotMARSUIObj", $"{iMark}|Error: {strError}", e);
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("SnapshotMARSUIObj", $"{iMark}|{dealResult.snapshotFilePath}");
            }
        }
    }
}
