using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Views.baseView
{
    public class MarsProjectBaseControlView: MarsBaseViewControl
    {
        protected string _projectName;
        protected long projectId = -1;

        public virtual string ProjectName
        {
            get { return _projectName; }
            set { _projectName = value;RaisePropertyChanged("ProjectName"); }
        }

        public virtual long ProjectId
        {
            get { return projectId; }
            set { projectId = value;RaisePropertyChanged("ProjectId"); }
        }
    }
}
