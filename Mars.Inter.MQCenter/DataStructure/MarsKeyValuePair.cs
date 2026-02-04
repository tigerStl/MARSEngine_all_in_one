using System;
using System.Collections.Generic;
using System.ComponentModel;
#if _NET4
using System.Threading.Tasks;
#endif

namespace Mars.message.AutoTestingDriver.SystemUtil.DataStructure
{

    public class MarsColumnForTableInfo
    {
        public string columnName { get; set; }
        public string columnKey { get; set; }
        public int columnKeyIdx { get; set; }
        public int ord { get; set; } 
        public bool isHidden { get; set; }
    }

    public class MarsTableCells
    {
        public string cellDisplayString { get; set; }
        public string cellDataValue { get; set; }
        public int colOrd;
        public string colName { get; set; }
    }

    public class MarsKeyValues<TKey, TValue> : INotifyPropertyChanged
    {
        private TKey mKey;
        private TValue mvalue;
        public TKey MKey { get { return mKey; } set { if (mKey.ToString() != value.ToString()) { mKey = value; OnPropertyChanged("MKey"); } } }
        public TValue MValue { get { return mvalue; } set { if (mvalue.ToString() != value.ToString()) { mvalue = value; OnPropertyChanged("MValue"); } } }
        public MarsKeyValues(TKey key, TValue value)
        {
            mKey = key;
            mvalue = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return MValue == null ? "" : MValue.ToString();
        }
        public List<MarsKeyValues<string, string>> Children { get; set; }
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public static MarsKeyValues<string, string> ConvertFromStringBySplitter(string strSrc, string[] arrSplitters, ref bool isOk, ref string strError)
        {
            if (string.IsNullOrEmpty(strSrc))
            {
                isOk = false;
                strError = "Null value is passed";
                return null;
            }

            string[] arrResult = strSrc.Split(arrSplitters, StringSplitOptions.None);
            if (arrResult.Length == 1)
            {
                isOk = true;
                return new MarsKeyValues<string, string>(arrResult[0], "");
            }
            if (arrResult.Length == 2)
            {
                isOk = true;
                return new MarsKeyValues<string, string>(arrResult[0], arrResult[1]);
            }

            strError = string.Format("string [{0}] can't be splitted into 2 by [{1}]", strSrc, arrSplitters);
            isOk = false;
            return null;
        }

    }


}
