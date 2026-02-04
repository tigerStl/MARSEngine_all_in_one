
/*A class that is bundled with the Checked Combo Box class. 
 * CCBoxItem defines an item in a Checked Combo Box*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.CompareGUI
{
    public class CCBoxItem
    {
        private int val;
        public int Value
        {
            get { return val; }
            set { val = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public CCBoxItem()
        {
        }

        public CCBoxItem(string name, int val)
        {
            this.name = name;
            this.val = val;
        }

        public override string ToString()
        {
            return string.Format(name, val);
        }
    }
}
