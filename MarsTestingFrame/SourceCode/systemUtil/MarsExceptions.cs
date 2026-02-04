using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.systemUtil
{
    public class MarsExceptions:Exception
    {
        public int ErrorId ;
        public MarsExceptions(int iErrorId, string strMessage)
            : base(strMessage)
        {
            ErrorId = iErrorId;
        }
    }
}
