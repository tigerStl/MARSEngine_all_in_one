using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string strSubColumns = "idx:Posting Date;Value Date;GL Date;Account ID;Debit Credit;Event Type;Posting Type];Amount";
            string[] arrRslt = 
                strSubColumns.Split(new string[] { ";" }, StringSplitOptions.None);
            
            Console.WriteLine($"\t\t7.3  [{strSubColumns}]==>[{arrRslt}]");
        }
    }
}
