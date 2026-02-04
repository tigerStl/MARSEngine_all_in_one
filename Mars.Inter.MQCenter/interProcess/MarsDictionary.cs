namespace MarsCore.MessageCenter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    [Serializable]
    public class MarsDataItem
    {
        [XmlElement("Key")]
        public string Key { get; set; }
        [XmlElement("Value")]
        public string Value { get; set; }
        public MarsDataItem(string k, string v)
        {
            Key = k;
            Value = v;
        }
        public MarsDataItem()
        {

        }
    }

    [Serializable]
    public class MarsDictionary
    {
        [XmlArray("MarsListItems")]
        public List<MarsDataItem> Items { get; set; } = new List<MarsDataItem>();

        public void Add(string k, string v)
        {
            if (k == null) return;
            foreach (var itm in Items)
            {
                if (string.Compare(itm.Key, k) == 0)
                {
                    itm.Value = v;
                    return;
                }
            }
            Items.Add(new MarsDataItem(k, v));
        }


        public void Clear()
        {
            Items.Clear();
        }

        public static MarsDictionary ConvertFrom(Dictionary<string, string> fromDic)
        {
            if (fromDic == null) return null;
            MarsDictionary result = new MarsDictionary();
            foreach (var k in fromDic.Keys)
            {
                result.Add(k, fromDic[k]);
            }
            return result;
        }

        public Dictionary<string, string> ConvertTo()
        {
            Dictionary<string, string> resultDic = new Dictionary<string, string>();
            foreach (var itm in Items)
            {
                if (resultDic.ContainsKey(itm.Key))
                    resultDic[itm.Key] = itm.Value;
                else
                    resultDic.Add(itm.Key, itm.Value);
            }
            return resultDic;
        }

        public static MarsDictionary ConvertToMarsDictionary(string input)
        {
            var marsDictionary = new MarsDictionary();

            // 按照 \r\n 分割成行
            var lines = input.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // 按照 := 分割成 key 和 value
                var parts = line.Split(new[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    marsDictionary.Add(key, value);
                }
            }

            return marsDictionary;
        }

    }
}
