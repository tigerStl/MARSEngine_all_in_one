using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData
{

    /***
     * 
     * sample data
     * {
     *    "APPLICATION":"summitft.exe 6.5",
     *    "KEYWORDS": ["*"],  --- removed on 2023-11-27
     *    "Error_Objects":[
     *      {
     *          "Error_Object":[
     *              {
     *                  "Name":"Default_ErrorTree",
     *                  "Keywords":["*", "FillEdit"],    added after 11252023, as some of keywords could generate error in different plis
     *                                                   if * exist, then no other keywords should follow, otherwise, should be special keywords
     *                  "ObjQuickAccess":[
     *                      {
     *                          "id":"swfname", 
     *                          "value":"_errorTree"
     *                      },
     *                      {
     *                          "id":"index", 
     *                          "value":"0"
     *                      }
     *                  ], 
     *                  "IMAGE":{
     *                      "enabled":true,
     *                      "scope":"PEGWINDOW|SELF|"
     *                  }
     *              }
     *          ]
     *      }
     *    ]
     * }
     * 
     * */

    public class MarsErrorCheckConst
    {
        public const string cnst_error_file_prefix = "_MARS_ERROR_FILE_:";
    }

    [Serializable]
    public class MarsErrorCheckData
    {
        public string APPLICATION { get; set; }
        //public List<string> KEYWORDS { get; set; }
        public List<MarsError_Objects> Error_Objects { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsIgnoreIfException { get; set; }
    }
    [Serializable]
    public class MarsObjQuickAccess
    {
        public string id { get; set; }
        public string value { get; set; }

    }
    [Serializable]
    public class MarsErrorCheckSelfProperties
    {
        public string propertyName { get; set; }
        public List<string> errorValue { get; set; } 
    }


    [Serializable]
    public class MarsError_Object
    {
        public string Name { get; set; }
        /// <summary>
        /// added on 12/12/2023
        /// two values should be put here, one-ObjectQuickAccess, the other-"SelfProperties"
        /// </summary>
        public string CheckMode { get; set; }
        public List<MarsObjQuickAccess> ObjQuickAccess { get; set; }
        public List<MarsErrorCheckSelfProperties> SelfProperties { get; set; }
        public List<string> Keywords { get; set; }
        public MarsErrorObjectIMAGE IMAGE { get; set; }

        public MarsErrorMessageMgr errorMessage { get; set; }

    }
    [Serializable]
    public class MarsErrorMessageMgr
    {
        public string propertyName { get; set; }

    }
    [Serializable]
    public class MarsError_Objects
    {
        public MarsError_Object Error_Object { get; set; }

    }
    /// <summary>
    /// Added after 11-27-2023
    /// </summary>
    [Serializable]
    public class MarsErrorObjectIMAGE
    {
        public bool enabled { get; set; }   
        public string scope { get; set; }
    }
    
}
