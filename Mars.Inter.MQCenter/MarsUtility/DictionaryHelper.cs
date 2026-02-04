using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MarsUtility
{
    public class DictionaryHelper
    {
        public static bool TryGetValueIgnoreCase(Dictionary<string, string> dict, string key, out string value)
        {
            if (dict == null)
            {
                value = null;
                return false;
            }
            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
    }
}
