using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.TEMP
{
    class XmlCompareConfig
    {
        public string BlockTag;
        public string ElementTag;
        public List<string> KeyFields = new List<string>();
        public List<string> CompareFields = new List<string>();
        public List<string> ShowFields = new List<string>();
        public List<string> ExcludeFields = new List<string>();

        public List<string> RowFields = new List<string>();
        public List<string> ColumnFields = new List<string>();

        public FieldNameMapper fieldNameMapper;
        public AdjustDataMap adjustDataMap;
        public Boolean AllOption = false;

        public Boolean ShowDiffOnly = false;

        public bool devMode=false;

        internal void SetFieldNameMapper(FieldNameMapper fieldNameMapper)
        {
            this.fieldNameMapper = fieldNameMapper;
        }

        internal void SetAllOption(bool opt)
        {
            AllOption = opt;
        }

        internal void SetShowDiffOnly(bool opt)
        {
            ShowDiffOnly = opt;
        }

        internal void SetAdjustData(AdjustDataMap adjustDataMap)
        {
            this.adjustDataMap = adjustDataMap;
        }
    }
}
