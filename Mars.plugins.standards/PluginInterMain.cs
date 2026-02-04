#if _noMesssageNamespace
namespace Mars.plugins.standards
#else 
namespace Mars.message.plugins.standards
#endif
{
    public enum EMars_PluginSensitive
    {
        E_SensitiveFor_None = 0x00,
        E_SensitiveFor_BeforeGetDataSet = 0x01,
        E_SensitiveFor_AfterGetDataSet = 0x02,
        E_SensitiveFor_BeforeRecodeDataToDB = 0x04,
        E_SensitiveFor_AfterRecodeDataToDB = 0x08,
        E_SensitiveFor_BeforeGetRC = 0x10,
        E_SensitiveFor_AfterGetRC = 0x20
    }
    public static class EMars_PluginSensitiveMethods
    {
        public static string GetString(this EMars_PluginSensitive ePluginsType)
        {
            switch (ePluginsType)
            {
                case EMars_PluginSensitive.E_SensitiveFor_None: return "";
                case EMars_PluginSensitive.E_SensitiveFor_AfterGetDataSet: return "E_SensitiveFor_AfterGetDataSet";
                case EMars_PluginSensitive.E_SensitiveFor_AfterGetRC: return "E_SensitiveFor_AfterGetRC";
                case EMars_PluginSensitive.E_SensitiveFor_AfterRecodeDataToDB: return "E_SensitiveFor_AfterRecodeDataToDB";
                case EMars_PluginSensitive.E_SensitiveFor_BeforeGetDataSet: return "E_SensitiveFor_BeforeGetDataSet";
                case EMars_PluginSensitive.E_SensitiveFor_BeforeGetRC: return "E_SensitiveFor_BeforeGetRC";
                case EMars_PluginSensitive.E_SensitiveFor_BeforeRecodeDataToDB: return "E_SensitiveFor_BeforeRecodeDataToDB";
                default: return "";
            }
        }
    }
    public interface I_MarsPluginsStandard4Data
    {
        EMars_PluginSensitive isSensitiveFor();
        //void PutSensitiveInformation(EMars_PluginSensitive eSensitive, string strSensitiveDataIdx);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensitiveType"></param>
        /// <param name="strKeyword"> </param>
        /// <param name="strObjectName"></param>
        /// <param name="strPegName"></param>
        /// <param name="strRC"></param>
        /// <param name="strDataSrc"></param>
        /// <param name="strTargetData"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        bool GetSensitiveData(EMars_PluginSensitive sensitiveType,
            string strKeyword,
            string strObjectName,
            string strRC,
            string strDataSrc, ref string strTargetData, ref string strError,
            string strPegName = "");

    }

    public abstract class AbstractClass_MarsPluginsStandard : I_MarsPluginsStandard4Data
    {
        public abstract bool GetSensitiveData(EMars_PluginSensitive sensitiveType, string strKeyword,
            string strObjectName,
            string strRC,
            string strDataSrc, ref string strTargetData, ref string strError,
            string strPegName = "");


        public abstract EMars_PluginSensitive isSensitiveFor();

        //public abstract void PutSensitiveInformation(EMars_PluginSensitive eSensitive, string strSensitiveDataIdx);
    }

    public enum MARS_DATA_NORMALIZATION
    {
        MARS_NONE,
        MARS_FLOAT20,
        MARS_DATE_FROM_YYYYMMDD,
        MARS_DATE_FROM_YYYYMMDD_SLASH,
        MARS_DATE_FROM_YYYYMMDD_DASH,
        MARS_DATE_FROM_DDMMYYYY,
        MARS_DATE_FROM_DDMMYYYY_SLASH,
        MARS_DATE_FROM_DDMMYYYY_DASH,
    }
    internal sealed class MARSDATA_NORMALIZATION
    {
        public static MARS_DATA_NORMALIZATION FromString(string strEnum)
        {
            if (string.IsNullOrEmpty(strEnum)) return MARS_DATA_NORMALIZATION.MARS_NONE;

            if (string.Compare("FLOAT20", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_FLOAT20;
            if (string.Compare("YYYYMMDD", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_YYYYMMDD;
            if (string.Compare("YYYY/MM/DD", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_YYYYMMDD_SLASH;
            if (string.Compare("YYYY-MM-DD", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_YYYYMMDD_DASH;
            if (string.Compare("DDMMYYYY", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_YYYYMMDD;
            if (string.Compare("DD-MM-YYYY", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_DDMMYYYY_DASH;
            if (string.Compare("DD/MM/YYYY", strEnum, true) == 0)
                return MARS_DATA_NORMALIZATION.MARS_DATE_FROM_DDMMYYYY_SLASH;
            return MARS_DATA_NORMALIZATION.MARS_NONE;
        }
    }

    public delegate bool MarsVerifyFunc(string valueToBeVerify, string strDataSrc, ref object ResultAfterNormalization, ref string strError);
    public delegate string MarsNormalizationData(string strDataToNormalization, string strNormalizationType, ref bool isOk, ref string strError);
    public interface IMarsVerifyFunc
    {
        MarsVerifyFunc GetVerifyFunc();
    }


}
