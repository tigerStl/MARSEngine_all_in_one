using MARS.AIL.NLP.Inter.AutoData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.restClient.communiteData
{

    public class MARSOBJ
    {
        public string? parent { get; set; }
        public string? swftype { get; set; }
        public string? ref_keyword { get; set; }
    }

    public class MARSData
    {
        public string? key { get; set;   }
        public string? value { get; set; } 
        public string? reference { get; set; }
        public int? idx
        {
            get; set;
        } = null;

        public string GetDictionaryValue(string sourceQueryId)
        {
            if (string.IsNullOrEmpty(key)) return value;
            if (key.Equals(MARSNLPConstant.cnst_data_key_default_suffix, StringComparison.OrdinalIgnoreCase))
            {
                /// return the last vords
                /// 
                if (string.IsNullOrEmpty(sourceQueryId)) return value;
                string[] tmpSplitedData = sourceQueryId.Split(' ');
                return tmpSplitedData[tmpSplitedData.Length - 1];
            }
            return value;
        }
    }

    public class DictionaryData_Request
    {
        public string? keynote { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class DictionaryData
    {
        public List<MARSOBJ>? _MARS_OBJ { get; set; }
        public List<MARSData>? _MARS_DATA { get; set; } 
        public string? alias { get; set; }
        public string? @ref { get; set; }
        public string? type { get; set; }

        /// <summary>
        /// 这个是动态的数据，如果python段发现有ref的对象，同时也找到了ref的内容，那么，就
        /// 设置改对象为true，被指向的，应该是基本词，不存在多义（暂时），如果存在多义，需要指定
        /// 实际的MARSOBJ，目前只采用
        /// </summary>
        public bool? _hasRef { get; set; } 
        /// <summary>
        /// 指向条目
        /// </summary>
        public List<DictionaryData>? _refDictionaryItem { get; set; }
        public string? query_id { get; set; } // this is item from request

        public MARSData? getDefaultData(string? keyIdx = null)
        {
            var tmpData = this.getMARSDataOrFromRef();

            if (tmpData == null) return null;
            if (tmpData.Count == 0) return null;
            if (string.IsNullOrEmpty(keyIdx))
            {
                return tmpData.OrderBy(p => p.idx).FirstOrDefault();
            }
            var v = tmpData.Where(p => keyIdx.Equals(p.key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (v == null) return tmpData.OrderBy(p=>p.idx).FirstOrDefault();
            return v;
        }

        public List<MARSOBJ>? getMARSOBJOrFromRef()
        {
            if (_MARS_OBJ != null) return _MARS_OBJ;
            if ((_refDictionaryItem == null) || (_refDictionaryItem.Count <= 0)) return new List<MARSOBJ>();
            /// should only one item in ref list
            return _refDictionaryItem[0]._MARS_OBJ;
        }

        public List<MARSData>? getMARSDataOrFromRef()
        {
            if (_MARS_DATA != null) return _MARS_DATA;
            if ((_refDictionaryItem == null) || (_refDictionaryItem.Count <= 0)) return new List<MARSData>();
            /// should only one item in ref list
            return _refDictionaryItem[0]._MARS_DATA;
        }
        public string? getAliasOrFromRef()
        {
            if (!string.IsNullOrEmpty(this.alias)) return this.alias;
            if ((_refDictionaryItem == null)||(_refDictionaryItem.Count<=0)) return null;
            return _refDictionaryItem[0].getAliasOrFromRef();
        }
    }

    public class DictionaryData_Response
    {
        public List<DictionaryData>? dictionary { get; set; }
        public string? result { get; set; }
    }

    public class DictionariesData_Request
    {
        public string[] keynotes { get; set; }
    }

    /// <summary>
    /// for api lookupDictionaries
    /// </summary>
    public class DictionaryObject_forResponse
    {
        public string k { get; set; }
        public List<DictionaryData?> obj { get; set; }

        public bool IsMarsObjExists()
        {
            if (obj == null) return false;
            return obj.Exists(p=>p._MARS_OBJ != null);             
        }

        public List<DictionaryData?>? getMarsDataSettings()
        {
            if (obj == null) return null;
            return obj.Where(p=>p.type.Equals(MARS_NLP_steps_Keywords.cnst_dictionary_type_mars_data, 
                StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
    /// <summary>
    /// for api lookupDictionaries
    /// </summary>
    public class DictionariesData_Response
    {
        public string message { get; set; }
        public string result { get; set; } = MARSNLP_REST_API_message.cnst_response_SUCCESS;
        public List<DictionaryObject_forResponse?> objs { get; set; }
    }

    

}
