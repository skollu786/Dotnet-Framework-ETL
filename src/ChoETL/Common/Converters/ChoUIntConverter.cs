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
    [ChoTypeConverter(typeof(uint))]
#if !NETSTANDARD2_0
    public class ChoUIntConverter : IValueConverter
#else
    public class ChoUIntConverter : IChoValueConverter
#endif
    {
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.UIntNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.UIntNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.UIntFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.UIntFormat);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.UIntNumberStyle);
                return format == null ? uint.Parse(text, culture) : uint.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is uint && targetType == typeof(string))
            {
                uint convValue = (uint)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.UIntFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
