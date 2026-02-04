

using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Mars.TestFramework.DataCompare;


namespace MARS.CompareGUI
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //BasicConfigurator.Configure();
            log4net.Config.XmlConfigurator.Configure();
            log.Info("Entering application.");
            if (args.Length == 0)
            {
                //Run GUI
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DataCompareForm());
            }
            else
            {
                //Run Command Line
                CommdLineOptions options = new CommdLineOptions();
                options.init(args);
                ComparewithID CmdCompareData = new ComparewithID();
                string ConfigLoc = "";

                //Get input values
                if (options.GetOptionStringValue("-C").Length > 0)
                {ConfigLoc = options.GetOptionStringValue("-C");}
                if (options.GetOptionStringValue("-ID").Length > 0)
                {CmdCompareData.CompareID = options.GetOptionStringValue("-ID");}
                if (options.GetOptionStringValue("-I1").Length > 0)
                {CmdCompareData.S1FileLocation = options.GetOptionStringValue("-I1");}
                if (options.GetOptionStringValue("-I2").Length > 0)
                {CmdCompareData.S2FileLocation = options.GetOptionStringValue("-I2");}
                if (options.GetOptionStringValue("-O").Length > 0)
                {CmdCompareData.OFileLocation = options.GetOptionStringValue("-O");}
                if (options.GetOptionBooleanValue("-H") == true)
                {
                    Console.WriteLine("");
                    Console.WriteLine("+----------------------------------+");
                    Console.WriteLine("|                                  |");
                    Console.WriteLine("|     MARS COMPARE UTILITY      |");
                    Console.WriteLine("|                                  |");
                    Console.WriteLine("|Flag          Purpose             |");
                    Console.WriteLine("|==================================|");
                    Console.WriteLine("|                                  |");
                    Console.WriteLine("|-H       Displays this screen     |");
                    Console.WriteLine("|-C       Config file location     |");
                    Console.WriteLine("|-ID      Compare ID               |");
                    Console.WriteLine("|-I1      Input file 1 location    |");
                    Console.WriteLine("|-I2      Input file 2 location    |");
                    Console.WriteLine("|-O       Output file location     |");
                    Console.WriteLine("|==================================|");
                    Console.WriteLine("|      Flags for File Compare      |");
                    Console.WriteLine("|       -C, -ID, -I1, I2, -O       |");
                    Console.WriteLine("|==================================|");
                    Console.WriteLine("|    Flags for Database Compare    |");
                    Console.WriteLine("|              -C, -O              |");
                    Console.WriteLine("|==================================|");
                    Console.WriteLine("+----------------------------------+");
                    Console.WriteLine("");
                }

                //Execute compare 
                if ((options.GetOptionStringValue("-C").Length > 0) && (options.GetOptionStringValue("-ID").Length > 0))
                {
                    ComparewithID ExeCompare = new ComparewithID();
                    ExeCompare = ReadConfigXML.GetCompareFromID(CmdCompareData.CompareID, ConfigLoc);

                    if (options.GetOptionStringValue("-I1").Length > 0)
                    { ExeCompare.S1FileLocation = CmdCompareData.S1FileLocation; }
                    if (options.GetOptionStringValue("-I2").Length > 0)
                    { ExeCompare.S2FileLocation = CmdCompareData.S2FileLocation; }
                    if (options.GetOptionStringValue("-O").Length > 0)
                    { ExeCompare.OFileLocation = CmdCompareData.OFileLocation; }

                    if (ExeCompare.S1Type == "DATABASE")
                    {
                        //S1 Connection String
                        DBConnectionwithID GetS1ConnString = new DBConnectionwithID();
                        GetS1ConnString = ReadConfigXML.GetConnectionFromID(ExeCompare.S1DBConn, ConfigLoc);
                        ExeCompare.S1ConnString = GetS1ConnString.BuildConnectionString();
                        //S1 Query
                        ExeCompare.S1Query = ReadConfigXML.GetQueryFromID(ExeCompare.S1QueryID, ConfigLoc);
                    }

                    if (ExeCompare.S2Type == "DATABASE")
                    {
                        //S2 Connection String
                        DBConnectionwithID GetS2ConnString = new DBConnectionwithID();
                        GetS2ConnString = ReadConfigXML.GetConnectionFromID(ExeCompare.S2DBConn, ConfigLoc);
                        ExeCompare.S2ConnString = GetS2ConnString.BuildConnectionString();
                        //S2 Query
                        ExeCompare.S2Query = ReadConfigXML.GetQueryFromID(ExeCompare.S2QueryID, ConfigLoc);
                    }

                   // ExecuteCompare.ExecuteCompareProgram(ExeCompare);
                }
                else
                {
                    Console.WriteLine("Error: The Compare Program requires both the Config File location and a Compare ID. Type -H for help.");
                }
            }
        }
    }
}
