using com.Mars.Constants;
using Mars.Securities;
using MarsTestFrame.SourceCode.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


/**
 * Record:
 *   Date  : 07/31/2015
 *   Reason: for server mode, services can start without starting qtp at the same time
 * 
 * */

namespace MarsFrameWork
{

    
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private FrameWorkStartMode CurrentStartMode = FrameWorkStartMode.FWSM_Normal;

        public FrameWorkStartMode currentStartMode { get { return this.CurrentStartMode; } }

        public ILicenseMgr gLicenseMgr { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            //MessageBox.Show(string.Format("{0}",e.Args.Length));
            try
            {
                if (e.Args.Length < 1)
                {
                    gLicenseMgr = TestFrameLicense.LoadLicense();
                    return;
                }
                if ("-S".CompareTo(e.Args[0])==0)
                {
                    /** just services **/
                    CurrentStartMode |= FrameWorkStartMode.FWSM_Slience;
                    gLicenseMgr = TestFrameLicense.LoadLicense();
                }
                if ("-GL".CompareTo(e.Args[0])==0)
                {
                    /** generate lisence file **/
                    if (e.Args.Length < 2)
                    {
                        Console.WriteLine("-GL [Date-YYYYMMDD] [Hard DiskNumber]");
                        Application.Current.Shutdown();
                    }
                    TestFrameLicense.Save(e.Args[1], e.Args.Length<=2?null:e.Args[2]);
                    TestFrameLicense.LoadLicense();
                    
                    Application.Current.Shutdown();
                }
                if (("-K".CompareTo(e.Args[0])==0)||(("-k".CompareTo(e.Args[0]) == 0)))
                {
                    List<string> strProcessName = new List<string>() { "UFT", "QtpAutomationAgent" };
                    if (e.Args.Length>=2)
                    {
                        strProcessName.Clear();
                        strProcessName.Add(e.Args[1]);
                    }
                    try
                    {
                        foreach (var pname in strProcessName)
                        {
                            Process[] arrP = Process.GetProcessesByName(pname);
                            Console.WriteLine("Find process [{0}], times:[{1}]", pname, arrP == null ? "NULL" : arrP.Length.ToString());
                            foreach (var p in arrP)
                            {
                                string strPName = pname;
                                if (p == null) continue;
                                try
                                {
                                    strPName = p.MainModule.FileName;
                                    
                                }
                                catch (Exception )
                                {
                                    
                                }
                                finally
                                {
                                    p.Kill();
                                    Console.WriteLine("[{0}] Killed", strPName);
                                }
                                
                                
                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Can't kill applciation [{0}], please kill it from taskmgr.exe\r\nException:[{1}]\r\n{2}", strProcessName,ex.Message,
                            ex.StackTrace);
                    }
                    Application.Current.Shutdown();
                }
                if (string.Compare("-PWD", e.Args[0],true)==0)
                {
                    if (e.Args.Length!=2)
                    {
                        Console.WriteLine(@"Format For Password generation:
MARSUtility -PWD yourpasswd
---------------------------
Then encoded password will generate. Copy the string to Mars.exe.config.");
                        Application.Current.Shutdown();
                    }
                    
                    string strUnencodedPwd = e.Args[1];
                    Console.WriteLine("Begin to Encode [{0}]", strUnencodedPwd);
                    string encodedPwd = MarsEncodePwd.EncodeString(strUnencodedPwd) ;
                    Console.WriteLine(string.Format("The encoded string is [{0}]", encodedPwd));
                    Application.Current.Shutdown();
                }
            }
            finally
            {
                //base.OnStartup(e);
            }
            
        }
    }
}
