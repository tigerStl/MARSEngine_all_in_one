using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.DataTolerance
{
    public abstract class TigerMarsDataTolerance
    {
        public string ToleranceMark;

        public abstract bool ParaseToleranceConfig(string strSource,ref string strError);
    }

    public class TigerMarsSubTolerance: TigerMarsDataTolerance
    {
        public double SubValue;

        public override bool ParaseToleranceConfig(string strSource, ref string strError)
        {
            if (string.IsNullOrEmpty(strSource))
            {
                strError = "Source is null";
                return false;
            }
            string strUpper = strSource.ToUpper();
            return false;
        }
    }
}
