using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MarsTestFrame.systemUtil
{
    public class TigerMarsUtil
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TigerMarsUtil));

        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int flags);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        public static string GetPathWithoutFileName(string strFileWithPath)
        {
            Logger.logBegin("GetPathWithoutFileName");
            try
            {
                if (strFileWithPath == null) return null;

                int iLastPos = strFileWithPath.LastIndexOf("\\");
                if (iLastPos == -1)
                {
                    return null;
                }

                return strFileWithPath.Substring(0, iLastPos);

            }
            finally
            {
                Logger.logEnd("GetPathWithoutFileName");

            }
        }

        public static string GetParameter(string strParaName, string strValue)
        {
            return string.Format(" ,[{0}={1}] ", strParaName, strValue);
        }

        public static string GetParameter(string[] arrParaName, string[] strValues)
        {
            string strFormat = "";
            int iMaxLen = arrParaName == null ? -1 : arrParaName.Length;
            iMaxLen = Math.Max(iMaxLen, strValues == null ? -1 : strValues.Length);
            for (int i=0;i<iMaxLen;i++)
            {
                strFormat = string.Format("{0},[{1}={2}]", strFormat, arrParaName[i], strValues[i]);
            }
            return strFormat;
        }

        public static bool RegularTest(string strPartern, string strValue)
        {
            if (strValue == null) return false;
            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace ;
            return Regex.IsMatch(strValue, strPartern, options);            
        }
        

        public static string GetAppRootDir()
        {
            string strDir = Path.GetDirectoryName(typeof(TigerMarsUtil).Assembly.Location);
            strDir = Directory.GetParent(strDir).FullName;

            if (!Directory.Exists(strDir))
                Directory.CreateDirectory(strDir);
            return strDir;
        }

        public static int KillProcessBelong2TargetFold(string strTargetFold)
        {
            Process[] arrProcs = Process.GetProcesses();
            string strP = string.Format(@"^{0}", strTargetFold) ;
            foreach (Process objProc in arrProcs)
            {
                try
                {
                    //if (objProc.StartInfo.UseShellExecute) continue;
                    if (objProc.MainModule == null) continue;
                    if (objProc.MainModule.FileName.StartsWith(strTargetFold))
                    {
                        objProc.Kill();
                    }
                }
                catch (Exception)
                {
                    
                }
                
                
            }
            return 1;
        }

        public static int KillProcessByName(string strAppName, string strExtendTitle)
        {
            Process[] arrProcess = Process.GetProcessesByName(strAppName);
            if (arrProcess == null) return 1;
            for (int i=0 ;i<arrProcess.Length; i++)
            {
                try
                {
                    if ((string.Compare("TestFrameMonitor", strAppName,true)==0)&&(!string.IsNullOrEmpty(strExtendTitle)))
                    {
                        // try to find right process to be killed
                        string strTitle = (arrProcess[i].MainWindowTitle??"").ToLower();
                        if (!strTitle.StartsWith((strExtendTitle??"").ToLower()+":"))
                        {
                            continue;
                        }
                    }
                    arrProcess[i].Kill();
                    //arrProcess[i].CloseMainWindow();
                }
                catch (Exception e)
                {
                    Logger.Error("KillProcessByName", string.Format("Can't close Mainwindow oft:[{0}]\r\n\tExceptions:[{1}]",strAppName, e.Message),e);
                    try
                    {
                        //arrProcess[i].Kill();
                    }
                    catch (Exception ee)
                    {
                        Logger.Error("KillProcessByName", string.Format("Can't Kill process oft:[{0}]\r\n\tExceptions:[{1}]", strAppName, ee.Message), ee);
                        
                    }
                   
                }
                
            }
            return 1;
        }

        public static bool CheckPathAvailable(string strPath, ref string strError,ref string strAbsPath)
        {
            
            if (!File.Exists(strAbsPath=Path.Combine(Application.StartupPath, strPath)))
            {
                strError = string.Format("Can't find Such File:[{0}]-Orignal:[{1}]", strAbsPath,strPath);
                return false;
            }
            return true;

        }



        public static string ConvertQuickAccess2CommaMode(string strOrgQuickAccess)
        {
            string[] arrQA = (strOrgQuickAccess == null ? "" : strOrgQuickAccess).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string strURL = "";
            for (int i = 0; i < arrQA.Length; i++)
            {
                if (i == 0)
                {
                    strURL = string.Format(@"""{0}""", arrQA[i]);
                }
                else
                {
                    strURL = string.Format(@"{0},""{1}""", strURL, arrQA[i]);
                }
            }
            return strURL;
        }
    }


    public class MarsKeyValues<TKey, TValue>: INotifyPropertyChanged
    {
        private  TKey mKey ;
        private TValue mvalue;
        public TKey MKey { get { return mKey; } set { if (mKey.ToString() != value.ToString()) { mKey = value; OnPropertyChanged("MKey"); } } }
        public TValue MValue { get { return mvalue; } set { if (mvalue.ToString() != value.ToString()) { mvalue = value; OnPropertyChanged("MValue"); } } }
        public MarsKeyValues(TKey key, TValue value)
        {
            mKey = key;
            mvalue = value;
        }

        public KeyValuePair<TKey, TValue> ToKeyValuePair()
        {
            return new KeyValuePair<TKey, TValue>(mKey, mvalue);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return MValue == null ? "" : MValue.ToString();
        }
        public List<MarsKeyValues<string, string>> Children { get; set; }
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }


    }
}
