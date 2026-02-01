using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mars.Validate
{
    public class MarsUIValidate
    {

    }

    public class IsNullValidateRule : ValidationRule
    {
        private static MLogger Logger = MLogger.GetLogger("IsNullValidateRule");

        public string ErrorMessage { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            Logger.Info("Validate",string.Format("value:[{0}]",value??""));
            if (value == null)
            {
                return new ValidationResult(false, ErrorMessage);// "It should not be null!");
            }

            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(false, ErrorMessage);// "It can't be empty!");
            }
            return ValidationResult.ValidResult;
        }
    }
}
