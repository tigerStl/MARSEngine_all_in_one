using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.Observer
{
    public interface IObserverInterface4All
    {
    }

    public interface IMarsObserver
    {
        void NotifyMessage(string strCatalog, string strMessage);
        void NotifyError(string strCatalog);
    }

}
