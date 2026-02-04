using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars.TestFramework.DataCompare
{
    class Util
    {
        public static string[] Split(string str, char ch)
        {
            string[] pair = str.Split(new char[] { ch }, StringSplitOptions.RemoveEmptyEntries);

            return pair;
        }
    }
}
