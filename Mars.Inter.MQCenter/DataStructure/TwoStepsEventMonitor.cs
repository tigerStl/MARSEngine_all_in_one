using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.DataStructure
{
    public class TwoStepsEventMonitor
    {
        public string currentObjectType { get; set; }
        public bool isTwoStepEvent { get; set; }
        public IntPtr previousObjectHandle { get; set; }
        public IntPtr currentObjectHandle { get; set; }
        public int countLog { get; set; }
        public System.Windows.Forms.Control twoStepsControl { get; set; }
        public void init()
        {
            currentObjectType = string.Empty;
            isTwoStepEvent = false;
            previousObjectHandle = IntPtr.Zero;
            currentObjectHandle = IntPtr.Zero;
            countLog = 0;
            twoStepsControl = null;
        }
        public bool IsTwoStepsControlFoucs()
        {
            if (twoStepsControl == null) return false;
            return twoStepsControl.Focused;
        }

    }
}
