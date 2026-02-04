using System;

namespace MarsTestFrame.SourceCode.systemUtil
{
    public class MarsExceptions : Exception
    {
        public int ErrorId;
        public MarsExceptions(int iErrorId, string strMessage)
            : base(strMessage)
        {
            ErrorId = iErrorId;
        }
    }
}
