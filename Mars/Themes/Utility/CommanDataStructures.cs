using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Utility
{
    class CommanDataStructures
    {
    }

    //public class MarsKeyValues<TKey, TValue> : INotifyPropertyChanged
    //{
    //    private TKey mKey;
    //    private TValue mvalue;
    //    public TKey MKey { get { return mKey; } set { if (mKey.ToString() != value.ToString()) { mKey = value; OnPropertyChanged("MKey"); } } }
    //    public TValue MValue { get { return mvalue; } set { if (mvalue.ToString() != value.ToString()) { mvalue = value; OnPropertyChanged("MValue"); } } }
    //    public MarsKeyValues(TKey key, TValue value)
    //    {
    //        mKey = key;
    //        mvalue = value;
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    public override string ToString()
    //    {
    //        return MValue == null ? "" : MValue.ToString();
    //    }
    //    public List<MarsKeyValues<string, string>> Children { get; set; }
    //    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    //    {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null)
    //            handler(this, e);
    //    }
    //    protected void OnPropertyChanged(string propertyName)
    //    {
    //        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    //    }
    //}
}
