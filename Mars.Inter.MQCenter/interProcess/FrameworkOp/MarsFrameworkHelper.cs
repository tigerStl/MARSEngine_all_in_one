using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.interProcess.FrameworkOp
{
    public sealed class MarsFrameworkHelper
    {
        public static void MarsRecursiveGetAllChildren(System.Windows.Forms.Control parentCntrl, List<object> targetList, bool isUnderRevoke = false)
        {

            if (parentCntrl == null) return;
            if ((!isUnderRevoke) && (parentCntrl.InvokeRequired))
            {
                parentCntrl.Invoke((System.Windows.Forms.MethodInvoker)(() =>
                {

                    for (int i = 0; i < parentCntrl.Controls.Count; i++)
                    {
                        if (parentCntrl.Controls[i] != null)
                        {
                            targetList.Add(parentCntrl.Controls[i]);
                            if (parentCntrl.Controls[i].HasChildren)
                            {
                                MarsRecursiveGetAllChildren(parentCntrl.Controls[i], targetList, true);
                            }
                        }
                    }
                }));
            }
            else
            {
                //MarsLoggerSimple.Info("\t", string.Format("Name of control [{0}], type:[{1}]", parentCntrl.Name, parentCntrl.GetType().ToString()));
                for (int i = 0; i < parentCntrl.Controls.Count; i++)
                {
                    if (parentCntrl.Controls[i] != null)
                    {
                        targetList.Add(parentCntrl.Controls[i]);
                        if (parentCntrl.Controls[i].HasChildren)
                        {
                            MarsRecursiveGetAllChildren(parentCntrl.Controls[i], targetList);
                        }

                    }
                }
            }

        }
    }
}
