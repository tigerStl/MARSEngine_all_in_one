using System;
using System.Collections.Generic;
using System.Text;

namespace MarsExceptionManagement
{
    public enum Mars_ErrorType
    {
        em_sqlException = 0x02, 
        em_programException, 
        em_restFULException, 
        em_restFULError,
        em_otherException
    }

    [Serializable]
    public class MarsExceptionBase
    {
        public Mars_ErrorType errorType { get; set; }
        public string messasgeToClient { get; set; }
        public MarsExceptionBase(Exception e) { 
        }
    }

    public class MarsSQLException
    {
        //public MarsSQLException(Exception e) : base(e)
        //{

        //}
    }
}
