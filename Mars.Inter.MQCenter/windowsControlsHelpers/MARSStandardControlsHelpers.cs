using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public class MARSStandardControlsHelpers
    {
        private static string[] standardControls = new string[]
            {
                "Button",
                "CheckBox",
                "ComboBox",
                "DateTimePicker",
                "Edit",
                "GroupBox",
                "Label",
                "ListBox",
                "ListView",
                "Menu",
                "MenuItem",
                "MonthCalendar",
                "ProgressBar",
                "RadioButton",
                "ScrollBar",
                "Slider",
                "Spinner",
                "StatusBar",
                "TabControl",
                "TabItem",
                "TextBox",
                "ToolBar",
                "ToolTip",
                "TreeView",
                "TreeItem"
            };
        
        public static bool IsMARSObjectsTypeStandardControl(string objectType)
        {
            
            return standardControls.Contains(objectType);
        }
    }
}
