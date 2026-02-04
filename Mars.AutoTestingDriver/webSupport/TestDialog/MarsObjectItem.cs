using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.webSupport.TestDialog
{
    class MarsWebObjListBoxItem
    {
        public string Text { get; set; }
        public object Tag { get; set; }

        public override string ToString()
        {
            return Text; // 让 ListBox 正确显示文本
        }
    }

}
