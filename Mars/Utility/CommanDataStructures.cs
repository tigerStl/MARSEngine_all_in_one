using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Mars.Utility
{
    public class CommanDataStructures
    {
    }

    public class MarsUtilities
    {
        
        public static string ConvertToUnsecureString(System.Security.SecureString securePassword)
        {
            if (securePassword == null)
            {
                return string.Empty;
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static void StartQTPStarer(string strCmdParameter)
        {
            //Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            //{
            //    (new Thread(new ThreadStart(delegate ()
            //    {
                    ProcessStartInfo StartInfo = new ProcessStartInfo();
                    StartInfo.FileName = @".\QtpStarter.exe";
                    StartInfo.Arguments = strCmdParameter; //"/StartQTPBackGround";
                    StartInfo.Domain = "NewMarsDomain";
                    Process objNewProce = new Process();
                    //Logger.Info("StartTest", string.Format("anonymous Thread start begin"));
                    objNewProce.StartInfo = StartInfo;
                    objNewProce.Start();
                    //Logger.Info("StartTest", string.Format("anonymous Thread started, with process Id:[{0}]", objNewProce.Id));
            //    }))).Start();

            //}));
        }

        internal static bool StartMarsEngine(string strCurrentUserName,string applicationId, long? iStoryBoardID, 
            string strStoryboardName, 
            bool isBaseLineTest,
            bool isContinue,
            bool isIgnoreError,
            bool isx64 , //default is true
            ref string strError)
        {
            ////admin -S “TigerTest” 138394 -App 2 -Mode NonBase -IsContinue true -IsIgnoreError true
            //kill monitor and start

            //if (!MarsProcessMgr.KillAndstarterMonitor(strCurrentUserName,ref strError))
            //{
            //    return false;
            //}

            return MarsProcessMgr.StartEngin(strCurrentUserName,applicationId, iStoryBoardID, strStoryboardName, 
                isBaseLineTest,
                isContinue,
                isIgnoreError,
                isx64,
                ref strError);            
        }

        private static class MarsProcessMgr
        {
            
            const string cnst_monitor     = "TestFrameMonitor";
            const string cnst_marsengine  = "Mars.AutoTestingDriver";
            const string cnst_marsengine32= "Mars.AutoTestingDriver32";
            private static MLogger Logger = MLogger.GetLogger(typeof(MarsProcessMgr));
            internal static bool KillAndstarterMonitor(string strUserName, ref string strError)
            {
                Process[] arrP = Process.GetProcessesByName(cnst_monitor);
                if ((arrP != null) && (arrP.Length > 0))
                {
                    foreach (var p in arrP)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch (Exception e)
                        {
                            strError = string.Format("Exception when Kill process:[{0}] -{1}\r\n {2}", cnst_monitor, e.Message, e.StackTrace);
                            return false;
                        }
                    }
                }

                string strPath = typeof(MarsProcessMgr).Assembly.Location;                
                strPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(strPath), cnst_monitor + ".exe");
                if (!System.IO.File.Exists(strPath))
                {
                    strError = string.Format("No such file exists:[{0}]", strPath);
                    return false;
                }

                //start monitor
                Process pMonitor = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        FileName = strPath,
                        Arguments = strUserName,
                    }
                };
                pMonitor.Start();

                return true;                
            }

            internal static bool KillEngine(string strEnginProcId)
            {
                Process[] arrEng = Process.GetProcessesByName(strEnginProcId);
                long lCurrentSessionId = Process.GetCurrentProcess().SessionId;
                foreach (var p in arrEng)
                {
                    if (p == null) continue;
                    try
                    {
                        if (p.SessionId == lCurrentSessionId)
                            p.Kill();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    
                }
                return true;
            }
            internal static bool StartEngin(string strCurrentLogName,string applicationId, long? iStoryBoardID, string strStoryboardName, 
                bool isBaseLineTest, 
                bool isContinue, 
                bool isIgnoreError,
                bool isx64,
                ref string strError,
                string strSpecialAppRequirement=null)
            {
                string strPath = typeof(MarsProcessMgr).Assembly.Location;
                string strEngine = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(strPath), (isx64?cnst_marsengine: cnst_marsengine32) + ".exe");
                
                if (!System.IO.File.Exists(strEngine))
                {
                    strError = string.Format("no such File [{0}]", strEngine);
                    Logger.Error("StartEngin", strError);
                    return false;
                }

                ///kill engine first
                ///
                KillEngine(cnst_marsengine  );
                KillEngine(cnst_marsengine32);

                //create thread to start engine
                //-S "TPG DEMO" 27275  -App 14 -Mode Base
                new Thread(new ThreadStart(
                    new Action(() => {
                        string strSpecialRequire = "";
                        if (!string.IsNullOrEmpty(strSpecialAppRequirement))
                        {
                            strSpecialRequire = " -ExtRequire "+ strSpecialAppRequirement;
                        }
                        Process pEng = new Process()
                        {
                                StartInfo = new ProcessStartInfo()
                                {
                                    UseShellExecute =false ,
                                    FileName = strEngine,
                                    Arguments = string.Format("{4} -S \"{0}\" {1} -App {2} -Mode {3} -Continue {5} -IgnoreError {6} {7} -currentDB {8}",
                                        strStoryboardName,
                                        iStoryBoardID ?? -1,
                                        applicationId,
                                        isBaseLineTest ? "Base" : "NonBase",
                                        strCurrentLogName,
                                        isContinue, 
                                        isIgnoreError,
                                        strSpecialRequire,
                                        MarsMainWindow.CurrentDatabaseIdx
                                        )
                                },
                                
                        };
                        
                        Logger.Info("StartEngine", string.Format("command is :[{0} {1}]", strEngine, pEng.StartInfo.Arguments));
                        pEng.Start();
                        })
                )).Start();
                return true;
            }
        }

        
    }

    public enum MarsInvokeFrom
    {
        e_FromUnknow=-1,
        e_FromTreeView =0,
        e_FromDockTab,
        e_FromMenu_Rebbon,
        e_FromMenu_Contex,
        e_FromNotTreeView
        
    }

    //public class MarsKeyValues<TKey, TValue> : INotifyPropertyChanged
    //{
    //    private TKey mKey;
    //    private TValue mvalue;
    //    public TKey MKey { get { return mKey; } set { if (mKey.ToString() != value.ToString()) { mKey = value; OnPropertyChanged("MKey"); } } }
    //    public TValue MValue { get { return mvalue; } set { if (mvalue.ToString() != value.ToString()) { mvalue = value; OnPropertyChanged("MValue"); } } }
    //    public MarsKeyValues(TKey key, TValue value)
    //    {
    //        mKey = key;
    //        mvalue = value;
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    public override string ToString()
    //    {
    //        return MValue == null ? "" : MValue.ToString();
    //    }
    //    public List<MarsKeyValues<string, string>> Children { get; set; }
    //    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    //    {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null)
    //            handler(this, e);
    //    }
    //    protected void OnPropertyChanged(string propertyName)
    //    {
    //        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    //    }
    //}
}
