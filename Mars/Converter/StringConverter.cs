using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mars.Converter
{

    public class StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,object parameter, CultureInfo culture)
        {
            if (value.Equals("Yes"))
            {
                Uri uri = new Uri("C:\\Source\\Repos\\mars\\Mars\\bin\\Debug\\a.jpg");
                BitmapImage source = new BitmapImage(uri);
                return source;

            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotSupportedException();

        }
    }

   


}
