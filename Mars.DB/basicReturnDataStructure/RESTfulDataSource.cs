using Mars.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace MarsEngineSvc.basicReturnDataStructure
{
    [Serializable, DataContract]
    public class RESTfulDatasources : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<T_DATA_SOURCEDTO> DataSources;

        public RESTfulDatasources():base() 
        {
            this.objectType = (int)RESTfulObjectType.dataSourceTable;
        }
    }
}