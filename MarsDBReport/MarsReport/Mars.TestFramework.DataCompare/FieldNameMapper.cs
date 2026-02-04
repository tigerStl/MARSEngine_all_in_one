using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars.TestFramework.DataCompare
{
    public class FieldNameMapper
    {
        public Dictionary<string, string> map = new Dictionary<string, string>();

        public void init(string configData)
        {
            string[] pairs = Util.Split(configData, ',');
            foreach (string pair in pairs)
            {
                if (pair.Trim().Length == 0)
                    continue;
                string[] keyValuePair = Util.Split(pair, ':');
                map.Add(keyValuePair[0].Trim(), keyValuePair[1].Trim());

            }
        }

        public string GetMappedValue(string key)
        {
            string value = key;
            if (map.ContainsKey(key))
                value = map[key];

            return value;
        }
    }
}
