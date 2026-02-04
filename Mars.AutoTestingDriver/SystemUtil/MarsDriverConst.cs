namespace Mars.AutoTestingDriver.SystemUtil
{
    public sealed class MarsDriverConst
    {
        public const string CURRENT_APPLICATION = "CURRENT_APPLICATION";

        public const int exit_code_error_executeTestCaseFailed = 0,
                         exit_code_error_paraFormat            = -0x02;
        public const int exit_code_error_noUserInfoInPara      = -0x03,
                         exit_code_error_cantStartMonitor      = -0x04,
                         exit_code_error_cantCreateMarsAccountFile = -0x05,
                         exit_code_error_noTestSteps           = -0x06,
                         exit_code_error_LoadTest_Failed       = -0x07,
                         exit_code_error_cantGetConfigApps     = -0x08,
                         exit_code_error_cantChangeConfigFile  = -0x09,
                         exit_code_error_loadConfigException   = -0x0A,
                         exit_code_error_cantCreateAtom        = -0x0B,
                         exit_code_error_mainException         = -0xFF;


        public const int exit_code_ExeTestFromCI_Ok            = 0x21,
                         exit_code_ExeTestFromCI_Failed        = -0x21,
                         exit_code_ExeTestFromJsonClipbord_Ok  = 0x22,
                         exit_code_ExeTestFromJsonClipbord_Failed = -0x22;

        public const int exit_code_ExecuteTestCase_Ok          = 1;
        public const int exit_code_Spy_ok                      = 0x11;
        public const int exit_code_ExeAsInject_Ok              = 0x02;
        public const int exit_code_LoadTest_Ok                 = 0x03;
        public const int exit_code_InjectToDlgStart32_Ok       = 0x04;
    }
}
