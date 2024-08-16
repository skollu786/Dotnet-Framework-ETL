﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(BigInteger))]
#if !NETSTANDARD2_0
    public class ChoBigIntegerConverter : IValueConverter
#else
    public class ChoBigIntegerConverter : IChoValueConverter
#endif
    {
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.BigIntegerNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.BigIntegerNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.BigIntegerFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.BigIntegerFormat);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.BigIntegerNumberStyle);
                return format == null ? BigInteger.Parse(text, culture) : BigInteger.Parse(text, format.Value, culture);
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is BigInteger && targetType == typeof(string))
            {
                BigInteger convValue = (BigInteger)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.BigIntegerFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
