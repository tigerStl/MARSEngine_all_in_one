using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Inter.MQCenter.simpleLog;
using MarsUFTAddins.IMars.tiger;

namespace Mars.Inter.MQCenter.DataStructure
{

    public class MarsObjectTypeMappingManagment
    {
        public const string cnst_mappingfileName = "marsTypeTextMappingCfg.json";
        private MarsObjectTypeMappingRoot marsObjTypeMappingInst = null;
        private static MarsObjectTypeMappingManagment marsObjectTypeMappingManagment = null;
        public static MarsObjectTypeMappingManagment GetObjTypeMappingInst(ref bool isOk, ref string strError, ref string strStack, bool isForceToUpdate = false)
        {
            MarsLoggerSimple.logBegin("MarsObjectTypeMappingManagment.GetObjTypeMappingInst");
            if (isForceToUpdate) marsObjectTypeMappingManagment = null;
            if (marsObjectTypeMappingManagment != null)
                return marsObjectTypeMappingManagment;
            /// 获得文件位置。默认在./config/marsObjTypeMappingText.json 
            ///             
            var fn = GetDefaultConfigFileName(ref isOk);
            if (!isOk)
            {
                strError = $"no such file exists|{fn}";
                MarsLoggerSimple.Error("MarsObjectTypeMappingManagment.GetObjTypeMappingInst", strError);
                return null;
            }
            try
            {
                marsObjectTypeMappingManagment = new MarsObjectTypeMappingManagment();
                var txt = System.IO.File.ReadAllText(fn);
                marsObjectTypeMappingManagment.marsObjTypeMappingInst = Newtonsoft.Json.JsonConvert.DeserializeObject<MarsObjectTypeMappingRoot>(txt);
                isOk = true;
                return marsObjectTypeMappingManagment;

            }
            catch (Exception e)
            {
                strError = $"Can't load MARS config file|{fn}";
                strStack = e.StackTrace;
                MarsLoggerSimple.Error("MarsObjectTypeMappingManagment.GetObjTypeMappingInst", $"{strError}|{e.Message}");
                marsObjectTypeMappingManagment = null;
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MarsObjectTypeMappingManagment.GetObjTypeMappingInst", $"returns|isOK|{isOk}|");
            }
        }

        private static string GetDefaultConfigFileName(ref bool isOk)
        {
            var fullpath = typeof(MarsObjectTypeMappingManagment).Assembly.Location;
            var fn = System.IO.Path.GetDirectoryName(fullpath);
            fn = System.IO.Path.Combine(fn, "config", cnst_mappingfileName);
            if (!System.IO.File.Exists(fn))
            {
                isOk = false;
                return null;
            }
            return fn;
        }

        internal bool FindNodeByPegNameAndType(object objControl, string strTypeName, string strPegName, ref string strError, ref string strAdv, ref string strDataReturned)
        {
            MarsLoggerSimple.logBegin("FindNodeByPegNameAndType", $"{strTypeName}|{strPegName}");
            try
            {
                if (string.IsNullOrEmpty(strTypeName))
                {
                    strError = "object type name is required";
                    return false;
                }
                if ((marsObjTypeMappingInst == null) || (marsObjTypeMappingInst.imageButtonConvert == null))
                {
                    MarsLoggerSimple.Error("FindNodeByPegNameAndType", strError = "Config file is not loaded and ensure the mapping objects exist.");
                    return false;
                }
                var nodes = marsObjTypeMappingInst.imageButtonConvert
                    .Where(p => (p.type != null)
                        && (p.type.Equals(strTypeName, StringComparison.OrdinalIgnoreCase))
                        && (!string.IsNullOrEmpty(p.propertyName))
                        && (!string.IsNullOrEmpty(p.objectHappyName))
                        && (p.objectHappyName.Equals(strPegName, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();
                if (nodes == null)
                {
                    // no config the object
                    strAdv = $"Please contact Marquis to enable type|{strTypeName}| and parent window|{strPegName}| for the image mode";
                    strError = $"MARS CAN'T support this type |{strTypeName} so far";
                    MarsLoggerSimple.Error("MarsObjectTypeMappingManagment.FindNodeByPegNameAndType", $"{strError}|{strAdv}");
                    return false;
                }
                /// 使用 反射获得对象信息
                /// 
                bool isNotExist = true;
                var objV = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(objControl, nodes.propertyName, ref isNotExist);
                if (isNotExist)
                {
                    strAdv = $"Please contact Marquis to ensure that |{nodes.propertyName}| has |{strTypeName}|";
                    strError = $"Can't find property||{nodes.propertyName} from |{strTypeName}";
                    MarsLoggerSimple.Error("FindNodeByPegNameAndType", $"{strAdv}|{strError}");
                    return false;
                }
                if (objV == null)
                {
                    strAdv = $"Please contact Marquis to ensure that |{nodes.propertyName}| has |{strTypeName}| and has right values";
                    strError = $"No value for |{nodes.propertyName}|";
                    MarsLoggerSimple.Error("FindNodeByPegNameAndType", $"{strAdv}|{strError}");
                    return false;
                }
                string v = objV.ToString();
                var rslt = nodes.valueMapping.FirstOrDefault(p => (p != null) && (!string.IsNullOrEmpty(p.v)) && (p.v.Equals(v, StringComparison.OrdinalIgnoreCase)));
                if (rslt == null)
                {
                    strAdv = $"Please contact Marquis to ensure that |{nodes.propertyName}| has |{strTypeName}| and has been configured well for |{v}|";
                    strError = $"No value for |{v}| under |{nodes.objectHappyName}|, type|{strTypeName}| is well configured";
                    MarsLoggerSimple.Error("FindNodeByPegNameAndType", $"{strAdv}|{strError}");
                    return false;
                }
                strDataReturned = rslt.t;
                return true;
            }
            catch (Exception ex)
            {
                strError = $"can't mapping the objects";
                strAdv = "";
                MarsLoggerSimple.Error("FindNodeByPegNameAndType", $"{ex.Message}", ex);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindNodeByPegNameAndType", strError);
            }
        }
    }

    /// <summary>
    /// sample json :
    /** {
	"imageButtonConvert":[
		{
			"type":"Summit.Framework.View.ImageButtonControl",
			"propertyName":"Checked",
			"valueMapping":[
				{
					"pegwindowName":"BOND_TRADE",
					"mappings":[
						{
							"v":"true",
							"t":"buy"

                        },
						{
    "v":"false",
							"t":"buy"

                        }
					]
				}
			]
		}
	]
}
     </summary>
    **/
    public class MarsObjectTypeMappingRoot
    {
        public List<MarsImageButtonConvert> imageButtonConvert { get; set; }
    }

    public class MarsImageButtonConvert
    {
        public string type { get; set; }
        public string propertyName { get; set; }
        public string objectHappyName { get; set; }
        public List<MarsValueAndTextMapping> valueMapping { get; set; }
    }

    public class MarsValueAndTextMapping
    {
        public string v { get; set; } // value, for example "yes"
        public string t { get; set; } // text
    }


}
