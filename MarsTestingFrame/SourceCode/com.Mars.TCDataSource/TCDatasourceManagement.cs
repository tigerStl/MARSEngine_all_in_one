using com.Mars.Constants;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.TCDataSource
{
    public class TCDatasourceManagement
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TCDatasourceManagement));
        private TCDatasourceManagement():base()
        {

        }
        internal static int GetTCRunLoopCount(string strTCName)
        {
            Logger.logBegin("GetTCRunLoopCount");
            /*** get setting for TCSource ***/
            string strSourceTC = AppConfigReader.GetTCDataSource();
            TCDatasourceManagement objInstance = getInstance();
            MARS_ADAPTER eSourceAdpt = ConvertStringToAdptr(strSourceTC);
            if (eSourceAdpt == MARS_ADAPTER._ADPTR_NOT_DEF)
            {
                throw new MarsExceptions((int)ERROR_CODE._TCDATA_NO_SUCH_ADAPTER, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_NO_SUCH_ADAPTER), eSourceAdpt));
            }

                

            Logger.logEnd("GetTCRunLoopCount");
            return -1;
        }

        protected static TCDatasourceManagement getInstance()
        {
            return new TCDatasourceManagement();
        }

        protected static MARS_ADAPTER ConvertStringToAdptr(string strSource)
        {
            Logger.logBegin("ConvertStringToAdptr");

            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE_XLS, strSource,true)==0)
            {
                return MARS_ADAPTER._ADPTR_TCDATASOURCE_XLS;
            }
#if _Datafrom_Database
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE_DB, strSource, true) == 0)
            {
                return MARS_ADAPTER._ADPTR_TCDATASOURCE_DB;
            }
            
#endif
            Logger.logEnd("ConvertStringToAdptr");
            return MARS_ADAPTER._ADPTR_NOT_DEF;
        }

        public static TCDataFile LoadDataFile(string strTCName, ref ERROR_CODE eCde)
        {
            Logger.logBegin("LoadDataFile");
            string strDataFileName = string.Format("{0}\\data\\{1}_DATA", AppConfigReader.GetXlsRootPath(), strTCName);
            string strDataFileWithPath = string.Format("{0}{1}.xls", "", strDataFileName);
            if (!File.Exists(strDataFileWithPath))
            {
                Logger.Error("LoadDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_NO_SUCH_DATAFILE), strDataFileWithPath));
                eCde = ERROR_CODE._TCDATA_NO_SUCH_DATAFILE;
                return null;
            }
            TCDataFile objDataFile = new TCDataFile();
            objDataFile.XlsFileNameWithPath = string.Format("{0}_DATA", strTCName);
            objDataFile.ExtraFilePath = strDataFileWithPath;
            eCde = objDataFile.loadTestCase();
            Logger.logEnd("LoadDataFile");
            return objDataFile;
        }

    }
}
