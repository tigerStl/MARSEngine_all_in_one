using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Dto
{
    [DataContract]
    public partial class V_OBJECT_APPS
    {
        [DataMember]
        public Int64 APPLICATION_ID;
        [DataMember]
        public Int64 OBJECT_ID;
        [DataMember]
        public string COMMENT;
        [DataMember]
        public string ENUM_TYPE;
        [DataMember]
        public string OBJECT_HAPPY_NAME;
        [DataMember]
        public string OBJECT_TYPE;
        [DataMember]
        public string QUICK_ACCESS;
        [DataMember]
        public Int64 TYPE_ID;
        [DataMember]
        public Int64 APP_APPLICATION_ID;
        [DataMember]
        public string APP_SHORT_NAME;
        [DataMember]
        public Int16 APPLICATION_TYPE_ID;
        [DataMember]
        public string COMMENT_APP;
        [DataMember]
        public string EXTRAPOPUPMENU;
        [DataMember]
        public string EXTRAREQUIREMENT;
        [DataMember]
        public string PROCESS_IDENTIFIER;
        [DataMember]
        public string STARTER_COMMAND;
        [DataMember]
        public string STARTER_PATH;
        [DataMember]
        public string VERSION;
        [DataMember]
        public Int16 TYPE_APPLICATION_TYPE_ID;
        [DataMember]
        public string TYPE_DESCRPTION;
        [DataMember]
        public Int64 TYP_TYPE_ID;
        [DataMember]
        public string TYPE_NAME;
    }
}
