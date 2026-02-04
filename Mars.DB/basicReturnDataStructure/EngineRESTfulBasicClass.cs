using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace MarsEngineSvc.basicReturnDataStructure
{
    public class EngineRESTfulBasicClass
    {
    }

    [DataContract]
    public enum RESTfulObjectType
    {
        error_unsupport = 0x00,
        error_obj,
        application_object, //application info
        application_object_list,
        storyboard_object,
        storyboard_objects_list,
        storyboard_lastMarkId,
        testcase_Steps,
        testdata_dbsetId,
        seq_id,
        marsObjects,
        keywords,
        systemLookup,
        reportObject,
        reportObject_Update,
        variables,
        variable_modal,
        variable_global,
        variable_loop,
        report_step_record_update,
        report_step_capture_data,
        proj_test_result,
        storyboard_testFullVision,
        refresh_objects,
        refresh_objects_unsupportedCommand,
        get_supportedDbLinks,
        dataSourceTable,

        report_step_del,
        storyboard_withapplication,

        update_status_var,
        variable_status,

        mars_bson_data_is_null,
        mars_bson_convert_error,
        _get_all_dbs_request,
        _get_all_dbs_response,

        _get_mapping_response_error,
        _get_mapping_response_data_null,
        _get_mapping_response_ok,

        _save_teststep_request,
        _save_teststep_response_ok,
        _save_teststep_response_error,

        _hardDisk_volumn_request,
        _hardDisk_volumn_response_ok,
        _hardDisk_volumn_response_error,

        _getProject_response_ok,
        _getProject_response_error,

        _getTestsuite_response_ok,
        _getTestsuite_response_error,

        _getMarsConfigDatabase_response_ok,
        _getMarsConfigDatabase_response_error,

        _createOrSaveTC_response_ok,
        _createOrSaveTC_response_error,

        _createOrUpdateDataset_response_ok,
        _createOrUpdateDataset_response_error,

        _getImageObject_request,
        _getImageObject_response_ok,
        _getImageObject_response_error,
    }

    [Serializable]
    [DataContract]
    public class MarsRESTKeyValuePair<T1, T2>
    {
        [DataMember]
        public T1 key { get; set; }
        [DataMember]
        public T2 value { get; set; }

        public MarsRESTKeyValuePair<T1, T2> copyFrom(KeyValuePair<T1, T2> src)
        {
            if (src.Equals(default(KeyValuePair<T1, T2>))) return null;
            MarsRESTKeyValuePair<T1, T2> rslt = new MarsRESTKeyValuePair<T1, T2>();
            rslt.key = src.Key;
            rslt.value = src.Value;
            return rslt;
        }

        public List<MarsRESTKeyValuePair<T1, T2>> copyFromList(IList<KeyValuePair<T1, T2>> src)
        {
            if (src == null) return null;
            List<MarsRESTKeyValuePair<T1, T2>> lstRslt = new List<MarsRESTKeyValuePair<T1, T2>>();
            for (int i = 0; i < src.Count; i++)
            {
                if (src[i].Equals(default(KeyValuePair<T1, T2>))) continue;
                MarsRESTKeyValuePair<T1, T2> oneItm = this.copyFrom(src[i]);
                if (oneItm == null) continue;
                lstRslt.Add(oneItm);
            }
            return lstRslt;
        }

        public List<MarsRESTKeyValuePair<T1, T2>> copyFromList(Dictionary<T1, T2> srcDic)
        {
            if (srcDic == null) return null;
            List<MarsRESTKeyValuePair<T1, T2>> rslt = new List<MarsRESTKeyValuePair<T1, T2>>();
            foreach (var itm in srcDic.Keys)
            {
                if (itm == null) continue;
                MarsRESTKeyValuePair<T1, T2> tmpKeypair = new MarsRESTKeyValuePair<T1, T2>()
                {
                    key = itm,
                    value = srcDic[itm]
                };
                rslt.Add(tmpKeypair);
            }
            return rslt;
        }

        public KeyValuePair<T1, T2> toKeyValuePair()
        {
            return new KeyValuePair<T1, T2>(key, value);
        }

        public IList<KeyValuePair<T1, T2>> toKeyValuePairList(List<MarsRESTKeyValuePair<T1, T2>> srcList)
        {
            if (srcList == null) return null;
            List<KeyValuePair<T1, T2>> rslt = new List<KeyValuePair<T1, T2>>();
            foreach (var itm in srcList)
            {
                if (itm == null) continue;
                KeyValuePair<T1, T2> tmpKeypair = new KeyValuePair<T1, T2>(itm.key, itm.value);
                rslt.Add(tmpKeypair);
            }
            return rslt;
        }

        public Dictionary<T1, T2> toDictionary(List<MarsRESTKeyValuePair<T1, T2>> srcList)
        {
            if (srcList == null) return null;
            Dictionary<T1, T2> rslt = new Dictionary<T1, T2>();
            foreach (var itm in srcList)
            {
                if (itm == null) continue;
                rslt.Add(itm.key, itm.value);
            }
            return rslt;
        }
    }
    [Serializable]
    [DataContract]
    public class RESTfulReturnObjects
    {
        [DataMember]
        public string? currentDBIdx { get; set; }

        [DataMember]
        public int objectType { get; set; }

        [DataMember]
        public string? ReturnedMessage { get; set; }

        [DataMember]
        public string? FromMethod { get; set; }

        [DataMember]
        public string? StackTrace { get; set; }

        [DataMember]
        public string? Ext { get; set; }

        //[DataMember]
        //public object AssignedObject { get; set; }

        public long convertExtToInt(long defaultWhenError)
        {
            if (string.IsNullOrEmpty(Ext)) return defaultWhenError;
            long rslt = defaultWhenError;
            if (!long.TryParse(Ext, out rslt)) return defaultWhenError;
            return rslt;
        }

        public virtual void setMessage(object o)
        {
            return;
        }
    }

    [Serializable]
    [DataContract]
    public class RESTfulReturnAllDBIdsRequest
    {
        [DataMember]
        public int objectType { get; set; }
        [DataMember]
        public bool isOk { get; set; }
        [DataMember]
        public string[] allDBIds { get; set; }
    }


    [Serializable]
    [DataContract]
    public class RESTfulSupportedDBLinks
    {
        [DataMember]
        public string DBIndx;
    }
    /// <summary>
    /// 当前服务支持的数据库链接信息
    /// </summary>
    [DataContract]
    public class RESTfulDBLinks : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<RESTfulSupportedDBLinks> currentDbLinks = new List<RESTfulSupportedDBLinks>();
        public void setDBLinks(List<string> dbLinks)
        {
            List<RESTfulSupportedDBLinks> tmpList = new List<RESTfulSupportedDBLinks>();
            //tmpList.Clear();
            if (dbLinks == null) return;
            dbLinks.ForEach(p =>
            {
                tmpList.Add(new RESTfulSupportedDBLinks()
                {
                    DBIndx = p
                });
            });
            currentDbLinks = tmpList;
            //setMessage(currentDBIdx);
        }

        public RESTfulDBLinks() : base()
        {
            objectType = (int)RESTfulObjectType.get_supportedDbLinks;
        }

        public override void setMessage(object o)
        {
            if (o == null) return;
            if (!(o is List<RESTfulSupportedDBLinks>))
            {
                return;
            }

        }
    }

}