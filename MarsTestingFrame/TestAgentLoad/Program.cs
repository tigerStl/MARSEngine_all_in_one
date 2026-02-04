using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAgentLoad
{
    class Program
    {
        static void Main(string[] args)
        {
            string strError = "";
            AgentHost.HostInjector.LoadInjector("TPG_PG", ref strError);
        }
    }
}
