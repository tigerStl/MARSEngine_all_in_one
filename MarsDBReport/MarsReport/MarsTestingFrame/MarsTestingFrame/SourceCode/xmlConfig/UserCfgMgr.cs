extern alias clientWCF;
#if _MarsCDriver
extern alias clientWCF;
using clientWCF::MarsTestFrame.CommuniteServer;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#else
using clientWCF::MarsTestFrame.CommuniteServer;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#endif

using com.Mars.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MarsTestFrame.SourceCode.xmlConfig
{
    public class UserCfgMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(UserCfgMgr)); 

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
                        Logger.Error("UserCfgMgr", string.Format("Exception:[{0}]", e.Message), e);
                    }
                    currentUserEnv = new MarsTestUserEnv() { MarsUserTestMode = SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD };
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
                if (currentUserEnv == null) return SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE;
                return currentUserEnv.MarsUserTestMode;
            }
           
        }

        public void SetUserTestMode(string strUserMode)
        {
            if (currentUserEnv == null) return;
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

        private void SaveUserEnvToFile(MarsTestUserEnv currentUserEnv,string strFullFileName)
        {
            Logger.logBegin("SaveUserEnvToFile");
            try
            {
                XmlHelper.XmlSerializeToFile(currentUserEnv, strFullFileName, Encoding.UTF8);                
            }catch(Exception e)
            {
                Logger.Error("SaveUserEnvToFile",e.Message, e);
            }
            finally
            {
                Logger.logEnd("SaveUserEnvToFile");
            }
        }

        private MarsTestUserEnv currentUserEnv=null;


        internal static string GetTestBaseModeInfo(string strUserEnvName)
        {
            Logger.logBegin("GetTestBaseModeInfo", string.Format("user Name:[{0}]", strUserEnvName));
            try
            {
                UserCfgMgr tmpUserEnv = new UserCfgMgr()
                {
                    UserFileName = strUserEnvName
                };

                return tmpUserEnv.UserTestMode;
            }catch(Exception e)
            {
                return SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD;
            }
            finally
            {
                Logger.logEnd("GetTestBaseModeInfo");
            }
            

        }

        internal static void ChangeUserBaseLineMode(string currentLoginUser, string strValue)
        {
            Logger.logBegin("ChangeUserBaseLineMode", string.Format("user Name:[{0}]", currentLoginUser));
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
                Logger.logEnd("ChangeUserBaseLineMode");
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
    }
}
