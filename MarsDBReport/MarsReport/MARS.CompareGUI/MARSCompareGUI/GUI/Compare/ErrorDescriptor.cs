using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.TEMP
{
    class ErrorDescriptor
    {
        public enum ErrorType { ROW_NOT_FOUND, ROW_NOT_MATCHING, COL_NOT_MATCHING, COL_NOT_FOUND };

        public ErrorType errorType;

        public ErrorDescriptor(ErrorType errorType)
        {
            this.errorType = errorType;
        }
    }
}
