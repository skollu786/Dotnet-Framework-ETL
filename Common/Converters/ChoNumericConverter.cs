﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
#if !NETSTANDARD2_0
    public class ChoNumericConverter : IValueConverter
#else
    public class ChoNumericConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (!((string)value).IsNumber())
                    throw new ApplicationException("'{0}' value is not number.".FormatString(value));
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (!((string)value).IsNumber())
                    throw new ApplicationException("'{0}' value is not number.".FormatString(value));
            }
            else if (value == DBNull.Value)
                return null;

            return value;
        }
    }
}
