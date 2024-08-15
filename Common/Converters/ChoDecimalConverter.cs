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
    [ChoTypeConverter(typeof(Decimal))]
#if !NETSTANDARD2_0
    public class ChoDecimalConverter : IValueConverter
#else
    public class ChoDecimalConverter : IChoValueConverter
#endif
    {
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.DecimalNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.DecimalNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.DecimalFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.DecimalFormat);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.DecimalNumberStyle);
                if (format == null)
                {
                    Decimal decResult = 0;
                    if (Decimal.TryParse(text, NumberStyles.Currency, culture, out decResult))
                        return decResult;
                }
                return format == null ? Decimal.Parse(text, culture) : Decimal.Parse(text, format.Value, culture);
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Decimal && targetType == typeof(string))
            {
                Decimal convValue = (Decimal)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.DecimalFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
