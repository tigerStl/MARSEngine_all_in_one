using System;

namespace com.Mars.Constants
{
    public enum ERROR_CODE
    {
        _OBJECT_IS_NULL = 0x00,
        _NO_ERROR = 0x01,

        #region QTPBatch file's Error
        _BATCH_ERROR_JET_READ_EXCEPTION = 0x100,
        _BATCH_ERROR_NO_EXECUTE_TABLE = 0x101,
        _BATCH_ERROR_CANT_UPDATE_PARA_4 = 0x102,
        #endregion

        #region TC/Test step
        _TEST_STEP_NO_SUCH_TABLE_OR_SHEET = 0x200,
        _TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_PARA_1 = 0x201,
        _TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_NUMBER_PARA_1 = 0x202,
        _TEST_STEP_COMPARISON_MODE_VALUE_SETTING_NO_OBJECT_PARA_1 = 0x203,
        _TEST_STEP_COMPARISON_MODE_BASELINE_PARAMETER_NOTSUPPORT_PARA_1 = 0x204,
        #endregion

        #region App.config
        _APP_NO_SECTION_SPECIAL = 0x300,
        _APP_NO_SECTION_KEY = 0x301,
        _APP_NO_SETTING_OBJECT_FROM = 0x302,
        _APP_WRONG_VALUE_SETTING_OBJECT_FROM = 0x303,
        _APP_NO_SETTING_KEYWORD_FROM = 0x304,
        _APP_WRONG_VALUE_SETTING_KEYWORD_FROM = 0x305,
        _APP_NO_SECTION_KEWWORDS = 0x306,
        #endregion

        #region Steps Compiler
        _COMPILER_NO_STEPS = 0x400,
        _COMPILER_TEST_STEP_OBJECT_EXPECT = 0x401,
        _COMPILER_OBJECT_TYPE_MISMATCH_CONFIGTEST_REQUIRED = 0x402,
        _COMPILER_SETCURRENT_APPLICATION_VALUEFORMAT_ERROR = 0x403,
        _COMPILER_NO_SUCH_APPLICATION_CONFIGED = 0x404,
        _COMPILER_NO_SUCH_OBJECT_FILE = 0x405,
        _COMPILER_CANT_LOAD_OBJECTFILE = 0x406,
        _COMPILER_UNKNOW_GETDATA_FROM_DICFILE = 0x407,
        _COMPILER_NO_DEFAULT_APPLICATION_INFO = 0x408,
        _COMPILER_UNKNOW_ERROR_GET_DEFAULT_APP = 0x409,
        _COMPILER_NO_SUCH_PEGWINDOW_INFO = 0x40a,
        _COMPILER_APPLICATON_LENTH_NOT_1 = 0x40b,
        _COMPILER_NO_PEGWINDOW_FOR_TESTSTEP_FIND = 0x40c,
        _COMPILER_NO_RC = 0x40d,
        _COMPILER_NO_PEGWINDOW_IDENTIFIERINFO = 0x40e,
        _COMPILER_NO_OBJECT_INDENTIFY_INFO = 0x40F,
        _COMPILER_SUBACTION_OBJECT_ISNULL_PARA_0 = 0x410,
        _COMPILER_SUBACTION_KEYWORD_IS_NOT_A_SUBACTION_PARA_1 = 0x411,
        _COMPILER_SUBACTION_NO_PEGININFORMATION_PARA_2 = 0x412,
        _COMPILER_NOT_THE_CURRENT_TESTCASE_SERVIING_PARA_4 = 0x413,
        #endregion

        #region Reg-Applications
        _REG_APPS_NO_SUCH_APPLICATION_SHORTNAMEORPATH = 0x500,

        #endregion

        #region Keywords setting
        _KEYWORDS_SETTING_NO_SUCHAPPLICATION_SHORTNAME = 0x600,
        _KEYWORDS_PARSE_NO_SUCHAPPLICATION_SHORTNAME = 0x601,
        _KEYWORDS_PARSE_NO_SUCH_PEGWINDOW = 0x602,
        _KEYWORDS_PARSE_NO_SUCH_OBJECT_UNDER_PEGWINDOW = 0x603,
        _KEYWORDS_PARSE_CONFIGED_OBJECT_TYPE_WRONG = 0x604,
        _KEYWORDS_PARSE_REFLECTION = 0x605,
        _KEYWORDS_PARSE_REFLECTION_NULL = 0x606,
        _KEYWORDS_NO_SUCH_FUNCTION = 0x607,
        _KEYWORDS_NO_SUCH_KEYWORDS_CONCEPT_FOUND = 0x608,
        _KEYWORDS_UNSUPPORT_RUN_FROM = 0x609,
        _KEYWORDS_CALL_FORMATTER_PARA_1 = 0x60A,
        _KEYWORDS_IF_FORMATTER_NO_VALUE_PARA_0 = 0x60B,
        _KEYWORDS_IF_FORMATTER_SETTING_ERROR_PARA_1 = 0x60C,
        #endregion

        #region Qtp Section
        _QTP_ERROR_VALIDATE = 0x700,
        _QTP_ERROR_GENERAL = 0x701,
        _QTP_ERROR_REFRESH = 0x702,
        _QTP_ERROR_INSTANCE_NULL = 0x703,
        _QTP_ERROR_CREATE_EMPTYTEST = 0x704,
        _QTP_ERROR_SETTINGADDINS = 0x705,
        #endregion

        #region TC Data
        _TCDATA_NO_SUCH_ADAPTER = 0x800,
        _TCDATA_NO_SUCH_DATAFILE = 0x801,
        _TCDATA_LOAD_TABLENAMES = 0x802,
        _TCDATA_NO_DATA_SHEET = 0x803,
        _TCDATA_NO_DATA_FIND = 0x804,
        _TCDATA_EXCEPTION_READFROM_ONEROW = 0x805,
        _TCDATA_EXCEPTION_LOADDATA = 0x806,
        _TCDATA_DATA_COLUMN_EXCEED = 0x807,
        _TCDATA_DATA_CELL_EXCEPTION_PARA_3 = 0x808,
        _TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2 = 0x809,
        _TCDATA_DATA_UPDATE_EXCEPTION_PARA_3 = 0x8A0,
        _TCDATA_DATA_UPDATECACHE_LOOPID_LESS_0_PARA_2 = 0x8A1,
        _TCDATA_DATA_UPDATECACHE_LOOPID_GREATER_PARA_3 = 0x8A2,
        _TCDATA_DATA_UPDATECACHE_NO_OBJECTINDEX_PARA_1 = 0x8A3,
        _TCDATA_DATA_INSERT_OBJECT_EXCEPTION_PARA_3 = 0x8A4,
        #endregion

        #region JetDB
        _JET_DB_CONNECTOIN_OPEN_PARA_1 = 0x8F0,
        _JET_DB_CMD_EXPAND_COLUMN_PARA_1 = 0x8F1,
        _JET_DB_GET_COLUMN_EXCEPTION_PARA_1 = 0x8F2,
        _JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2 = 0x8F3,
        _JET_DB_NO_EXPAND_COLUMN_SUPPORT_PARA_1 = 0x8F4,
        _JET_DB_EXCEPTION_WHEN_RUN_QUERY_PARA_2 = 0x8F5,
        _JET_DB_FIELDNAMES_DNTMATCH_VALUES_PARA_2 = 0x8F6,
        #endregion

        #region Service WCF Error
        _SERVICE_ERROR_OBJECT_TYPE_IS_NOT_TEST_STEP = 0x900,
        _SERVICE_ERROR_NO_HOST_SETTING = 0x901,
        _SERVICE_ERROR_NO_PROTOCO_SETTING = 0x0902,
        _SERVICE_ERROR_NO_PORT_SETTING = 0x0903,
        _SERVICE_ERROR_NO_SERVICENAME_SETTING = 0x0904,
        _SERVICE_ERROR_NO_SERVICE_START_UNKNOW = 0x0905,
        _SERVICE_ERROR_NO_SERVICE_STOP_UNKNOW = 0x0906,
        _SERVICE_ERROR_CLIENT_UNKNOW = 0x09A1,
        _SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST = 0x9A2,
        _SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE = 0x9A3,
        _SERVICE_ERROR_CLIENT_CANT_END_NAVIGATE = 0x9A4,
        _SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING = 0x9A5,
        _SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1 = 0x9A6,
        _SERVICE_ERROR_CLIENT_OBJECTNAME_ISNULL_PARA_0 = 0x9A7,
        _SERVICE_ERROR_CLIENT_KEYWORDNAME_ISNULL_PARA_0 = 0x9A8,
        _SERVICE_ERROR_CLIENT_DATA_ISNULL_PARA_0 = 0x9A9,
        _SERVICE_ERROR_CLIENT_LOOP_ISNULL_PARA_0 = 0x9AA,
        _SERVICE_ERROR_CLIENT_LOOP_ISNOT_A_NUMBER_PARA_1 = 0x9AB,
        _SERVICE_ERROR_CLIENT_SWITCH_DATA_NO_FILENAME_PARA_0 = 0x9AC,
        _SERVICE_ERROR_SERVER_NO_TESTCASENAME_PARA_0 = 0x9AD,
        _SERVICE_ERROR_IF_SUBOBJECT_REQUIRED_PARA_1 = 0x9AE,
        _SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1 = 0x9AF,
        _SERVICE_ERROR_NO_COMPILER_ASSIGNED_PARA_2 = 0x9B0,
        _SERVICE_ERROR_NO_TESTCASE_GETTER_ASSIGNED_PARA_0 = 0x9B1,
        _SERVICE_ERROR_NO_TESTSUITE_NAVIGATE_ASSIGNED_PARA_0 = 0x9B2,
        _SERVICE_ERROR_NO_GETNEXTTESTSUITE_ASSIGNED_PARA_0 = 0x9B3,
        _SERVICE_ERROR_CALL_BEGIN_NAVIGATEFIRST_PARA_0 = 0x9B4,
        _SERVICE_ERROR_SERVER_ASSIGN_LOADTESTSUITE_FIRST_PARA_0 = 0x9B5,
        _SERVCIE_ERROR_NO_TESTSUITE_IS_DONE_STATUS_ASSIGNED_PARA_0 = 0x9B6,
        _SERVICE_ERROR_PARAMETER_SHOULDBE_INT_PARAP_1 = 0X9B7,
        #endregion

        #region Client Error
        _CLIENT_ERROR_PARAMETERISNULL_PARA_1 = 0xA00,
        #endregion //Client Error


    }

    public static class ERROR_INFO
    {
        public static string GET_ERROR_STR(ERROR_CODE eCode)
        {
            switch (eCode)
            {
                case ERROR_CODE._NO_ERROR: return TestFrameMonitor.Properties.Resources._NO_ERROR;
                case ERROR_CODE._OBJECT_IS_NULL: return TestFrameMonitor.Properties.Resources._OBJECT_IS_NULL;
                case ERROR_CODE._BATCH_ERROR_JET_READ_EXCEPTION: return TestFrameMonitor.Properties.Resources._BATCH_ERROR_JET_READ_EXCEPTION;
                case ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE: return TestFrameMonitor.Properties.Resources._BATCH_ERROR_NO_EXECUTE_TABLE;
                case ERROR_CODE._BATCH_ERROR_CANT_UPDATE_PARA_4: return TestFrameMonitor.Properties.Resources._BATCH_ERROR_CANT_UPDATE_PARA_4;
                case ERROR_CODE._TEST_STEP_NO_SUCH_TABLE_OR_SHEET: return TestFrameMonitor.Properties.Resources._TEST_STEP_NO_SUCH_TABLE_OR_SHEET;
                case ERROR_CODE._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_PARA_1: return TestFrameMonitor.Properties.Resources._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_PARA_1;
                case ERROR_CODE._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_NUMBER_PARA_1: return TestFrameMonitor.Properties.Resources._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_NUMBER_PARA_1;
                case ERROR_CODE._TEST_STEP_COMPARISON_MODE_VALUE_SETTING_NO_OBJECT_PARA_1: return TestFrameMonitor.Properties.Resources._TEST_STEP_COMPARISON_MODE_VALUE_SETTING_NO_OBJECT_PARA_1;
                case ERROR_CODE._TEST_STEP_COMPARISON_MODE_BASELINE_PARAMETER_NOTSUPPORT_PARA_1: return TestFrameMonitor.Properties.Resources._TEST_STEP_COMPARISON_MODE_BASELINE_PARAMETER_NOTSUPPORT_PARA_1;
                case ERROR_CODE._APP_NO_SECTION_SPECIAL: return TestFrameMonitor.Properties.Resources._APP_NO_SECTION_SPECIAL;
                case ERROR_CODE._APP_NO_SECTION_KEY: return TestFrameMonitor.Properties.Resources._APP_NO_SECTION_KEY;
                case ERROR_CODE._COMPILER_NO_STEPS: return TestFrameMonitor.Properties.Resources._COMPILER_NO_STEPS;
                case ERROR_CODE._COMPILER_TEST_STEP_OBJECT_EXPECT: return TestFrameMonitor.Properties.Resources._COMPILER_TEST_STEP_OBJECT_EXPECT;
                case ERROR_CODE._COMPILER_OBJECT_TYPE_MISMATCH_CONFIGTEST_REQUIRED: return TestFrameMonitor.Properties.Resources._COMPILER_OBJECT_TYPE_MISMATCH_CONFIGTEST_REQUIRED;
                case ERROR_CODE._COMPILER_SETCURRENT_APPLICATION_VALUEFORMAT_ERROR: return TestFrameMonitor.Properties.Resources._COMPILER_SETCURRENT_APPLICATION_VALUEFORMAT_ERROR;
                case ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED: return TestFrameMonitor.Properties.Resources._COMPILER_NO_SUCH_APPLICATION_CONFIGED;
                case ERROR_CODE._APP_NO_SETTING_OBJECT_FROM: return TestFrameMonitor.Properties.Resources._APP_NO_SECTION_OBJECT_FROM;
                case ERROR_CODE._APP_WRONG_VALUE_SETTING_OBJECT_FROM: return TestFrameMonitor.Properties.Resources._APP_WRONG_VALUE_SETTING_OBJECT_FROM;
                case ERROR_CODE._REG_APPS_NO_SUCH_APPLICATION_SHORTNAMEORPATH: return TestFrameMonitor.Properties.Resources._REG_APPS_NO_SUCH_APPLICATION_SHORTNAMEORPATH;
                case ERROR_CODE._COMPILER_NO_SUCH_OBJECT_FILE: return TestFrameMonitor.Properties.Resources._COMPILER_NO_SUCH_OBJECT_FILE;
                case ERROR_CODE._COMPILER_CANT_LOAD_OBJECTFILE: return TestFrameMonitor.Properties.Resources._COMPILER_CANT_LOAD_OBJECTFILE;
                case ERROR_CODE._COMPILER_UNKNOW_GETDATA_FROM_DICFILE: return TestFrameMonitor.Properties.Resources._COMPILER_UNKNOW_GETDATA_FROM_DICFILE;
                case ERROR_CODE._COMPILER_NO_DEFAULT_APPLICATION_INFO: return TestFrameMonitor.Properties.Resources._COMPILER_NO_DEFAULT_APPLICATION_INFO;
                case ERROR_CODE._COMPILER_UNKNOW_ERROR_GET_DEFAULT_APP: return TestFrameMonitor.Properties.Resources._COMPILER_UNKNOW_ERROR_GET_DEFAULT_APP;
                case ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO: return TestFrameMonitor.Properties.Resources._COMPILER_NO_SUCH_PEGWINDOW_INFO;
                case ERROR_CODE._COMPILER_APPLICATON_LENTH_NOT_1: return TestFrameMonitor.Properties.Resources._COMPILER_APPLICATON_LENTH_NOT_1;
                case ERROR_CODE._COMPILER_NO_PEGWINDOW_FOR_TESTSTEP_FIND: return TestFrameMonitor.Properties.Resources._COMPILER_NO_PEGWINDOW_FOR_TESTSTEP_FIND;
                case ERROR_CODE._COMPILER_NO_RC: return TestFrameMonitor.Properties.Resources._COMPILER_NO_RC;
                case ERROR_CODE._COMPILER_NO_PEGWINDOW_IDENTIFIERINFO: return TestFrameMonitor.Properties.Resources._COMPILER_NO_PEGWINDOW_IDENTIFIERINFO;
                case ERROR_CODE._COMPILER_NO_OBJECT_INDENTIFY_INFO: return TestFrameMonitor.Properties.Resources._COMPILER_NO_OBJECT_INDENTIFY_INFO;
                case ERROR_CODE._COMPILER_SUBACTION_OBJECT_ISNULL_PARA_0: return TestFrameMonitor.Properties.Resources._COMPILER_SUBACTION_OBJECT_ISNULL_PARA_0;
                case ERROR_CODE._COMPILER_SUBACTION_KEYWORD_IS_NOT_A_SUBACTION_PARA_1: return TestFrameMonitor.Properties.Resources._COMPILER_SUBACTION_KEYWORD_IS_NOT_A_SUBACTION_PARA_1;
                case ERROR_CODE._COMPILER_SUBACTION_NO_PEGININFORMATION_PARA_2: return TestFrameMonitor.Properties.Resources._COMPILER_SUBACTION_NO_PEGININFORMATION_PARA_2;
                case ERROR_CODE._COMPILER_NOT_THE_CURRENT_TESTCASE_SERVIING_PARA_4: return TestFrameMonitor.Properties.Resources._COMPILER_NOT_THE_CURRENT_TESTCASE_SERVIING_PARA_4;
                case ERROR_CODE._APP_NO_SETTING_KEYWORD_FROM: return TestFrameMonitor.Properties.Resources._APP_NO_SETTING_KEYWORD_FROM;
                case ERROR_CODE._APP_WRONG_VALUE_SETTING_KEYWORD_FROM: return TestFrameMonitor.Properties.Resources._APP_WRONG_VALUE_SETTING_KEYWORD_FROM;
                case ERROR_CODE._APP_NO_SECTION_KEWWORDS: return TestFrameMonitor.Properties.Resources._APP_NO_SECTION_KEWWORDS;
                case ERROR_CODE._KEYWORDS_SETTING_NO_SUCHAPPLICATION_SHORTNAME: return TestFrameMonitor.Properties.Resources._KEYWORDS_SETTING_NO_SUCHAPPLICATION_SHORTNAME;
                case ERROR_CODE._KEYWORDS_PARSE_NO_SUCHAPPLICATION_SHORTNAME: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_NO_SUCHAPPLICATION_SHORTNAME;
                case ERROR_CODE._KEYWORDS_PARSE_NO_SUCH_PEGWINDOW: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_NO_SUCH_PEGWINDOW;
                case ERROR_CODE._KEYWORDS_PARSE_NO_SUCH_OBJECT_UNDER_PEGWINDOW: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_NO_SUCH_OBJECT_UNDER_PEGWINDOW;
                case ERROR_CODE._KEYWORDS_PARSE_CONFIGED_OBJECT_TYPE_WRONG: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_CONFIGED_OBJECT_TYPE_WRONG;
                case ERROR_CODE._KEYWORDS_PARSE_REFLECTION: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_REFLECTION;
                case ERROR_CODE._KEYWORDS_PARSE_REFLECTION_NULL: return TestFrameMonitor.Properties.Resources._KEYWORDS_PARSE_REFLECTION_NULL;
                case ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION: return TestFrameMonitor.Properties.Resources._KEYWORDS_NO_SUCH_FUNCTION;
                case ERROR_CODE._KEYWORDS_NO_SUCH_KEYWORDS_CONCEPT_FOUND: return TestFrameMonitor.Properties.Resources._KEYWORDS_NO_SUCH_KEYWORDS_CONCEPT_FOUND;
                case ERROR_CODE._KEYWORDS_UNSUPPORT_RUN_FROM: return TestFrameMonitor.Properties.Resources._KEYWORDS_UNSUPPORT_RUN_FROM;
                case ERROR_CODE._KEYWORDS_CALL_FORMATTER_PARA_1: return TestFrameMonitor.Properties.Resources._KEYWORDS_CALL_FORMATTER_PARA_1;
                case ERROR_CODE._KEYWORDS_IF_FORMATTER_NO_VALUE_PARA_0: return TestFrameMonitor.Properties.Resources._KEYWORDS_IF_FORMATTER_NO_VALUE_PARA_0;
                case ERROR_CODE._KEYWORDS_IF_FORMATTER_SETTING_ERROR_PARA_1: return TestFrameMonitor.Properties.Resources._KEYWORDS_IF_FORMATTER_SETTING_ERROR_PARA_1;
                case ERROR_CODE._QTP_ERROR_VALIDATE: return TestFrameMonitor.Properties.Resources._QTP_ERROR_VALIDATE;
                case ERROR_CODE._QTP_ERROR_GENERAL: return TestFrameMonitor.Properties.Resources._QTP_ERROR_GENERAL;
                case ERROR_CODE._QTP_ERROR_REFRESH: return TestFrameMonitor.Properties.Resources._QTP_ERROR_REFRESH;
                case ERROR_CODE._TCDATA_NO_SUCH_ADAPTER: return TestFrameMonitor.Properties.Resources._TCDATA_NO_SUCH_ADAPTER;
                case ERROR_CODE._TCDATA_NO_SUCH_DATAFILE: return TestFrameMonitor.Properties.Resources._TCDATA_NO_SUCH_DATAFILE;
                case ERROR_CODE._TCDATA_LOAD_TABLENAMES: return TestFrameMonitor.Properties.Resources._TCDATA_LOAD_TABLENAMES;
                case ERROR_CODE._TCDATA_NO_DATA_SHEET: return TestFrameMonitor.Properties.Resources._TCDATA_NO_DATA_SHEET;
                case ERROR_CODE._TCDATA_NO_DATA_FIND: return TestFrameMonitor.Properties.Resources._TCDATA_NO_DATA_FIND;
                case ERROR_CODE._TCDATA_EXCEPTION_READFROM_ONEROW: return TestFrameMonitor.Properties.Resources._TCDATA_EXCEPTION_READFROM_ONEROW;
                case ERROR_CODE._TCDATA_EXCEPTION_LOADDATA: return TestFrameMonitor.Properties.Resources._TCDATA_EXCEPTION_LOADDATA;
                case ERROR_CODE._TCDATA_DATA_COLUMN_EXCEED: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_COLUMN_EXCEED;
                case ERROR_CODE._TCDATA_DATA_CELL_EXCEPTION_PARA_3: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_CELL_EXCEPTION_PARA_3;
                case ERROR_CODE._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2;
                case ERROR_CODE._TCDATA_DATA_UPDATE_EXCEPTION_PARA_3: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_UPDATE_EXCEPTION_PARA_3;
                case ERROR_CODE._TCDATA_DATA_UPDATECACHE_LOOPID_LESS_0_PARA_2: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_UPDATECACHE_LOOPID_LESS_0_PARA_2;
                case ERROR_CODE._TCDATA_DATA_UPDATECACHE_LOOPID_GREATER_PARA_3: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_UPDATECACHE_LOOPID_GREATER_PARA_3;
                case ERROR_CODE._TCDATA_DATA_UPDATECACHE_NO_OBJECTINDEX_PARA_1: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_UPDATECACHE_NO_OBJECTINDEX_PARA_1;
                case ERROR_CODE._TCDATA_DATA_INSERT_OBJECT_EXCEPTION_PARA_3: return TestFrameMonitor.Properties.Resources._TCDATA_DATA_INSERT_OBJECT_EXCEPTION_PARA_3;
                case ERROR_CODE._QTP_ERROR_INSTANCE_NULL: return TestFrameMonitor.Properties.Resources._QTP_ERROR_INSTANCE_NULL;
                case ERROR_CODE._QTP_ERROR_CREATE_EMPTYTEST: return TestFrameMonitor.Properties.Resources._QTP_ERROR_CREATE_EMPTYTEST;
                case ERROR_CODE._QTP_ERROR_SETTINGADDINS: return TestFrameMonitor.Properties.Resources._QTP_ERROR_SETTINGADDINS;
                case ERROR_CODE._SERVICE_ERROR_OBJECT_TYPE_IS_NOT_TEST_STEP: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_OBJECT_TYPE_IS_NOT_TEST_STEP;
                case ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_HOST_SETTING;
                case ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_PROTOCO_SETTING;
                case ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_PORT_SETTING;
                case ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_SERVICENAME_SETTING;
                case ERROR_CODE._SERVICE_ERROR_NO_SERVICE_START_UNKNOW: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_SERVICE_START_UNKNOW;
                case ERROR_CODE._SERVICE_ERROR_NO_SERVICE_STOP_UNKNOW: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_SERVICE_STOP_UNKNOW;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_UNKNOW;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_CANT_END_NAVIGATE: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_CANT_END_NAVIGATE;
                case ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                case ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                case ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTCASENAME_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_SERVER_NO_TESTCASENAME_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_OBJECTNAME_ISNULL_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_OBJECTNAME_ISNULL_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_KEYWORDNAME_ISNULL_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_KEYWORDNAME_ISNULL_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_DATA_ISNULL_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_DATA_ISNULL_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_LOOP_ISNULL_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_LOOP_ISNULL_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_LOOP_ISNOT_A_NUMBER_PARA_1: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_LOOP_ISNOT_A_NUMBER_PARA_1;
                case ERROR_CODE._SERVICE_ERROR_CLIENT_SWITCH_DATA_NO_FILENAME_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CLIENT_SWITCH_DATA_NO_FILENAME_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_IF_SUBOBJECT_REQUIRED_PARA_1: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_IF_SUBOBJECT_REQUIRED_PARA_1;
                case ERROR_CODE._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1;
                case ERROR_CODE._SERVICE_ERROR_NO_COMPILER_ASSIGNED_PARA_2: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_COMPILER_ASSIGNED_PARA_2;
                case ERROR_CODE._SERVICE_ERROR_NO_TESTCASE_GETTER_ASSIGNED_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_TESTCASE_GETTER_ASSIGNED_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_NO_TESTSUITE_NAVIGATE_ASSIGNED_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_TESTSUITE_NAVIGATE_ASSIGNED_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_NO_GETNEXTTESTSUITE_ASSIGNED_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_NO_GETNEXTTESTSUITE_ASSIGNED_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_CALL_BEGIN_NAVIGATEFIRST_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_CALL_BEGIN_NAVIGATEFIRST_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_SERVER_ASSIGN_LOADTESTSUITE_FIRST_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_SERVER_ASSIGN_LOADTESTSUITE_FIRST_PARA_0;
                case ERROR_CODE._SERVCIE_ERROR_NO_TESTSUITE_IS_DONE_STATUS_ASSIGNED_PARA_0: return TestFrameMonitor.Properties.ServiceResource._SERVCIE_ERROR_NO_TESTSUITE_IS_DONE_STATUS_ASSIGNED_PARA_0;
                case ERROR_CODE._SERVICE_ERROR_PARAMETER_SHOULDBE_INT_PARAP_1: return TestFrameMonitor.Properties.ServiceResource._SERVICE_ERROR_PARAMETER_SHOULDBE_INT_PARAP_1;
                case ERROR_CODE._JET_DB_CONNECTOIN_OPEN_PARA_1: return TestFrameMonitor.Properties.Resources._JET_DB_CONNECTOIN_OPEN_PARA_1;
                case ERROR_CODE._JET_DB_CMD_EXPAND_COLUMN_PARA_1: return TestFrameMonitor.Properties.Resources._JET_DB_CMD_EXPAND_COLUMN_PARA_1;
                case ERROR_CODE._JET_DB_GET_COLUMN_EXCEPTION_PARA_1: return TestFrameMonitor.Properties.Resources._JET_DB_GET_COLUMN_EXCEPTION_PARA_1;
                case ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2: return TestFrameMonitor.Properties.Resources._JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2;
                case ERROR_CODE._JET_DB_NO_EXPAND_COLUMN_SUPPORT_PARA_1: return TestFrameMonitor.Properties.Resources._JET_DB_NO_EXPAND_COLUMN_SUPPORT_PARA_1;
                case ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_QUERY_PARA_2: return TestFrameMonitor.Properties.Resources._JET_DB_EXCEPTION_WHEN_RUN_QUERY_PARA_2;
                case ERROR_CODE._JET_DB_FIELDNAMES_DNTMATCH_VALUES_PARA_2: return TestFrameMonitor.Properties.Resources._JET_DB_FIELDNAMES_DNTMATCH_VALUES_PARA_2;

                case ERROR_CODE._CLIENT_ERROR_PARAMETERISNULL_PARA_1: return TestFrameMonitor.Properties.ServiceResource._CLIENT_ERROR_PARAMETERISNULL_PARA_1;
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

    public enum Keyword_RunType
    {
        _NORMAL_RUNTYPE = 0x00,
        _SUBACTION_RUNTYPE_NORMAL = 0x01,
        _SUBACTION_RUNTYPE_SELECTION = 0x02,
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

        #region ObjectDictionary File
        public const string CNST_XLS_HEADER_DIC_OBJECTHAPPYNAME = "Object";
        public const string CNST_XLS_HEADER_DIC_OBJECTIDENTIFIER = "identify";
        public const string CNST_XLS_HEADER_DIC_EXPAND = "Expand Information";
        public const string CNST_XLS_HEADER_DIC_COMMENT = "Comment";
        #endregion

        #region App.config
        public const string CNST_APPCONFIG_SECITON_XLSCONFGMODE = "XlsConfigMode";
        public const string CNST_APPCONFIG_SECTION_XLSMODE_ROOT = "RootPath";
        public const string CNST_APPSETTING_APPLICATION_REG_MODE = "RegistedApplicationsMode";
        public const string CNST_APPSETTING_APPLICATION_REG_MODE_VALUE_CONFIG = "config";
        public const string CNST_APPSETTING_APPLICATION_REG_MODE_VALUE_DB = "database";
        public const string CNST_APPCONFIG_APPREG_ATTR_NAME = "AppName";
        public const string CNST_APPCONFIG_APPREG_ATTR_COMMAND = "command";
        public const string CNST_APPCONFIG_APPREG_ATTR_PATH = "path";
        public const string CNST_APPCONFIG_APPREG_ATTR_IDENTIFIER = "identifier";
        public const string CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE = "ApplicationType";
        public const string CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE_DEFAULT = "window";
        public const string CNST_APPCONFIG_APPREG_ATTR_OBJECTPATH = "ObjectFile";
        public const string CNST_APPCONFIG_APPSETTING_OBJECTSOURCE = "ObjectSource";
        public const string CNST_APPCONFIG_APPSETTING_OBJECTSOURCE_XLS = "xlsFile";
        public const string CNST_APPCONFIG_APPSETTING_OBJECTSOURCE_DB = "dataBase";
        public const string CNST_APPCONFIG_APPSETTING_DEFAULTAPP = "DefautlApplication";
        public const string CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE = "KeywordSource";
        public const string CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE_CONFIG = "config";
        public const string CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE_DB = "database";
        public const string CNST_APPCONFIG_APPSETTING_DEBUGMODE = "DebugMode";
        public const string CNST_APPCONFIG_APPSETTING_DEBUGMODE_VBS = "VBS";
        public const string CNST_APPCONFIG_APPSETTING_DEBUGMODE_NONE = "NONE";
        public const string CNST_APPCONFIG_APPSETTING_TCDATASOURCE = "TCDataSource";
        public const string CNST_APPCONFIG_APPSETTING_TCDATASOURCE_XLS = "xlsFile";
        public const string CNST_APPCONFIG_APPSETTING_BASELINEMODE = "BaseLineMode";
        public const string CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD = "Build";
        public const string CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE = "Comparison";
        public const string CNST_APPCONFIG_KEYWORDS_ATTR_KEY = "key";
        public const string CNST_APPCONFIG_KEYWORDS_ATTR_APPLIEDAPPS = "AppliedApplications";
        public const string CNST_APPCONFIG_KEYWORDS_ATTR_RUNFROM = "RunFrom";
        public const string CNST_APPCONFIG_KEYWORDS_ATTR_PARAM_PARSE = "ParameterParse";
        public const string CNST_APPCONFIG_KEYWORDS_ATTR_KEY_DEFAULT = "NON-KEYWORDS-SETTING";
        public const string CNST_APPCONFIG_SECTION_KEYWORDS = "Keywords";
        public const string CNST_APPCONFIG_KEYWORDS_APP_VALUE_ALL = "All";
        public const string CNST_APPCONFIG_KEYWORDS_APP_VALUE_ALLWINDOWS = "All_Windows";
        public const string CNST_APPCONFIG_KEYWORDS_RUN_FROM_QTP = "Qtp";
        public const string CNST_APPCONFIG_KEYWORDS_RUN_FROM_UTF = "UTF";
        public const string CNST_APPCONFIG_SECTION_UFT_INIT_SCRIPTS = "UFTInitScripts";
        public const string CNST_APPCONFIG_SECTION_CURRENT_UFT_ADDINS = "CurrentUFTAddins";
        public const string CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE = "FrameWorkService";

        #endregion

        #region System Reserved Keywords
        public const string CNST_RESERVED_KEYWORD_SETCURRENTAPPLICATION = "SetCurrentApplication";
        public const string CNST_RESERVED_KEYWORD_PEGWINDOW = "PegWindow";
        public const string CNST_RESERVED_KEYWORD_LOOP = "Loop";
        public const string CNST_RESERVED_KEYWORD_ENDLOOP = "EndLoop";
        public const string CNST_RESERVED_KEYWORD_CAPTUREVALUE = "CaptureValue";
        public const string CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE = "CaptureAndCompare";
        public const string CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY = "CaptureAndCompareByKey";
        public static string[] CNST_ARR_FEEDBACKFUNCTIONS = new string[] { CNST_RESERVED_KEYWORD_CAPTUREVALUE, CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE, CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY };

        #region subActions
        public const string CNST_SUBACTION_KEYWORD_CALL = "CALLSUBTEST";
        public const string CNST_SUBACTION_KEYWORD_DEALERALLOCATION = "DEALERALLOCATION";
        public const string CNST_SUBACTION_KEYWORD_BUSINESSALLOCATION = "BUSINESSALLOCATION";
        public const string CNST_SUBACTION_KEYWORD_IF = "IF";
        public static string[] CNST_ARR_KEYWORD_SUBACTIONS = new string[] { CNST_SUBACTION_KEYWORD_CALL, CNST_SUBACTION_KEYWORD_DEALERALLOCATION, CNST_SUBACTION_KEYWORD_BUSINESSALLOCATION, CNST_SUBACTION_KEYWORD_IF };

        public const string CNST_ALLOCATION_TESTSUITENAME = "SubActions_DealerAlloc.xls";
        public const string CNST_DEALERALLOCATION_TESTCASE = "DealerAllocation";
        public const string CNST_BUSINESSALLOCATION_TESTCASE = "BusinessAllocation";
        #endregion //SubActions
        #endregion

        #region KeywordsConcept
        public const string CNST_KEYWORD_CONCEPT_PREFIX_QTP = "_QTP_KEYWORDS_{0}";
        #endregion


        #region Section Service
        public const string CNST_SERVICE_KEY_URL_HOST = "HOST";
        public const string CNST_SERVICE_KEY_URL_PROTOCOL = "PROTOCOL";
        public const string CNST_SERVICE_KEY_URL_PORT = "PORT";
        public const string CNST_SERVICE_KEY_URL_SERVICENAME = "SERVICENAME";
#if _VEDIO_TIGER_
        public const string CNST_SERVICE_KEY_VEDIO = "_VEDIO_TIGER_";
#endif
        #endregion

        #region Data file
        public const string CNST_DATA_FIELD_OBJECT = "ObjectName";
        public const string CNST_DATA_FIELD_TEST_PREFIX = "T";
        public const string CNST_DATA_RPT_SHEETNAME_PREFIX = "MarsComparisonRpt";
        public const string CNST_DATA_RPT_DEFAULT_FIELDNAME_OBJECT = "ObjectName";
        public const string CNST_DATA_RPT_DEFAULT_FIELDNAME_APPLICATION_1 = "First_Application";
        public const string CNST_DATA_RPT_DEFAULT_FIELDNAME_APPLICATION_2 = "Second_Application";
        public const string CNST_DATA_RPT_DEFAULT_FIELDNAME_RESULT = "Compare_Result";
        public const string CNST_DATA_RPT_DEFAULT_HEADVALUE_OBJECT = "Object Name";
        public const string CNST_DATA_RPT_DEFAULT_HEADVALUE_APP_1 = "First Application";
        public const string CNST_DATA_RPT_DEFAULT_HEADVALUE_APP_2 = "Second Application";
        public const string CNST_DATA_RPT_DEFAULT_HEADVALUE_RESULT = "Comparison Result";
        #endregion 

        #region Enhanced Keyword
        public const string CNST_ENHANCE_PEG_RUNTIME_PREFIX = "MultiplePane:Runtime:";
        public const string CNST_ENHANCE_VALUE_EQUALTO_PREFIX = "equals:";
        public const string CNST_ENHANCE_STORAGEMODE_COMPARISON_REGULAR = @"^\[StorageMode:Comparing;ColIndx:\d{1};ConvertMethod:\S*\];";
        public const string CNST_ENHANCE_STORAGEMODE_COMPARISON_PREFIX = @"[StorageMode:Comparing;ColIndx:";
        public const string CNST_ENHANCE_STORAGEMODE_COMPARISON_CONVERT = @";ConvertMethod";
        public const string CNST_ENHANCE_IF_FORMATTER_REGULOR_EX = @"RowCount(>|<|=|>=|<=){1}\d{1,2}\?return\=(false|true)\:{1}clickButton\[{1}\S+\]";
        public const string CNST_IF_REGQULOR_PARSE = @"(>|<|=|>=|<=){1}(\d+)\?return\=(true|false):(\S+)\[(\S+)\]";



        #endregion

        #region Test WCF Client
        public const string CNST_CLIENT_GRID_KEYWORD = "K:";
        public const string CNST_CLIENT_GRID_OBJECT = "O:";
        public const string CNST_CLIENT_GRID_RC = "R&C:";
        public const string CNST_CLIENT_GRID_VALUE = "D/V:";
        public readonly static int CNST_CLIENT_GRID_HINT_MXLENGHT = Math.Max(Math.Max(CNST_CLIENT_GRID_KEYWORD.Length, CNST_CLIENT_GRID_OBJECT.Length), Math.Max(CNST_CLIENT_GRID_RC.Length, CNST_CLIENT_GRID_VALUE.Length));
        #endregion

        #region Monitor Command
        public const string CNST_MONITOR_RUNFROM = "Run From";
        public const string CNST_MONITOR_BREAKS = "Breakpoints";
        public const string CNST_MONITOR_SKIP = "skip Steps";
        #endregion //Monitor Command
    }

    public enum MARS_ADAPTER
    {
        _ADPTR_NOT_DEF = 0x00,

        #region Test suite adapter
        _ADPTR_XLSJET_2_TESTSUITE = 0x01,
        #endregion

        #region Test step adapter
        _DAPTR_XLSJET_2_TESTSTEP = 0x10,
        #endregion

        #region Reg application adapter
        _ADPTR_APP_SETTING_SHORTNAME = 0x20,
        _ADPTR_APP_SETTING_FULLPATH = 0x21,
        _ADPTR_APP_SETTING_NONE = 0x22,
        #endregion

        #region Objects dictionary
        _ADPTR_OBJECTS_LOAD_FROM_XLS = 0x30,
        #endregion

        #region Keywords
        _ADPTR_KEYWORD_CONFIG = 0x40,
        #endregion
        #region TCDataSource
        _ADPTR_TCDATASOURCE_XLS = 0x50,
        #endregion
    }

}
