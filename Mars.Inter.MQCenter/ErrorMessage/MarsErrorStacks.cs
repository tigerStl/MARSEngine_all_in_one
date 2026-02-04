using System;
using System.Diagnostics;
using System.Reflection;
#if _withMessageNamespace
namespace Mars.message.AutoTestingDriver.ErrorMessage
#else
namespace Mars.AutoTestingDriver.ErrorMessage
#endif
{
    internal class MarsErrorStacks
    {
        public static string StackTraceDump(StackTrace st1 = null)
        {
            //try
            //{
            //    throw new Exception("To get Error");
            //}
            //catch (Exception e)
            //{
            //    return e.StackTrace;
            //}

            string text = "";
            StackTrace st = st1 == null ? new StackTrace(true) : st1;
            for (int i = 1; i < st.FrameCount; i++) //the first row is this method
            {
                // Note that high up the call stack, there is only
                // one stack frame.
                StackFrame sf = st.GetFrame(i);
                int lineNumber = sf.GetFileLineNumber();
                Console.WriteLine($"{i} at fileName:{sf.GetFileName()} method:{sf.GetMethod().Name} line:{sf.GetFileLineNumber()}");
                MethodBase method = sf.GetMethod();
                var declaringType = method.DeclaringType;
                string methodFullName = "";
                if (declaringType == null)
                {
                    methodFullName = method.Name;
                }
                else
                    methodFullName = declaringType.FullName;

                text += "\r\n";
                if (lineNumber == 0)
                    text += methodFullName;
                else
                    text += $"at Class: {methodFullName} Method:{sf.GetMethod()} Line: {sf.GetFileLineNumber()}";
                //text += "Class: " + methodFullName + " Method: " + sf.GetMethod() + " File: " + sf.GetFileName() + " Line: " + sf.GetFileLineNumber();
            }

            return text;
        }
    }
}
