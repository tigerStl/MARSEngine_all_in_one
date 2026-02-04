using Mars.message.AutoTestingDriver.ErrorMessage;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal abstract class ClickRadioButtonBase
    {
        internal abstract bool Click(object cntrl, string strParameter, string strData, ref string strError, ref string strAdv, ref string strStack);
    }
    internal class ClickRadioButton
    {
        internal class Standard : ClickRadioButtonBase
        {
            internal override bool Click(object cntrl, string strParameter, string strData, ref string strError, ref string strAdv,
                ref string strStack)
            {
                System.Windows.Forms.RadioButton radioButton = cntrl as System.Windows.Forms.RadioButton;
                if (radioButton == null)
                {
                    strError = "Object is not a radiobutton."; ;//string.Format("object is not a radiobutton its type is [{0}]", cntrl==null?"NULL":cntrl.GetType().ToString());
                    strStack = $"its type is [{0}]\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Mars supports Infragistics, WinForm and WPF controls, and use object spy to correct.";
                    return false;
                }
                if ((string.Compare(strData, "true", true) == 0) || (string.Compare(strData, "on", true) == 0))
                {
                    if (!radioButton.Checked)
                    {
                        radioButton.PerformClick();
                    }
                }
                else
                {
                    if (radioButton.Checked)
                    {
                        radioButton.PerformClick();
                    }
                }

                return true;
            }
        }
    }
}
