using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mars.Converter
{
    public class BaseCompConverter : IValueConverter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(BaseCompConverter));
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Logger.logBegin("Convert",string.Format("value:[{0}] targetType:[{1}]", value==null?"NULL":value.ToString(),targetType));
            if (value == null) return "";
            switch (value.ToString())
            {
                case "1":
                    return "BASELINE";
                case "2":
                    return "COMPARE";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0;
            switch (value.ToString())
            {
                case "BASELINE":
                    return "1";
                case "COMPARE":
                    return "2";
            }
            return "";
           
        }
    
}
}
