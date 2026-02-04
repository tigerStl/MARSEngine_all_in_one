#if !_MarsInOtherProcess
#if _MarsCDriver
extern alias clientWCF;
using clientWCF::MarsTestFrame.CommuniteServer;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#else
    using MarsTestFrame.CommuniteServer;
    using Route2NSEx.src.Marquis.systemUtil;
#endif


using com.Mars.Constants;
#else
using MarsTestFrame.CommuniteServer;
#endif
using System;
using System.Text;
using System.Xml.Serialization;

#if !_MarsInOtherProcess
namespace MarsTestFrame.SourceCode.xmlConfig
#else
namespace MarsEngineInProcess.SourceCode.xmlConfig
#endif
{
    public class UserCfgMgr
    {
#if !_MarsInOtherProcess
        private static MLogger Logger = MLogger.GetLogger(typeof(UserCfgMgr));
#endif
        public void LoadFromFile(string strFileName)
        {
            ;
            string strFile = typeof(UserCfgMgr).Assembly.Location;
            strFile = System.IO.Path.GetDirectoryName(strFile);
            string strUserNameFile = System.IO.Path.Combine(strFile, strFileName + ".xml");
            if (System.IO.File.Exists(strUserNameFile))
                currentUserEnv = XmlHelper.XmlDeserializeFromFile<MarsTestUserEnv>(strUserNameFile, Encoding.UTF8);
            else
            {
                currentUserEnv = new MarsTestUserEnv();
            }
        }
        private string userFileName = "";
        public string UserFileName
        {
            get { return userFileName; }
            set
            {
                if (string.Compare(userFileName, value, true) == 0) return;
                userFileName = value;
#if _NO_C_DRIVER_WRITE
                string strFile = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
#else
                string strFile = typeof(UserCfgMgr).Assembly.Location;
                strFile = System.IO.Path.GetDirectoryName(strFile);
#endif
                string strUserNameFile = System.IO.Path.Combine(strFile, userFileName + ".xml");
                if (!System.IO.File.Exists(strUserNameFile))
                {
                    try
                    {
                        System.IO.FileStream fs = System.IO.File.Create(strUserNameFile);
                        fs.Close();
                    }
                    catch (Exception e)
                    {
#if !_MarsInOtherProcess
                        Logger.Error("UserCfgMgr", string.Format("Exception:[{0}]", e.Message), e);
#endif
                    }
#if !_MarsInOtherProcess
                    currentUserEnv = new MarsTestUserEnv() { MarsUserTestMode = SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD };
#else
                    currentUserEnv = new MarsTestUserEnv() { MarsUserTestMode = "Build" };
#endif
                    SaveUserEnvToFile(currentUserEnv, strUserNameFile);
                }
                else
                {

                    currentUserEnv = XmlHelper.XmlDeserializeFromFile<MarsTestUserEnv>(strUserNameFile, Encoding.UTF8);
                }

            }
        }

        public string UserTestMode
        {
            get
            {
                if (currentUserEnv == null)
#if !_MarsInOtherProcess
                    return SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE;
#else
                    return null;
#endif
                return currentUserEnv.MarsUserTestMode;
            }

        }

        public string MarsAccountName
        {
            get
            {
                if (currentUserEnv == null) return null;
                return currentUserEnv.MarsAccountName;
            }
            set
            {
                if (currentUserEnv == null) currentUserEnv = new MarsTestUserEnv();
                currentUserEnv.MarsAccountName = value;
            }
        }

        public void SetUserTestMode(string strUserMode = null)
        {
            if (currentUserEnv == null) return;
            if (!string.IsNullOrEmpty(strUserMode))
                currentUserEnv.MarsUserTestMode = strUserMode;
#if _NO_C_DRIVER_WRITE
            string strFile = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
#else
            string strFile = typeof(UserCfgMgr).Assembly.Location;
            strFile = System.IO.Path.GetDirectoryName(strFile);
#endif
            string strUserNameFile = System.IO.Path.Combine(strFile, userFileName + ".xml");

            SaveUserEnvToFile(currentUserEnv, strUserNameFile);
        }

        public bool SaveUserEvnToFileByUserFileName(ref string strErrorr, ref string strAdv, ref string strStack)
        {
            string strFile = typeof(UserCfgMgr).Assembly.Location;
            strFile = System.IO.Path.GetDirectoryName(strFile);
            string strUserNameFile = System.IO.Path.Combine(strFile, userFileName + ".xml");
            try
            {
                XmlHelper.XmlSerializeToFile(currentUserEnv, strUserNameFile, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                strErrorr = "Can't save user Env File ";
                strAdv = "Contact Marquis when error continues";
                strStack = $"{ e.Message}\r\n{e.StackTrace}";
                return false;

            }
        }

        private void SaveUserEnvToFile(MarsTestUserEnv currentUserEnv, string strFullFileName)
        {
#if !_MarsInOtherProcess
            Logger.logBegin("SaveUserEnvToFile");
#endif
            try
            {
                XmlHelper.XmlSerializeToFile(currentUserEnv, strFullFileName, Encoding.UTF8);
            }
            catch (Exception e)
            {
#if !_MarsInOtherProcess
                Logger.Error("SaveUserEnvToFile", e.Message, e);
#endif
            }
            finally
            {
#if !_MarsInOtherProcess
                Logger.logEnd("SaveUserEnvToFile");
#endif
            }
        }

        private MarsTestUserEnv currentUserEnv = null;


        internal static string GetTestBaseModeInfo(string strUserEnvName)
        {
#if !_MarsInOtherProcess
            Logger.logBegin("GetTestBaseModeInfo", string.Format("user Name:[{0}]", strUserEnvName));
#endif
            try
            {
                UserCfgMgr tmpUserEnv = new UserCfgMgr()
                {
                    UserFileName = strUserEnvName
                };

                return tmpUserEnv.UserTestMode;
            }
            catch (Exception e)
            {
#if !_MarsInOtherProcess
                return SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD;
#else
                return null;
#endif
            }
            finally
            {
#if !_MarsInOtherProcess
                Logger.logEnd("GetTestBaseModeInfo");
#endif
            }


        }

        internal static void ChangeUserBaseLineMode(string currentLoginUser, string strValue)
        {
#if !_MarsInOtherProcess
            Logger.logBegin("ChangeUserBaseLineMode", string.Format("user Name:[{0}]", currentLoginUser));
#endif
            try
            {
                UserCfgMgr tmpUserEnv = new UserCfgMgr()
                {
                    UserFileName = currentLoginUser
                };

                tmpUserEnv.SetUserTestMode(strValue);

            }
            finally
            {
#if !_MarsInOtherProcess
                Logger.logEnd("ChangeUserBaseLineMode");
#endif
            }
        }
    }


    [XmlRoot(ElementName = "Env")]
    public class MarsTestUserEnv
    {
        [XmlElement(ElementName = "TestMode")]
        public string MarsUserTestMode
        {
            get;
            set;
        }

        [XmlElement(ElementName = "MarsAccountName")]
        public string MarsAccountName
        {
            get;
            set;
        }
    }
}
