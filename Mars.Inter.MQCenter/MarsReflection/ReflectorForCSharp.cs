using System;
using System.Collections.Generic;
#if tiger_dotNet4
using System.Linq;
#endif
using System.Reflection;
using System.Text.RegularExpressions;
using Logger = Mars.message.Inter.MQCenter.simpleLog.MarsLoggerSimple;
using Mars.message.AutoTestingDriver.ErrorMessage;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;

namespace MarsUFTAddins.IMars.tiger
{
    public delegate int MarsReflectCompare<T>(T v1, T v2, ref bool isOk, ref string strError);
    public delegate bool MarsReflectCheckToContinue<T>(T x1, ref bool isOk, ref string strError, T x2 = default(T));

    public class ReflectorForCSharp
    {
        public static int GetLineNumber()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1);
            return frame.GetFileLineNumber();
        }

        public static string GetObjectBaseType(Type o,bool isWithSystem = false)
        {
            if (o == null) return "";
            string currentType = o.ToString();
            if (!isWithSystem)
            {
                if (currentType.StartsWith("System")) return currentType + ";";
            }
            return currentType + ";" + GetObjectBaseType(o.BaseType, isWithSystem);
        }

        private static void GetTypeAndItsAncestor(Type typ, List<Type> lstResult)
        {
            if (typ == null) return;
            if (typ.ToString().StartsWith("System"))
            {
                lstResult.Add(typ);
            }
            else
            {
                GetTypeAndItsAncestor(typ.BaseType, lstResult);
            }
        }

        public bool ObjectIsIList(object o)
        {
            if (o == null) return false;
            return o is IList 
                && o.GetType().IsGenericType
                //&& o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))
                ;
        }

        public static string GetTypeAndItsAncestor(Type typ)
        {
            List<Type> lstRslt = new List<Type>();
            GetTypeAndItsAncestor(typ, lstRslt);
            string strResult = "";
            foreach (var t in lstRslt)
            {
                if (t == null) continue;
                strResult = string.Format("{0};{1}", strResult, t.ToString());
            }
            return strResult;
        }

        protected string getParameterAndValue(string strPara, string strV)
        {
            return string.Format("{0}:[{1}]", strPara, strV);
        }

        public static object GetProperty(object objSrc, string strPropertyIdx, ref bool isNotExist)
        {
            Logger.Info("GetProperty", string.Format("strPropertyIdx:[{0}]", strPropertyIdx));
            Type objType = objSrc.GetType();
            PropertyInfo oProperty = objType.GetProperty(strPropertyIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (oProperty == null)
            {
                isNotExist = true;
                Logger.Info("GetProperty", "no such Property:" + strPropertyIdx);
                return null;
            }
            return oProperty;
        }

        public static object GetPropertyValueByPropertyNameIdx(object objSrc, string strProperty, ref bool isNotExist)
        {
            isNotExist = false;
            object pro = GetProperty(objSrc, strProperty, ref isNotExist);
            if ((isNotExist) || (pro == null))
            {
                return null;
            }
            PropertyInfo p = pro as PropertyInfo;
            if (p==null)
            {
                isNotExist = true;
                return null;
            }
            isNotExist = false;
            Logger.Info("GetPropertyValueByPropertyNameIdx", $"property to get [{p.Name}]");
            return GetPropValue(objSrc, p.Name);
        }

        internal object GetAllProperties(object objSrc)
        {
            Logger.logBegin("GetAllProperties");
            Type objType = objSrc.GetType();
            PropertyInfo[] arrPro = objType.GetProperties();
            string strResult = "";
            foreach (var itm in arrPro)
            {
                if (itm == null) continue;
                strResult = string.Format("{0};{1}-[{2}]", strResult, itm.Name, itm.GetValue(objSrc, null));
            }
            Logger.logEnd("GetAllProperties");
            return strResult;
        }

        internal Dictionary<PropertyInfo, string> GetAllPropertiesWithValues(object objSrc)
        {
            Dictionary<PropertyInfo, string> rslt = new Dictionary<PropertyInfo, string>();
            Type objType = objSrc.GetType();
            PropertyInfo[] arrProNonRunTime = objType.GetProperties(BindingFlags.Public| BindingFlags.NonPublic |BindingFlags.FlattenHierarchy);
            List<PropertyInfo> arrPro = new List<PropertyInfo>();
            if (arrProNonRunTime!=null)
                arrPro.AddRange(arrProNonRunTime);
            var runTimePro = objType.GetRuntimeProperties();
            if (runTimePro != null)
                arrPro.AddRange(runTimePro);
            foreach(var itm in arrPro)
            {
                if (itm == null) continue;
                try
                {                    
                    object ov = itm.GetValue(objSrc);
                    if (ov == null)
                        rslt.Add(itm, "NULL");
                    else
                        rslt.Add(itm, ov.ToString());
                }
                catch (Exception)
                {

                }
                
            }
            return rslt;
        }


        public static Object GetPropValue(Object obj, String propName)
        {
            string[] nameParts = propName.Split('.');
            if (nameParts.Length == 1)
            {
                return obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static
                    | BindingFlags.Instance).GetValue(obj, null);
            }

            foreach (String part in nameParts)
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        public static T GetPropertyValue<T>(object obj, string propertyName, ref string strError, ref bool isOk,  T defaultValue = default)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
            {
                isOk = false ;
                strError = $"No Object instance of property name is empty|{propertyName}|";
                return defaultValue;
            }
                

            try
            {
                // 获取对象类型
                var type = obj.GetType();

                // 获取指定属性
                PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                // 如果属性存在，获取其值
                if (propertyInfo != null)
                {
                    object value = propertyInfo.GetValue(obj);
                    // 将属性值转换为泛型类型 T
                    if (value is T typedValue)
                    {
                        isOk = true;
                        return typedValue;
                    }
                }
                isOk =false ;
                strError = $"No such property |{propertyName}| Exists in such type|{type.FullName}";
                return defaultValue;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = $"can't get |{propertyName}| with exception|{e.Message}";
                return defaultValue;
            }

        }


        public static object GetMemberWithTimeDelay(object objsrc, string strMemIdx, int iSec)
        {
            long tickBegin = DateTime.Now.Ticks;
            long tickCur = tickBegin;
            object oTarget = null;
            while ((((tickCur - tickBegin) / TimeSpan.TicksPerSecond) < iSec) && (oTarget == null))
            {
                oTarget = GetMember(objsrc, strMemIdx);
                tickCur = DateTime.Now.Ticks;
                if (oTarget != null) return oTarget;
                System.Threading.Thread.Sleep(100);
            }
            return null;
        }
        /// <summary>
        /// return fieldInfo only
        /// </summary>
        /// <param name="objSrc"></param>
        /// <returns></returns>
        public Dictionary<MemberInfo, string> getAllMemberInfo(object objSrc)
        {
            Logger.logBegin("getAllMemberInfo");
            if (objSrc == null) return null;
            Type objType = objSrc.GetType();
            if (objType == null) return null;
            
            var members = objType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy |BindingFlags.Instance);
            var runtimeFields = objType.GetRuntimeFields();
            List<MemberInfo> lstTmp = new List<MemberInfo>();
            lstTmp.AddRange(members);
            lstTmp.AddRange(runtimeFields);

            Dictionary<MemberInfo, string> rslt = new Dictionary<MemberInfo, string>();
            try
            {
                foreach(var objOne in lstTmp)
                {
                    
                    if (objOne == null) continue;
                    if (objOne is MemberInfo)
                        switch (((MemberInfo)objOne).MemberType)
                        {
                            case MemberTypes.Field:
                                Logger.Info("GetMember", "return field");
                                object objResult = (((FieldInfo)objOne).GetValue(objSrc));
                                rslt.Add(objOne, objResult == null ? "NULL" : objResult.ToString());
                                Logger.logEnd("GetMember");
                                break;
                            case MemberTypes.Property:
                                continue;
                            //case MemberTypes.NestedType:
                            //    rslt.Add(objOne, )
                            default:
                                Logger.Info("GetMember", "unsupported: " + ((MemberInfo)objOne).MemberType.ToString());
                                Logger.logEnd("GetMember");
                                break;
                        }
                    else if (objOne is FieldInfo)
                    {
                        object objResult = (((FieldInfo)objOne).GetValue(objSrc));
                        rslt.Add(objOne, objResult == null ? "NULL" : objResult.ToString());
                    }
                }
                return rslt;
            } catch(Exception e)
            {
                Logger.Error("getAllMemberInfo", e.Message, e);
                return rslt;
            }
            finally
            {
                Logger.logEnd("getAllMemberInfo " + $"total return members:[{rslt.Keys.Count}]");
            }
        }
        /// <summary>
        /// isSafecall should be available only for control
        /// </summary>
        /// <param name="objSrc"></param>
        /// <param name="strMemIdx"></param>
        /// <param name="isNotExist"></param>
        /// <param name="isSafeCall"></param>
        /// <returns></returns>

        public static object GetMember(object objSrc, string strMemIdx, ref bool isNotExist, bool isSafeCall = false)
        {
            //Logger.Info("GetMember", string.Format("strMemIdx:[{0}]", strMemIdx));
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = null;
            bool isInvokeCall = (objSrc is System.Windows.Forms.Control) && (isSafeCall);
            System.Windows.Forms.Control c = null;
            if (isInvokeCall)
            {
                c = objSrc as System.Windows.Forms.Control;                
            }
            arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                isNotExist = true;
                Logger.Info("GetMember", "no such Member:" + strMemIdx);
                return null;
            }
            isNotExist = false;
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", "find member but value is null:" + strMemIdx);
                return null;
            }
            object objResult = null;
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    //Logger.Info("GetMember", "return field");
                    if (isInvokeCall)
                    {
                        c.Invoke(new Action(() =>{
                            objResult=(((FieldInfo)objOne).GetValue(objSrc));
                        }));
                        Logger.logEnd("GetMember", $"invoke mode|field");
                        return objResult;
                    }
                    else {
                        objResult = (((FieldInfo)objOne).GetValue(objSrc));
                        Logger.logEnd("GetMember", $"not invoke mode|field");
                        return objResult;
                    }
                case MemberTypes.Property:
                    Logger.Info("GetMember", "return Porperty");
                    if (isInvokeCall)
                    {
                        c.Invoke(new Action(() => {
                            objResult = (((PropertyInfo)objOne).GetValue(objSrc, null));
                        }));                        
                    }
                    else
                        objResult = (((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.logEnd("GetMember", $"invokeMode|{isInvokeCall}|Porperty");
                    return objResult;
                default:
                    Logger.Info("GetMember", "unsupported: " + objOne.MemberType.ToString());
                    Logger.logEnd("GetMember");
                    isNotExist = true;
                    return null;
            }
        }

        public static object GetMember(object objSrc, string strMemIdx)
        {
            //Logger.Info("GetMember", string.Format("strMemIdx:[{0}]", strMemIdx));
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                //Logger.Info("GetMember", "no such Member:" + strMemIdx);
                return null;
            }
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", "find member but value is null:" + strMemIdx);
                return null;
            }
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    Logger.Info("GetMember", "return field");
                    object objResult = (((FieldInfo)objOne).GetValue(objSrc));
                    Logger.logEnd("GetMember");
                    return objResult;
                case MemberTypes.Property:

                    objResult = (((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.Info("GetMember", string.Format("return Porperty, [{1}]-[{0}]", objResult == null ? "" : objResult.ToString(), strMemIdx));
                    Logger.logEnd("GetMember");
                    return objResult;
                default:
                    Logger.Info("GetMember", "unsupported: " + objOne.MemberType.ToString());
                    Logger.logEnd("GetMember");
                    return null;
            }
        }

        public static T GetMemberByType<T>(object objSrc, string strMemIdx)
        {
            Logger.logBegin("GetMember", string.Format("strMemIdx:[{0}]", strMemIdx));
            //Logger.Info("GetMember", getParameterAndValue("strMemIdx", strMemIdx));
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                Logger.Info("GetMember", "no such Member:" + strMemIdx);
                return default(T);
            }
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", "find member but value is null:" + strMemIdx);
                return default(T);
            }
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    Logger.Info("GetMember", "return field");
                    T objResult = (T)(((FieldInfo)objOne).GetValue(objSrc));
                    Logger.logEnd("GetMember");
                    return objResult;
                case MemberTypes.Property:
                    Logger.Info("GetMember", "return Porperty");
                    objResult = (T)(((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.logEnd("GetMember");
                    return objResult;
                default:
                    Logger.Info("GetMember", "unsupported: " + objOne.MemberType.ToString());
                    Logger.logEnd("GetMember");
                    return default(T);
            }

        }

        public T GetMember<T>(object objSrc, string strMemIdx, ref bool isNotExists)
        {
            Logger.logBegin("GetMember");
            Logger.Info("GetMember", getParameterAndValue("strMemIdx", strMemIdx));
            isNotExists = false;
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                isNotExists = true;
                Logger.Info("GetMember", "no such Member:" + strMemIdx);
                return default(T);
            }
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", "find member but value is null:" + strMemIdx);
                return default(T);
            }
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    Logger.Info("GetMember", "return field");
                    T objResult = (T)(((FieldInfo)objOne).GetValue(objSrc));
                    Logger.logEnd("GetMember");
                    return objResult;
                case MemberTypes.Property:
                    Logger.Info("GetMember", "return Porperty");
                    objResult = (T)(((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.logEnd("GetMember");
                    return objResult;
                default:
                    Logger.Info("GetMember", "unsupported: " + objOne.MemberType.ToString());
                    Logger.logEnd("GetMember");
                    isNotExists = true;
                    return default(T);
            }

        }

        public T GetMember<T>(object objSrc, string strMemIdx)
        {
            Logger.logBegin("GetMember");
            Logger.Info("GetMember", getParameterAndValue("strMemIdx", strMemIdx));
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                Logger.Info("GetMember", "no such Member:" + strMemIdx);
                return default(T);
            }
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", "find member but value is null:" + strMemIdx);
                return default(T);
            }
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    Logger.Info("GetMember", "return field");
                    T objResult = (T)(((FieldInfo)objOne).GetValue(objSrc));
                    Logger.logEnd("GetMember");
                    return objResult;
                case MemberTypes.Property:
                    Logger.Info("GetMember", "return Porperty");
                    objResult = (T)(((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.logEnd("GetMember");
                    return objResult;
                default:
                    Logger.Info("GetMember", "unsupported: " + objOne.MemberType.ToString());
                    Logger.logEnd("GetMember");
                    return default(T);
            }

        }
        public bool SetMemberValue(object value2Set, object objSrc, string strMemIdx, ref string strError, ref string strStack)
        {
            Logger.logBegin("SetMemberValue");
            Logger.Info("SetMemberValue", getParameterAndValue("strMemIdx", strMemIdx));
            Type objType = objSrc.GetType();
            MemberInfo[] arrMember = objType.GetMember(strMemIdx, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (arrMember.Length <= 0)
            {
                Logger.Info("SetMemberValue", strError = $"Object member [{strMemIdx} is NULL");
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            MemberInfo objOne = arrMember[0];
            if (objOne == null)
            {
                Logger.Info("GetMember", strError = $"Object member [{strMemIdx}]'s value is NULL");
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            switch (objOne.MemberType)
            {
                case MemberTypes.Field:
                    Logger.Info("SetMemberValue", "return field");
                    ((FieldInfo)objOne).SetValue(objSrc, value2Set);

                    Logger.logEnd("SetMemberValue");
                    return true;
                case MemberTypes.Property:
                    Logger.Info("SetMemberValue", "return Porperty");
                    ((PropertyInfo)objOne).SetValue(objSrc, value2Set, null);
                    //objResult = (T)(((PropertyInfo)objOne).GetValue(objSrc, null));
                    Logger.logEnd("SetMemberValue");
                    return true;
                default:
                    Logger.Info("SetMemberValue", strError = $"Object member [{strMemIdx}] is not changable.");
                    strStack = MarsErrorStacks.StackTraceDump();
                    Logger.logEnd("SetMemberValue");
                    return false;
            }
        }
        public T GetPrivateField<T>(object instance, string fieldname)
        {
            Logger.logBegin("GetPrivateField");
            Logger.Info("GetPrivateField", getParameterAndValue("fieldname", fieldname));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            Logger.logEnd("GetPrivateField");
            return (T)field.GetValue(instance);
        }

        public void GetAllEventsName(object objInst, ref List<string> lstResult)
        {
            Logger.logBegin("--------GetAllEventsName--------");
            EventInfo[] arrList = objInst.GetType().GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (lstResult == null) lstResult = new List<string>();
            foreach (EventInfo e in arrList)
            {
                lstResult.Add(string.Format("{0,32}", e.Name));
            }
            Logger.logEnd("GetAllEventsName");
        }

        public T GetPrivateProperty<T>(object instance, string propertyname)
        {
            Logger.logBegin("GetPrivateProperty");
            Logger.Info("GetPrivateProperty", getParameterAndValue("propertyname", propertyname));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();

            PropertyInfo field = type.GetProperty(propertyname, flag);
            Logger.logEnd("GetPrivateProperty");
            return (T)field.GetValue(instance, null);
        }

        public void SetPrivateField(object instance, string fieldname, object value)
        {
            Logger.logBegin("SetPrivateField");
            Logger.Info("SetPrivateField", getParameterAndValue("fieldname", fieldname));
            Logger.Info("SetPrivateField", getParameterAndValue("value", value == null ? "" : value.ToString()));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            field.SetValue(instance, value);
            Logger.logEnd("SetPrivateField");
        }

        public bool SetProperty(object instance, string propertyname, object value, ref string strError)
        {
            Logger.logBegin("SetPrivateProperty", propertyname);
            try
            {
                Logger.Info("SetPrivateProperty", getParameterAndValue("propertyname", propertyname));
                Logger.Info("SetPrivateProperty", getParameterAndValue("value", value == null ? "" : value.ToString()));
                BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                Type type = instance.GetType();
                PropertyInfo field = type.GetProperty(propertyname, flag);
                field.SetValue(instance, value, null);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SetProperty", e.Message, e);
                strError = e.Message;
                return false;
            }
            finally
            {
                Logger.logEnd("SetPrivateProperty");
            }


        }

        public static string MarsGetParentsNames(Control targetObject)
        {
            while (targetObject != null)
            {
                return targetObject.Name + ";" + MarsGetParentsNames(targetObject.Parent);
            }
            return "";
        }

        internal string getNamePath(Control targetObject)
        {
            return targetObject.Parent == null?"":MarsGetParentsNames(targetObject.Parent);            
        }

        public void SetPrivateProperty(object instance, string propertyname, object value)
        {
            Logger.logBegin("SetPrivateProperty");
            Logger.Info("SetPrivateProperty", getParameterAndValue("propertyname", propertyname));
            Logger.Info("SetPrivateProperty", getParameterAndValue("value", value == null ? "" : value.ToString()));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname, flag);
            field.SetValue(instance, value, null);
            Logger.logEnd("SetPrivateProperty");
        }

        public T CallPrivateMethod<T>(object instance, string name, params object[] param)
        {
            Logger.logBegin("CallPrivateMethod");
            Logger.Info("CallPrivateMethod", getParameterAndValue("propertyname", name));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            MethodInfo method = type.GetMethod(name, flag);
            return (T)method.Invoke(instance, param);
        }

        public object CallMethod(object inst, string strName, params object[] param)
        {
            Logger.logBegin("CallMethod");
            Logger.Info("CallMethod", getParameterAndValue("propertyname", strName));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = inst.GetType();
            MethodInfo method = type.GetMethod(strName, flag);
            return method.Invoke(inst, param);
        }

        internal bool WaitForPerprotyValue(object targetDropdownList, string proPerName, string valueToCompare, int iWaitForSeconds, ref string strError, ref string strStack)
        {
            Logger.logBegin("WaitForPerprotyValue", $"{proPerName}, datatoCheck:{valueToCompare}-[{iWaitForSeconds}]");
            ReflectorForCSharp r = new ReflectorForCSharp();
            try
            {
                long s = System.DateTime.Now.Ticks, e = s;
                bool isNotExists = false;
                bool isOk = false;
                while (((System.DateTime.Now.Ticks - s)/ TimeSpan.TicksPerSecond) <= iWaitForSeconds){
                    
                    object rslt = ReflectorForCSharp.GetMember(targetDropdownList, proPerName, ref isNotExists);
                    if (isNotExists)
                    {
                        strStack = Environment.StackTrace;
                        strError = $"No {proPerName} exists in type {targetDropdownList.GetType()}";
                        return false;
                    }
                    if (rslt == null)
                    {
                        strStack = Environment.StackTrace;
                        strError = $"Can't get {proPerName} from in type {targetDropdownList.GetType()}";
                        return false;
                    }
                    if (valueToCompare.Equals(rslt.ToString(), StringComparison.OrdinalIgnoreCase))
                        return true;
                        
                    System.Threading.Thread.Sleep(100);
                }
                return false;
            }
            finally
            {
                Logger.logEnd("WaitForPerprotyValue");
            }
        }

        public object CallMethodByTypes(object inst, string strName, params object[] param)
        {
            List<Type> lstTyp = new List<Type>();
            if (param == null) lstTyp = null;
            else
            {
                foreach (var x in param)
                {
                    if (x == null) lstTyp.Add(null);
                    else
                        lstTyp.Add(x.GetType());
                }
            }
            Logger.Info("CallMethod", getParameterAndValue("propertyname", strName));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = inst.GetType();
            MethodInfo method = type.GetMethod(strName, lstTyp.ToArray());
            return method.Invoke(inst, param);
        }

        public object CallMethodJustByName(object inst, string strName, params object[] param)
        {
            Logger.logBegin("CallMethodJustByName");
            Logger.Info("CallMethodJustByName", getParameterAndValue("propertyname", strName));
            Type type = inst.GetType();
            MethodInfo method = type.GetMethod(strName);
            return method.Invoke(inst, param);
        }

        public object CallMethodByParaType(object inst, string strName, Type[] arrT, params object[] param)
        {
            Logger.logBegin("CallMethodByParaType");
            Logger.Info("CallMethodByParaType", getParameterAndValue("propertyname", strName));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = inst.GetType();
            MethodInfo method = type.GetMethod(strName, flag, null, arrT, null);
            return method.Invoke(inst, param);
        }

        public object CallMethod(object inst, string strName, Type[] arrT, ref bool isNotExists, params object[] param)
        {
            Logger.logBegin("CallMethod");
            Logger.Info("CallMethod", getParameterAndValue("propertyname", strName));
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type type = inst.GetType();
            MethodInfo method = type.GetMethod(strName, arrT);
            if (method == null)
            {
                isNotExists = true;
                return null;
            }
            isNotExists = false;
            return method.Invoke(inst, param);
        }

        public bool WaitUntilMembersEquals<T>(object inst, string strMemberName, T valueToCompare, ref string strError, MarsReflectCompare<T> compareFunc)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            T oT = GetMember<T>(inst, strMemberName);
            if (oT == null)
            {
                strError = string.Format("WaitUntilMembersGreater - NO object find for no such member [{0}].", strMemberName);
                return false;
            }
            long ls = DateTime.Now.Ticks;
            long le = DateTime.Now.Ticks;
            bool isOk = false;
            T tv = default(T);
            while ((le - ls) / TimeSpan.TicksPerSecond < 120)
            {
#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    tv = GetMemberByType<T>(inst, strMemberName);
                }));
#else
                tv = GetMemberByType<T>(inst, strMemberName);
#endif
                if (compareFunc(tv, valueToCompare, ref isOk, ref strError) == 0)
                {
                    return true;
                }
                System.Threading.Thread.Sleep(10);
                le = DateTime.Now.Ticks;
            }
            strError = string.Format("Have waited over 2 minutes, but value for [{0}] is [{1}] which is not greater than [{2}]", strMemberName, tv, valueToCompare);
            return false;
        }

        public bool WaitUntilMembersGreater<T>(object inst, string strMemberName, T valueToCompare, ref string strError, MarsReflectCompare<T> compareFunc)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            T oT = GetMember<T>(inst, strMemberName);
            if (oT == null)
            {
                strError = string.Format("WaitUntilMembersGreater - NO object find for no such member [{0}].", strMemberName);
                return false;
            }
            long ls = DateTime.Now.Ticks;
            long le = DateTime.Now.Ticks;
            bool isOk = false;
            T tv = default(T);
            while ((le - ls) / TimeSpan.TicksPerSecond < 120)
            {
#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                new Action(() =>
                {
                    tv = GetMemberByType<T>(inst, strMemberName);
                }));
#else
                tv = GetMemberByType<T>(inst, strMemberName);
#endif
                if (compareFunc(tv, valueToCompare, ref isOk, ref strError) > 0)
                {
                    return true;
                }
                System.Threading.Thread.Sleep(10);
                le = DateTime.Now.Ticks;
            }
            strError = string.Format("Have waited over 2 minutes, but value for [{0}] is [{1}] which is not greater than [{2}]", strMemberName, tv, valueToCompare);
            return false;
        }

        public void CallEvent(object objInst, string strEvent, params object[] param)
        {
            Logger.logBegin("CallEvent");
            EventInfo objEvnt = objInst.GetType().GetEvent(strEvent);
            if (objEvnt == null)
            {
                Logger.Info("CallEvent", string.Format("no such event:{0}", strEvent));
                return;
            }
            Type objDeleType = objEvnt.EventHandlerType;
            MethodInfo objMthdInfoHandle = objInst.GetType().GetMethod(strEvent, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = Delegate.CreateDelegate(objDeleType, objMthdInfoHandle);
            //MethodInfo objAddHandler = objEvnt.get

            Logger.logEnd("CallEvent");
        }

        public MethodInfo GetMethod(object objInst, string strMethodName)
        {
            Logger.logBegin("GetMethod");
            try
            {
                if (objInst == null) return null;

                MethodInfo objMethod = objInst.GetType().GetMethod(strMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return objMethod;
            }
            finally
            {
                Logger.logEnd("GetMethod");
            }
        }

        public static T WaitUntilMemberExistSafe<T>(object src, string strMemberIndx,
            ref bool isok, ref string strError,
            int iWaitSencond = 60)
        {
            return WaitUntilMemberExist<T>(src, strMemberIndx, ref isok, ref strError, iWaitSencond, true);
        }
        

        public static T WaitUntilMemberExist<T>(object src, string strMemberIndx,
            ref bool isok, ref string strError,
            int iWaitSencond = 60,
            bool isSafeMember = false)
        {
            long lstart = DateTime.Now.Ticks;
            long lend = lstart;
            bool isNotExists = false;
            isok = true;

            while ((lend - lstart) / TimeSpan.TicksPerSecond < iWaitSencond)
            {
                object o = null;
                if (!isSafeMember)
                    o = GetMember(src, strMemberIndx, ref isNotExists);
                else
                {
                    if (src is System.Windows.Forms.Control)
                    {
                        o = GetMember(src, strMemberIndx, ref isNotExists, true);
                    }
                    else
                    {
                        o = GetMember(src, strMemberIndx, ref isNotExists);
                    }
                }
                if (isNotExists)
                {
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    if (!(o is T))
                    {
                        isok = false;
                        strError = string.Format("[{0}] is not type :[{1}]", strMemberIndx, typeof(T).ToString());
                        return default(T);
                    }
                    if ((T)o != null) return (T)o;

                    System.Threading.Thread.Sleep(100);

                }
                lend = DateTime.Now.Ticks;
            }

            isok = false;
            strError = string.Format("Can't find [{0}] after spending [{1}] seconds", strMemberIndx, iWaitSencond);
            return default(T);
        }


        public sealed class MarsTigerUtility
        {
            //public static MLogger Logger = MLogger.GetLogger(typeof(MarsTigerUtility));
            public static bool RegularExpressChecking(string strPartern, string strSrc)
            {
                Logger.logBegin(string.Format("RegularExpressChecking,{0}-{1}", CombinePara("strPartern", strPartern), CombinePara("strSrc", strSrc)));
                try
                {
                    Regex regex = new Regex(strPartern);
                    return regex.IsMatch(strSrc);
                }
                finally
                {
                    Logger.logEnd("RegularExpressChecking");
                }
            }


            public static string CombinePara(string strPara, string strValue)
            {
                return string.Format("{0}:[{1}]", strPara, strValue);
            }
        }


    }
}
