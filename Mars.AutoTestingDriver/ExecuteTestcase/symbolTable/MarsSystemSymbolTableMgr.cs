using Mars.AutoTestingDriver.Properties;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase.symbolTable
{

    public class MarsMemorySymbol
    {
        public string varName { get; set; }
        public string varType { get; set; }
        public object varInfo { get; set; }
    }

    /// <summary>
    /// this is the class like memory variable management. 
    /// </summary>
    public class MarsSystemSymbolTableMgr
    {
        private static MLogger logger  = MLogger.GetLogger(typeof(MarsSystemSymbolTableMgr));
        private static Dictionary<string,MarsMemorySymbol> currentMarsMemorySymbolTable = new Dictionary<string, MarsMemorySymbol>();
        public static bool putVarToSymbolTable(string varIdxName, string typ, DataTable dt, ref string strError)
        {
            try
            {
                if (currentMarsMemorySymbolTable.ContainsKey(varIdxName))
                {
                    currentMarsMemorySymbolTable[varIdxName] = new MarsMemorySymbol()
                    {
                        varInfo = dt,
                        varType = typ,
                        varName = varIdxName,
                    };
                }
                else
                {
                    currentMarsMemorySymbolTable.Add(varIdxName, new MarsMemorySymbol()
                    {
                        varInfo = dt,
                        varType = typ,
                        varName = varIdxName
                    });
                }
                return true;
            }catch(Exception e)
            {
                strError = Resources.mars_symboltable_cant_add;
                logger.Error("putVarToSymbolTable", e.Message, e);
                return false;
            }
        }
    }
}
