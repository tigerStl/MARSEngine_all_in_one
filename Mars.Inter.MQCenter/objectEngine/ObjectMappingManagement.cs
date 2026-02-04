#if !(_source_dll || _sub_dll )
/// 因为使用system.text.json.dll,会造成系统冲突，所以，在.netframework下，使用Newtonsoft.Json.dll
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace Mars.Inter.MQCenter.objectEngine
{
    public enum MarsStepsInsertOption
    {
        AppendToTheEnd,
        InsertAfterWithoutNewPeg,
        InsertAfterWithNewPeg
    }

    internal class ObjectMappingManagement
    {
    }

    public class MarsTypeAndEnvent
    {
        public List<string> mouseDownEvent { get; set; }
        public List<string> mouseUpEvent { get; set; }
    }

    public class MARSWEbMapping
    {
        public List<string> PreviewScanType { get; set; }
        public List<ObjectEngine_Type_webTypeXpathKeyword> TypesAndKeywords { get; set; }
    }

    public class ObjectEngineConfigFile
    {
        public List<string> ignoreTypes { get; set; }
        public List<string> twoStepsTypes { get; set; }
        public List<ObjectEngine_TypeMappingDotNet> marsTypeMapping_dotNet { get; set; }
        public MARSWPF_MappingObject marsTypeMapping_wpf_core { get; set; }
        public MARSWEbMapping marsTypeMapping_web_core { get; set; }
        public MarsStepsInsertOption stepsInsertOption { get; set; }
        public bool IsOnlyGenerateObjectInsideContainer { get; set; }
        public MarsTypeAndEnvent typeAndEnvent { get; set; }

        public List<string> standardMarsControlTypes { get; set; }

        public string getObjectTypeByTypePathForCore(string strTypPath, ref bool isOk, ref string strError, ref string strMARSType)
        {
            if (string.IsNullOrEmpty(strTypPath))
            {
                isOk = false;
                strError = "No type(core) path is passed";
                return null;
            }
            //if (marsObjectDotNetEngineFile == null)
            //{
            //    isOk = false;
            //    strError = "Please ensure that the config file exists and is loaded";
            //    return null;
            //}

            var objMap = marsTypeMapping_wpf_core.TypesAndKeywords
                .FirstOrDefault(p => p.controlType.Any(x => strTypPath.Equals(x,StringComparison.OrdinalIgnoreCase)));
            if (objMap != null)
            {
                if (string.IsNullOrEmpty(objMap.defaultKeywords.FirstOrDefault()))
                {
                    isOk = false;
                    strError = $"No Keywords is assigned to |{objMap.controlType.ToArray()}";
                    return null;
                }
                isOk = true;
                strMARSType = objMap.marsType;
                return objMap.defaultKeywords.FirstOrDefault()!;
            }
            strError = $"the type is not supported so far |{strTypPath}|";
            isOk = false;
            return null;
        }

        public string getObjectTypeByTypePath(string strTypPath, ref bool isOk, ref string strError)
        {
            if (string.IsNullOrEmpty(strTypPath))
            {
                isOk = false;
                strError = "No type path is passed";
                return null;
            }
            //if (marsObjectDotNetEngineFile == null)
            //{
            //    isOk = false;
            //    strError = "Please ensure that the config file exists and is loaded";
            //    return null;
            //}

            var objMap = marsTypeMapping_dotNet
                .FirstOrDefault(p => p.controlType.Any(x => strTypPath.IndexOf(x) >= 0));
            if (objMap != null)
            {
                if (string.IsNullOrEmpty(objMap.defaultKeywords.FirstOrDefault()))
                {
                    isOk = false;
                    strError = $"No Keywords is assigned to |{objMap.controlType.ToArray()}";
                    return null;
                }
                isOk = true;
                return objMap.marsType;
            }
            strError = $"the type is not supported so far |{strTypPath}|";
            isOk = false;
            return null;
        }
    }

    public class MARSWPF_MappingObject
    {
        public List<ObjectEngine_TypeMappingDotNet> TypesAndKeywords { get; set; }
        public List<string> IgnoreWpfTypes { get; set; }

        public bool IsIgnoreWpfTypes(string typeName)
        {
            if ((IgnoreWpfTypes == null) || (IgnoreWpfTypes.Count <= 0)) return false;
            return IgnoreWpfTypes.Contains(typeName);
        }
    }

    public class ObjectEngine_TypeMappingDotNet
    {
        public string marsType { get; set; }
        public List<string> controlType { get; set; }
        public List<string> defaultKeywords {
            get;
            set;
        }
    }

    public class ObjectEngine_Type_webTypeXpathKeyword
    {
        public string marsType { get; set; }
        public List<string> webXPaths { get; set; }
        public string webColumnHeaderTextProperty { get; set; } // 获取头部text的属性 比如col-id, 
        public List<string> defaultKeywords
        {
            get;
            set;
        }
    }

    

    public class ObjectEngineConfigFileManagement
    {
        private static string targetFile = "";
        private static ObjectEngineConfigFile marsObjectDotNetEngineFile = null;

        private static List<string>? allBasicEditComboButtonTyps = null ;

        public static List<string>? AllBasicEditComboButtonTyps
        {
            get
            {
                if (allBasicEditComboButtonTyps == null)
                {
                    ObjectEngineConfigFile cfgFile  = GetEngineObject();
                    allBasicEditComboButtonTyps     = cfgFile.marsTypeMapping_wpf_core.TypesAndKeywords
                        .Where(x => x.marsType.Equals("SwfEdit", StringComparison.OrdinalIgnoreCase)
                            || x.marsType.Equals("SwfCombobox", StringComparison.Ordinal))
                        .SelectMany(x => x.controlType)
                        .Distinct()
                        .ToList();
                }
                return allBasicEditComboButtonTyps;
            }
        }

        public static bool IsIgnoredTypes(string typ)
        {
            if (string.IsNullOrEmpty(typ)) return false;
            if (marsObjectDotNetEngineFile == null)
            {
                GetEngineObject();                
            }
            if (marsObjectDotNetEngineFile == null) return false;
            if (marsObjectDotNetEngineFile.marsTypeMapping_wpf_core == null) return false;
            if (marsObjectDotNetEngineFile.marsTypeMapping_wpf_core.IgnoreWpfTypes.IndexOf(typ) >= 0)
                return true;
            return false;
        }

        public static List<string> GetStandardsObjectsTypes() {
            if (marsObjectDotNetEngineFile == null)
            {
                GetEngineObject();
            }
            if (marsObjectDotNetEngineFile == null) return new List<string>();
            return marsObjectDotNetEngineFile.standardMarsControlTypes;
        }


        public static ObjectEngineConfigFile LoadFromConfigFile(string filePath, 
            ref string strError, 
            ref bool isOk)
        {
            try
            {
                string dirInfo = typeof(ObjectEngineConfigFileManagement).Assembly.Location;
                dirInfo = System.IO.Path.GetDirectoryName(dirInfo);
                targetFile = System.IO.Path.Combine(dirInfo,"config", filePath);
                if (!System.IO.File.Exists(targetFile))
                {
                    strError = $"can't find such file|{targetFile}";
                    isOk = false;
                    return null;
                }
                string strTxt = System.IO.File.ReadAllText(targetFile);
#if !(_source_dll || _sub_dll )
                marsObjectDotNetEngineFile = JsonConvert.DeserializeObject<ObjectEngineConfigFile>(strTxt);
#else
                marsObjectDotNetEngineFile = System.Text.Json.JsonSerializer.Deserialize<ObjectEngineConfigFile>(strTxt);
#endif
                return marsObjectDotNetEngineFile;  
            }
            catch (Exception e)
            {
                strError = $"can't load dotnet object engine file|{filePath}| with exception|{e.Message}";
                
                isOk = false;
                return marsObjectDotNetEngineFile  = null;
            }
        }

        public static ObjectEngineConfigFile GetEngineObject(bool isReload = false, string strConfigFileName= "engineObjectsManager.json")
        {
            if ((marsObjectDotNetEngineFile == null) || (isReload))
            {
                string strError = "";
                bool isOk = false;
                LoadFromConfigFile(strConfigFileName, ref strError, ref isOk);
            }
            return marsObjectDotNetEngineFile;
        }

        public static string saveBacktoFile(ref bool isOk, ref string strError)
        {
            if (!System.IO.File.Exists(targetFile))
            {
                strError = $"no |{targetFile}| exists, Init first.";
                isOk = false;
                return null;
            }
            try
            {
#if !(_source_dll || _sub_dll )
                string strData = JsonConvert.SerializeObject(marsObjectDotNetEngineFile);
#else
                string strData = System.Text.Json.JsonSerializer.Serialize(marsObjectDotNetEngineFile);
#endif
                System.IO.File.WriteAllText(targetFile, strData);
                isOk = true;
                return targetFile; 
            }catch(Exception e)
            {
                strError = $"can't override the file|{targetFile}| with error|{e.Message}\r\n{e.StackTrace}";
                isOk = false;
                return null;
            }
            finally
            {

            }
        }

        public static bool isTypeStringInTestStepTypes(string targetType)
        {
            if (string.IsNullOrEmpty(targetType)) return false; 
            if (marsObjectDotNetEngineFile == null) return false;
            if (marsObjectDotNetEngineFile.twoStepsTypes == null) return false;
            if (marsObjectDotNetEngineFile.twoStepsTypes.IndexOf(targetType) >= 0)
                return true;
            return false;
        }

        public static ObservableCollection<string> GetMarsSupportsTypesForWpf()
        {               
            if (marsObjectDotNetEngineFile== null)
            {
                marsObjectDotNetEngineFile = GetEngineObject(true);                    
            }
            if (marsObjectDotNetEngineFile == null) return null;
            
            var typeList = marsObjectDotNetEngineFile.marsTypeMapping_wpf_core.TypesAndKeywords.Select(x => x.marsType)
                    .OrderBy(x => x)
                    .ToList();
            return new ObservableCollection<string>(typeList);                            
        }

        public static ObjectEngine_TypeMappingDotNet? GetMARSTypeByObjectType(string t)
        {
            if (marsObjectDotNetEngineFile == null)
            {
                marsObjectDotNetEngineFile = GetEngineObject(true);
            }
            if (marsObjectDotNetEngineFile == null) return null;
            if (marsObjectDotNetEngineFile.marsTypeMapping_wpf_core.IsIgnoreWpfTypes(t)) return null;

            var marsTyp = marsObjectDotNetEngineFile.marsTypeMapping_wpf_core.TypesAndKeywords
                .Where(x=>x.controlType.Any(p=>p.Equals(t,StringComparison.OrdinalIgnoreCase)&&(!string.IsNullOrEmpty(p))))
                .FirstOrDefault();
            return marsTyp;
        }
    }


}
