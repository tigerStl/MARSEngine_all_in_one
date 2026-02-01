namespace Mars.message.Utility
{
    public class MarsConstants
    {
        public static int   NumberOfDataSetColumns          = 20;
        public const string CNST_TEMP_PICTUREPATH           = "TempPicturePath";
        public const string CNST_TEST_REPORT_PATH           = "REPORT_PATH";
        public const string CNST_TEST_SOFTWARE_PROVIDER     = "SoftWareProvider";
        public const string CNST_TEST_CLIENTFULLNAME        = "ClientFullName";
        public const string CNST_REPORTFIRSTPAGEHEADER      = "ReportFirstPageHeader";
        public const string CNST_REPORTPAGEEYEBROW          = "ReportPageEyebrow";
        public const string CNST_TARGET_APPLICATION         = "TestTargetApplication";
        public const string CNST_DATABASE_PASSWORD          = "MARS_DB_PWD";
        ///自动获取数据库数据的配置文件位置
        /// 
        public const string CNST_AUTO_LOAD_DATAFROMDB_PATH  = "AutoloadDataFromDBCfgPath";
        public const string CNST_AUTO_LOAD_DATA_DIRECTORY   = "autoLoadDataCfg";
        public const string CNST_COMMENT_IGNORE_ERROR       = "_MARS_IGNORE_ERROR_COMMNET_";
        public const string CNST_QUERY_URL_UUID_PARA_NAME   = "uuid";
        public const string CNST_QUERY_CMD_START_SVC        = "startEngineSvc";
        public const string CNST_QUERY_URL_COMMAND          = "command";
        public const string CNST_QUERY_URL_COMMAND_OBJTOOL  = "objectRegTool";
        public const string CNST_QUERY_URL_OBJREC           = "ObjRec";
        public const string CNST_QUERY_URL_RECORDREPLAY     = "RecordReplay"; //2025, RECORD and replay

        public const string CNST_SYPTOOL_EXE_NAME           = "MarsSpyTool.exe";
        public const string CNST_SYPTOOL_PARA_UUID          = "-uuid";
        public const string CNST_SPYTOOL_PARA_MODE          = "-mode";
        public const string CNST_SPYTOOL_PARA_REMOTESERVER  = "-server";        
        public const string CNST_SPYTOOL_PARA_MODE_RECTC    = "TestRec";
        public const string CNST_SYPTOOL_UUIDFILE_NAME      = "marsObjUUID.uuid";
        public const string CNST_SYPTOOL_JSONOBJ_FILENAME   = "marsobjectFile.objson";
        public const string CNST_SYPTOOL_STEPS_FILENAME     = "marsStepsFile.objson";
        public const string CNST_SPYTOOL_OBJ_FILE_ENDMARK   = "::ENDMARK::";
        public const string CNST_SYPTOOL_MONITOR_TYPE       = "*.objson";

        public const string CNST_AUTO_CHECKERROR_PREFIX     = "__MARS_AUTO_ERROR:";

        /// keyword 常数
        /// 
        public const string CNST_KEYWORD_WAITUNTIL          = "WaitUntil";
    }

    public class MarsGlobarVar
    {
        public static string UUID_FROM_WEB = null;
        public static string Update_testResultURL = null;
        public static string MARS_WEB_HOST = null;// MARS web 
        public static string MARS_WEB_STORYSTATUS_CALLBACK = null;
        public static string MARS_CURRENT_DB = null;
        public static string MARS_current_StoryboardId = null;
    }


}
