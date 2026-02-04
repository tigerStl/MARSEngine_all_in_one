
using Mars.message.AutoTestingDriver.ErrorMessage;
using System.Diagnostics;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.standardControl
{
    class MarsListBoxOperation
    {
        internal bool SelectListItem(string strData, string strParaMeter, ListBox lstBox, ref string strError, ref string strAdv, ref string strStack)
        {
            if (lstBox == null)
            {
                strError = "Passed Null to a function";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            string strTmp = "";
            for (int i = 0; i < lstBox.Items.Count; i++)
            {
                if (lstBox.Items[i] == null) continue;
                string strItm = lstBox.Items[i].ToString();
                strTmp += (";" + strItm);
                if ((windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, strItm)) || (string.Compare(strItm, strData, true) == 0))
                {
                    var rect = lstBox.GetItemRectangle(i);
                    if ((default(System.Drawing.Rectangle).Equals(rect))
                        ||(rect.Width<=2)
                        ||(rect.Height<=2))
                        lstBox.SelectedIndex = i;
                    else
                    {
                        /// mouse模式
                        /// 
                        var scrnRct = lstBox.RectangleToScreen(rect);
                        if (default(System.Drawing.Rectangle).Equals(rect))
                            lstBox.SelectedIndex = i;
                        else
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(scrnRct.X+ scrnRct.Width/2, 
                                scrnRct.Y+scrnRct.Height/2);
                        }
                    }
                    return true;
                }
            }
            strError = string.Format("no [{0}] found in [{1}]", strData, strTmp);
            strStack = $"{strError}\r\n{MarsErrorStacks.StackTraceDump()}";
            strError = $"Can't find [{strData}] in ListBox";
            strAdv = $"Make sure [{strData}] is avaialbe in ListBox";
            return false;
        }
    }
}
