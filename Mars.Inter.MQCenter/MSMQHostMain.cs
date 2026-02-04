using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter
{

    public interface IMSMQHostMain
    {
        bool StartMsmqService();
    }
    public class MSMQHostMain: IMSMQHostMain
    {
        public bool StartMsmqService()
        {
            return false;
        }
    }
}
