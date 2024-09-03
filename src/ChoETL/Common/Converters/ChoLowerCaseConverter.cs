using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NET7_0_OR_GREATER
using System.Windows.Data;
#endif

namespace ChoETL
{
#if !NET7_0_OR_GREATER
    public class ChoLowerCaseConverter : IValueConverter
#else
    public class ChoLowerCaseConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return ((string)value).ToLower();
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return ((string)value).ToLower();
            }
            return value;
        }
    }
}
