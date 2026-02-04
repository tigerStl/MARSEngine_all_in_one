extern alias clientWCF;

using clientWCF::MarsTestFrame.CommuniteServer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsTestFrame.SourceCode.systemUtil
{
    /// <summary>
    /// 该文件提供所有供host调用的delegate等东东
    /// </summary>
    /// 
    public delegate void OnAddTestStepUnitObjEvent(IList<TestStep4Services> obj);
    public delegate void OnTestResultMessageArrivedEvent(string strInformationToDisplay, string strType);
    public delegate bool OnRequestWCFSvcStatusEvent();
    public delegate bool OnRequestStartWCFSvcEvent(int iMode, ref string strError);

    class MARSFrameworkPublicInterfaces
    {
    }
}
