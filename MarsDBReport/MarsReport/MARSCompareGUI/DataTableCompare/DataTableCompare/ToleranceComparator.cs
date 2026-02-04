using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class ToleranceComparator
    {

        internal static ToleranceResult Compare(ToleranceConfig tc, double num1, double num2)
        {
            ToleranceResult tr = new ToleranceResult();
            if (tc.CompareType.Equals("A"))
                tr = CompareAbsolute(num1, num2, tc.ToleranceValue);
            else
                tr = CompareByPercent(num1, num2, tc.ToleranceValue);

            return tr;
        }

        private static ToleranceResult CompareByPercent(double num1, double num2, double toleranceValue)
        {
            ToleranceResult tr = new ToleranceResult();
            double n1, n2;

            if (num1 < num2)
            {
                n1 = num1;
                n2 = num2;
            }

            else
            {
                n1 = num2;
                n2 = num1;
            }


            double diff = (n1 / n2);

            if (diff > toleranceValue)
            {
                tr.Result = false;
                tr.ResultComment = "Difference found over " + toleranceValue + " percent";
            }
            else
            {
                tr.Result = true;
                tr.ResultComment = "Difference found under " + toleranceValue + " percent";
            }

            tr.diff = diff;

            return tr;
        }

        private static ToleranceResult CompareAbsolute(double num1, double num2, double toleranceValue)
        {
            ToleranceResult tr = new ToleranceResult();
            double diff = Math.Abs(num1 - num2);

            if (diff > toleranceValue)
            {
                tr.Result = false;
                tr.ResultComment = "Difference found over " + toleranceValue;
            }
            else
            {
                tr.Result = true;
                tr.ResultComment = "Difference found under " + toleranceValue;
            }

            tr.diff = diff;
            return tr;
        }


    }
}
