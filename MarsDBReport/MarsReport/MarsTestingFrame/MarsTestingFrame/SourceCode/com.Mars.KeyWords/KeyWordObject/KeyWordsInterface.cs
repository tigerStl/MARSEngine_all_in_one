using com.Mars.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject
{
    public interface IKeyWordParse
    {
        bool IsRightFormatForObject(string strApplicationShortName, string strPegWindowName, string strObjHappyName, ref ERROR_CODE eCode);
        bool IsRightFormatForRowAndColumn(string strApplicationShortName, string strPegWindowName, string strObjHappyName, string strRC, ref ERROR_CODE eCode);
        bool IsRightFormatForDataUnderScript(string strValue_RC, ref ERROR_CODE eCode);
        E_KeywordParameterID IsParameterRequired();
        
    }

    public enum E_KeywordParameterID
    {
        e_Keyword_Parameter_None = 0x00,
        e_Keyword_Parameter_object = 0x01,
        e_Keyword_Parameter_RC=0x02,
        e_Keyword_Parameter_Value=0x04,
        e_Keyword_Parameter_All = e_Keyword_Parameter_object | e_Keyword_Parameter_RC | e_Keyword_Parameter_Value
    }
}
