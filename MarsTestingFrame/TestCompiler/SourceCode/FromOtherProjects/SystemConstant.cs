using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace com.Mars.Constants
{
    public enum ERROR_CODE{
        _OBJECT_IS_NULL=0x00,
        _NO_ERROR=0x01,

        #region QTPBatch file's Error
        _BATCH_ERROR_JET_READ_EXCEPTION = 0x100,
        _BATCH_ERROR_NO_EXECUTE_TABLE = 0x101 ,
        #endregion

        #region TC/Test step 
        _TEST_STEP_NO_SUCH_TABLE_OR_SHEET = 0x200,

        #endregion

        #region App.config
        _APP_NO_SECTION_SPECIAL=0x300,
        _APP_NO_SECTION_KEY = 0x301,
        #endregion 

    }

    public static class ERROR_INFO{
        public static string GET_ERROR_STR(ERROR_CODE eCode)
        {
            switch(eCode)
            {
                case ERROR_CODE._NO_ERROR: return MarsTestFrame.Properties.Resources._NO_ERROR;
                case ERROR_CODE._OBJECT_IS_NULL: return MarsTestFrame.Properties.Resources._OBJECT_IS_NULL;
                case ERROR_CODE._BATCH_ERROR_JET_READ_EXCEPTION: return MarsTestFrame.Properties.Resources._BATCH_ERROR_JET_READ_EXCEPTION;
                case ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE: return MarsTestFrame.Properties.Resources._BATCH_ERROR_NO_EXECUTE_TABLE;
                case ERROR_CODE._TEST_STEP_NO_SUCH_TABLE_OR_SHEET: return MarsTestFrame.Properties.Resources._TEST_STEP_NO_SUCH_TABLE_OR_SHEET;
                case ERROR_CODE._APP_NO_SECTION_SPECIAL: return MarsTestFrame.Properties.Resources._APP_NO_SECTION_SPECIAL;
                case ERROR_CODE._APP_NO_SECTION_KEY: return MarsTestFrame.Properties.Resources._APP_NO_SECTION_KEY;
                default: return "";
            }
        }
    }

    public enum TC_STATUS
    {
        _INITIALIZED = 0x01,
        _COMPILED = 0x02,
        _RUNNING = 0x03
    }


    public class SystemConstant
    {
        #region Xls Jeb DB
        public const string CNST_XLS_JETDB_TABLENAME = "TABLE_NAME";
        #endregion

        #region Xls qtp Batch
        public const string CNST_XLS_HEADER_RUN = "RUN";
        public const string CNST_XLS_HEADER_TEST_WORKBOOK = "TEST_WORKBOOK";
        public const string CNST_XLS_HEADER_TEST_SHEET = "TEST_SHEET";
        #endregion

        #region Xl TC file
        public const string CNST_XLS_HEADER_OBJECT = "OBJECT";
        public const string CNST_XLS_HEADER_RC = "ROW_COLUMN";
        public const string CNST_XLS_HEADER_KEYWORD = "KEYWORD";
        public const string CNST_XLS_HEADER_VALUE = "VALUE";
        public const string CNST_XLS_HEADER_COMMENT = "COMMENT";
        #endregion

        #region App.config 
        public const string CNST_APPCONFIG_SECITON_XLSCONFGMODE = "XlsConfigMode";
        public const string CNST_APPCONFIG_SECTION_XLSMODE_ROOT = "RootPath";
        #endregion
    }

    public enum MARS_ADAPTER
    {
        #region Test suite adapter
        _ADPTR_XLSJET_2_TESTSUITE = 0x01,
        #endregion

        #region Test step adapter
        _DAPTR_XLSJET_2_TESTSTEP = 0x10,
        #endregion
    }

}
