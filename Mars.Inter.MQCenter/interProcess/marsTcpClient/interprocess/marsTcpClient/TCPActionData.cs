using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Inter.MQCenter.interProcess.marsTcpClient
{
    [DataContract]
    public class TCPActionData
    {
        public const string cnst_datatype_objectcolltion    = "_MARS_OBJECT_LIST";
        public const string cnst_datatype_shakinghandClient = "_MARS_ClientShakingHand";
        public const string cnst_datatype_shakinghandServer = "_MARS_ServerShakingHand";
        public const string cnst_datatype_basicDataType     = "_MARS_BasicDataType";

        [DataMember]
        public string @Version;
        [DataMember]
        public string @DataType;
        [DataMember]
        public string @DataSubType;
        [DataMember(Name ="testMesage")]
        public string Message;

        public TCPActionData()
        {
            DataType = cnst_datatype_basicDataType;
            Version  = "1.0.0.0";
        }
        public virtual bool setMessage(object oSrc, ref string strError)
        {
            return true;
        }

        public virtual T restoreObjectFromMessage<T>(ref bool isOk, ref string strError)
        {
            isOk = false;
            strError = "No Implemente";
            return default(T);
        }
        public virtual string GetJson()
        {
            try
            {
#if !_MarsToolsImport
                simpleLog.MarsLoggerSimple.logBegin("GetJson");
#endif
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                return js.Serialize(this);
            } catch(Exception e)
            {
#if !_MarsToolsImport
                simpleLog.MarsLoggerSimple.Error("GetJson", e.Message, e);
#endif
                return null;
            }
            finally
            {
#if !_MarsToolsImport
                simpleLog.MarsLoggerSimple.logEnd("GetJson");
#endif
            }
            
        }

        public virtual bool setObjectFromMessage(ref string strError)
        {
            return true;
        }
        /// <summary>
        /// 工厂方法
        /// </summary>
        /// <param name="src"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal static TCPActionData JSonToObj(string src, ref string strError)
        {
            if (string.IsNullOrEmpty(src))
            {
                strError = "Para is empty or Null.";
                return null;
            }
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            try
            {
                TCPActionData bscData = js.Deserialize<TCPActionData>(src);
                if (bscData == null)
                {
                    strError = "Can't Deseriallize. Returns Null";
                    return null;
                }
                bool isOk = true;
                switch (bscData.DataType)
                {
                    case cnst_datatype_shakinghandClient:
                        try
                        {
                            TCPActionDataShakingHand shkHand = js.Deserialize<TCPActionDataShakingHand>(src);
                            if (shkHand == null)
                            {
                                strError = "Can't Deserialize to ";
                                return null;
                            }
                            isOk = shkHand.setObjectFromMessage(ref strError);
                            if (!isOk)
                                return null;
                            return shkHand;
                        } catch (Exception e) {
                            strError = e.Message;
                            return null;    
                        }
                    default:
                        strError = $"unsupported type of [{bscData.DataType}]";
                        return null;
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }
    }


    public class TCPActionDataPing
    {
        [DataMember]
        public string @Ping = "Ping";
    }
    public class TCPActionDataShakingHand: TCPActionData
    {
        private TCPActionDataPing pingData = new TCPActionDataPing();
        public TCPActionDataShakingHand():base()
        {
            string strError = "";
            this.DataType = "_MARS_ClientShakingHand";
            setMessage(new TCPActionDataPing(),ref strError);
        }

        public override bool setMessage(object oSrc, ref string strError)
        {
            if (oSrc == null)
            {
                strError = "Parameter is null.";
                return false;
            }
            /// 注入后，采用 .IsInstanceOfType 会出错
            if (oSrc.GetType()!=typeof(TCPActionDataPing))
            {
                strError = $"Only TCPActionDataPing is supported for TCPActionDataShakingHand.setMessage, but it is [{oSrc.GetType()}]";
                return false;
            }
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            this.Message = js.Serialize(oSrc); 

            return true;
        }

        public override bool setObjectFromMessage(ref string strError)
        {
            bool isOk = false;
            this.pingData = restoreObjectFromMessage<TCPActionDataPing>(ref isOk, ref strError);
            return isOk;
        }

        public override T restoreObjectFromMessage<T>(ref bool isOk, ref string strError)
        {
            if (typeof(T) != typeof(TCPActionDataPing))
            {
                isOk = false;
                strError = $"Only TCPActionDataPing is supported,but the type is {typeof(T)}";
                return default(T);
            }
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                var rslt = js.Deserialize<T>(Message);
                if (rslt == null)
                {
                    strError = "Can't convert to object [TCPActionDataPing]";
                    isOk = false;
                    return default(T);
                }
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                return default(T);
            }
        }
    }

    [DataContract]
    public class TCPAction_OneObject
    {
        [DataMember]
        public string @ObjectGUIID;
        [DataMember]
        public long @Handle;
        [DataMember]
        public int @X;
        [DataMember]
        public int @Y;
        [DataMember]
        public int @Width;
        [DataMember]
        public int @Height;
        [DataMember]
        public string @TypeName;
        [DataMember]
        public string @TypePath;
        [DataMember]
        public string @ObjectName;
        [DataMember]
        public string @ObjectNamePath;
    }
    [DataContract]
    public class TCPAction_ObjectCollection : TCPActionData
    {
        public TCPAction_ObjectCollection()
        {
            DataType = cnst_datatype_objectcolltion;
        }

        public override bool setMessage(object oSrc, ref string strError)
        {
            if (oSrc == null)
            {
                strError = "parameter is null";
                return false;
            }
            if (!(oSrc is IEnumerable<TCPAction_OneObject>))
            {
                strError = "";
                return false;
            }
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                Message = js.Serialize(oSrc);
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                return false;
            }            
        }

        public override T restoreObjectFromMessage<T>(ref bool isOk, ref string strError)
        {
            isOk = false;
            strError = "No Implemente";
            return default(T);
        }
    }
}
