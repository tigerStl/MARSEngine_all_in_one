using MARS_Web.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace MARSWebUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRESTFulLink()
        {
            string MarsEnvironment = string.Empty;
            
            MarsConfig mc = MarsConfig.Configure(@"C:\Users\gengf\Source\Repos\MARS_Revamp\MARS_Web\Config\Mars.config",MarsEnvironment);
            Debugger.Log(0, "INFO", MarsConfig.restfulInfo.ToString());
        }
    }
}
