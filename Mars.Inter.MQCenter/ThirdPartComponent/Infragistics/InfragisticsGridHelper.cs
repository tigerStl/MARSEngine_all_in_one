using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static MarsUFTAddins.IMars.tiger.ReflectorForCSharp;
using Mars.message.Inter.MQCenter;
using System.Windows.Forms;

namespace Mars.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    public class InfragisticsGridHelper
    {
        public const string cnst_sortHeaders = "SORTHEADER;";
        public static bool isSortHeaderModifer(string strPara, ref string modifiedPara)
        {
            message.Inter.MQCenter.simpleLog.MarsLoggerSimple.logBegin("isSortHeaderModifer", $"try to deal|{strPara}");
            strPara = strPara == null ? "" : strPara;
            if (strPara.StartsWith(cnst_sortHeaders, StringComparison.OrdinalIgnoreCase))
            {
                modifiedPara = strPara.Substring(cnst_sortHeaders.Length);
                return true;
            }
            return false;
        }


        /// <summary>
        /// 获取指定列标题对应的 HeaderUIElement。
        /// </summary>
        /// <param name="grid">UltraGrid 对象。</param>
        /// <param name="headerCaption">目标列标题。</param>
        /// <returns>HeaderUIElement 或 null。</returns>
        public object GetColumnHeaderElement(object grid, string headerCaption, ref string strError, ref bool isOk, ref string strAdv, ref string strStack)
        {
            if (grid == null || string.IsNullOrEmpty(headerCaption))
            {
                isOk = false;
                strError = "No columnName is set";
                strAdv = "Please ensure column name is set";
                strStack = Environment.StackTrace;
                return null;                
            }

            // 使用反射获取 DisplayLayout 属性
            var displayLayout = ReflectorForCSharp.GetMember(grid, "DisplayLayout");
            if (displayLayout == null)
            {
                isOk = false;
                strAdv   = "Please ensure column name is set";
                strError = $"can't find |displayLayOut| from type|{grid.GetType().FullName} |{strAdv}";
                strStack = Environment.StackTrace;
                return null;                
            }

            // 获取 Bands 属性并迭代
            var bands = ReflectorForCSharp.GetMember(displayLayout, "Bands");
            
            //var bandsProperty = displayLayout.GetType().GetProperty("Bands", BindingFlags.Instance | BindingFlags.Public);
            //if (bandsProperty == null) throw new InvalidOperationException("无法获取 Bands 属性");

            //var bands = bandsProperty.GetValue(displayLayout);
            //if (bands == null) throw new InvalidOperationException("Bands 属性为空");

            // 遍历 Band 集合
            foreach (var band in (System.Collections.IEnumerable)bands)
            {
                // 获取 Columns 属性
                var columnsProperty = band.GetType().GetProperty("Columns", BindingFlags.Instance | BindingFlags.Public);
                if (columnsProperty == null) continue;

                var columns = columnsProperty.GetValue(band);
                if (columns == null) continue;

                // 遍历 Columns 集合
                foreach (var column in (System.Collections.IEnumerable)columns)
                {
                    // 匹配 Header.Caption
                    var headerProperty = column.GetType().GetProperty("Header", BindingFlags.Instance | BindingFlags.Public);
                    if (headerProperty == null) continue;

                    var header = headerProperty.GetValue(column);
                    if (header == null) continue;

                    var captionProperty = header.GetType().GetProperty("Caption", BindingFlags.Instance | BindingFlags.Public);
                    if (captionProperty == null) continue;

                    var caption = captionProperty.GetValue(header) as string;
                    if (MarsWindowsAPIsExtend.RegularTest(headerCaption, caption)) 
                    //MarsTigerUtility.RegularExpressChecking(headerCaption, caption);
                    //if (caption == headerCaption)
                    {
                        // 获取 HeaderUIElement
                        var getUIElementMethod = header.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "GetUIElement" && m.GetParameters().Length == 0);// GetMethod("GetUIElement", null);// BindingFlags.Instance | BindingFlags.Public);
                        if (getUIElementMethod == null)
                        {
                            
                            strAdv = $"Please ensure the column |{headerCaption}| is visbile";
                            strError = $"can't find GetUIElement from type|{header.GetType().FullName}|in|{grid.GetType().FullName}|{strAdv}";
                            strStack = Environment.StackTrace;
                            isOk = false;
                            return null;
                        }
                        return getUIElementMethod.Invoke(header, null);
                    }
                }

            }
            strAdv = $"Please ensure the column |{headerCaption}| is visbile";
            strError = $"No|{headerCaption} is found or visible|{strAdv}";
            strStack = Environment.StackTrace;
            isOk = false;            
            return null;
        }

        internal bool MimiClickUIElementAtTheCenter(System.Windows.Forms.Control c, object headerUI, ref string strError, ref string strAdv, ref string strStack)
        {
            MarsLoggerSimple.logBegin("MimiClickUIElementAtTheCenter", $"TRY TO OP ON|{headerUI?.GetType().FullName}");
            try
            {
                var objRect = ReflectorForCSharp.GetMember(headerUI, "Rect");
                if (objRect==null)
                {
                    MarsLoggerSimple.Error("MimiClickUIElementAtTheCenter", strError = $"can't find Rect from UIElement|{headerUI?.GetType().FullName}");
                    strAdv = "please ensure the type assemblies has the right version";
                    strStack = Environment.StackTrace;
                    return false;
                }
                if (objRect is Rectangle rect)
                {
                    var clntRct = c.RectangleToScreen(rect);
                    MarsLoggerSimple.Info("MimiClickUIElementAtTheCenter", $"target rect in screen|{clntRct}");
                    MarsWindowsAPIsExtend.MoveMouse(clntRct.X + clntRct.Width / 2, clntRct.Y + clntRct.Height / 2);
                    System.Threading.Thread.Sleep(200);
                    MarsWindowsAPIsExtend.LeftMouseClick(clntRct.X + clntRct.Width / 2, clntRct.Y + clntRct.Height / 2);
                    return true;
                }
                MarsLoggerSimple.Error("MimiClickUIElementAtTheCenter", strError = $"variable Rect is not Rectangle|{objRect?.GetType().FullName}");
                strAdv = "please ensure the type assemblies has the right version after get rect";
                strStack = Environment.StackTrace;
                return false;
            }
            catch (Exception e)
            {
                strError = $"Can't get rect from control|{c?.GetType().FullName}";
                strAdv = "please ensure that assemblies are right.";
                strStack = e.StackTrace;
                MarsLoggerSimple.Error("MimiClickUIElementAtTheCenter", $"{e.Message}|{strError}", e);
                return false;
            }
        }

        /// <summary>
        /// 通过点击heder，实现排序
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara">在使用该方法之前，包括sortheader前缀，这里，已经将前缀删除，因此，就是column</param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        public static string SortHeaderByClick(object oSourceControl, string strCapturePara, string strPegName, string strObjName,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            MarsLoggerSimple.logBegin("SortHeaderByClick", $"[{iMark}] [{strCapturePara}], [{strPegName}]-[{strObjName}]");

            string strColName = strCapturePara;
            try
            {
                if (string.IsNullOrEmpty(strColName))
                {
                    MarsLoggerSimple.Error("SortHeaderByClick", $"[{iMark}] " + (strError = string.Format("Wrong format of captureValue/CaptureAndCompare for grid. columnName is required for sorted mode, but [{0}]", strCapturePara)));
                    strError = "Incorrect format for grid cell location.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct grid location use";
                    isOk = false;
                    return "";
                }

                //获得列名
                string strKey = "";
                int iColIdx = -1;
                bool isOkTmp = false;
                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                bool isNotExists = false;
                int iRowCount = -1;
                object oRows = null;
                long lstart = DateTime.Now.Ticks, lnow = lstart;

                Control tmpc = (System.Windows.Forms.Control)oSourceControl;

                /// 有时候，系统加载较慢，因此这里检测是否已经加载完毕，通过
                while ((iRowCount <= 0) && (((lnow - lstart) / TimeSpan.TicksPerSecond) < 15))
                {
                    isOkTmp = true;

                    System.Threading.Thread.Sleep(50);
#if _NET4

                    ((System.Windows.Forms.Control)oSourceControl).Invoke(//System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                    new Action(() =>
                    {
                        oRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows", ref isNotExists);
                        if (isNotExists)
                        {
                            isOkTmp = false;
                            MarsLoggerSimple.Error("\t", strErrorTmp = "No Rows exists in Grid, Wrong Infragistics version?");
                            strErrorTmp = "Object does not contain rows property";
                            strAdvTmp = "Make sure object is a UltraGrid";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            return;
                        }
                        iRowCount = ReflectorForCSharp.GetMemberByType<int>(oRows, "Count");
                    }
                    ));
                    if (!isOkTmp) break;
                    lnow = DateTime.Now.Ticks;
                }
                isOk = isOkTmp;
                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                List<MARSColumnsInfo> lstCols = new List<MARSColumnsInfo>();
                MarsLoggerSimple.Info("\t", string.Format("Row count:[{0}]", iRowCount));
                if (!isOk) return "";

                /// added o 20241218 to make sure the windows is read by sending wm_paint
                IntPtr messageStub = IntPtr.Zero;
                MarsWindowsAPIs.SendMessage(tmpc.Handle, (int)WM.PAINT, 0, ref messageStub);

                InfragisticsGridHelper gridHelper = new InfragisticsGridHelper();
                object headerUI = null;
#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                new Action(() =>
                {
                    headerUI = gridHelper.GetColumnHeaderElement(tmpc, strColName, ref strErrorTmp, ref isOkTmp, ref strAdvTmp, ref strStackTmp);
                }));
                if ((isOkTmp) && (headerUI == null))
                {
                    strAdvTmp = $"Please ensure|{strColName}| exists and is available in str screen view";
                    strErrorTmp = "can't find available Header's position";
                    strStackTmp = Environment.StackTrace;
                }
                if (!isOkTmp)
                {
                    isOk = isOkTmp;
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                }
                if (!isOk)
                    return "";

                /// 获得rectangle，并且点击
                /// 
                isOk = gridHelper.MimiClickUIElementAtTheCenter(tmpc, headerUI, ref strError, ref strAdv, ref strStackTmp);
                if (isOk)
                {
                    return "SUCCESS";
                }
                return "FAILED";
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("SortHeaderByClick", ex.Message, ex.StackTrace);
                strError = ex.Message;
                strAdv = "Please ensure the object is available and in the screen";
                strStack = ex.StackTrace;
                return "FAILED";
            }
            finally
            {
                MarsLoggerSimple.logEnd("SortHeaderByClick", iMark + "");
            }
        }
    }
}
