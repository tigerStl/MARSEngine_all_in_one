using Mars.message.Inter.MQCenter.MarsObjectsOperations;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    class MarsCheckBoxOperation : ThirdPartControlOpBase
    {
        internal bool SetBox(System.Windows.Forms.Control chckbox, string strParaMeter, string strData, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("MarsCheckBoxOperation.setBox", 
                string.Format("SetBox-Data-[{0}] Para:[{1}]", strParaMeter, strData));
            try
            {
                if (chckbox == null)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsCheckBoxOperation.setBox", strError = "source Object is null");
                    return false;
                }
                MarsCheckBoxOpBase checkBoxOpBase = new MarsCheckBoxOpBase();
                bool isOk = false;            
                bool valueOfData = checkBoxOpBase.convertTestDataToValue(strData, ref strError,ref isOk);
                if (!isOk) return false;
                ReflectorForCSharp rfc = new ReflectorForCSharp();


                /// get checked from control
                /// if the value is same as valueOfData, the return true
                /// get the bound, and click
                /// 
                bool isNotExists = false;
                bool valueOfControl = rfc.GetMember<bool>(chckbox, "Checked", ref isNotExists);
                if (isNotExists)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsCheckBoxOperation.setBox", strError = $"no 'Checked' property exists in type|{chckbox.GetType()}|");
                    return false;
                }
                if (valueOfControl == valueOfData)
                {
                    simpleLog.MarsLoggerSimple.Info("MarsCheckBoxOperation.setBox", $"value from data is|{strData}|, which can be taken as same as|{valueOfControl}, no op is taken, return ");
                    return true;
                }              

                /// wait for process is ready
                /// 
                MarsObjectOpBase.WaitUntilCurrentProcessIsNotBusy(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => {
                    var rect = chckbox.Bounds;
                    var rectScreen = chckbox.RectangleToScreen(rect);
                    simpleLog.MarsLoggerSimple.Info("MarsCheckBoxOperation.setBox", $"from client|{rect}|to screen|{rectScreen}");
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rectScreen.X + rectScreen.Width / 2, rectScreen.Y + rectScreen.Height / 2);
                });
                MarsObjectOpBase.WaitUntilCurrentProcessIsNotBusy(10);
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("MarsCheckBoxOperation.setBox", strError=e.Message, e);
                return false;
            }finally
            {
                simpleLog.MarsLoggerSimple.logEnd("MarsCheckBoxOperation.setBox");
            }
        }
    }
}
