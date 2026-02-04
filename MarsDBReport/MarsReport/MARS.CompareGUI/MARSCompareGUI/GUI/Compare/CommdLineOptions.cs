using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.CompareGUI
{
    class CommdLineOptions
    {
        Dictionary<string, string> opts = new Dictionary<string, string>();
        public void init(string[] args)
        {
            string tmpStr = "";

            foreach (string word in args)
            {
                tmpStr += word + " ";
            }

            string[] pairs = tmpStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pairStr in pairs)
            {
                string[] pair = pairStr.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 1)
                    opts.Add("-" + pair[0].Trim(), null);
                else
                    opts.Add("-" + pair[0].Trim(), pair[1].Trim());
            }
        }

        public string GetOptionStringValue(string option)
        {
            string value = "";

            if (opts.ContainsKey(option))
                value = opts[option];

            return value;
        }

        public bool GetOptionBooleanValue(string option)
        {
            bool value = false;

            if (opts.ContainsKey(option))
                value = true;

            return value;
        }
    }
}
